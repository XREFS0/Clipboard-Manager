using System.Text.RegularExpressions;
using MASA.ClipboardManager.Core.Enums;
using MASA.ClipboardManager.Core.Interfaces;

namespace MASA.ClipboardManager.Application.Detectors;

public class ContentTypeDetector : IContentTypeDetector
{
    private static readonly Regex UrlRegex = new(
        @"^(https?|ftp|file):\/\/[-A-Za-z0-9+&@#\/%?=~_|!:,.;]*[-A-Za-z0-9+&@#\/%=~_|]",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly Regex EmailRegex = new(
        @"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$",
        RegexOptions.Compiled);

    private static readonly Regex HtmlTagRegex = new(
        @"<\/?(html|head|body|div|span|p|a|script|style|table|tr|td|th|h[1-6]|ul|ol|li)[^>]*>",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly string[] CodeKeywords =
    {
        "public class ", "private void ", "function ", "const ", "let ", "var ",
        "def ", "import ", "export default", "#include", "namespace ", "using System",
        "SELECT * FROM", "INSERT INTO", "UPDATE ", "DELETE FROM", "public static void Main",
        "console.log", "System.out.println", "Console.WriteLine", "<?php", "fn main()",
        "func ", "package main", "class ", "interface ", "enum ", "struct "
    };

    public (ClipboardContentType Type, string? Preview) Detect(string? text, string format, IEnumerable<string>? fileList)
    {
        if (fileList != null && fileList.Any())
        {
            var files = fileList.ToList();
            if (files.Count == 1)
            {
                var first = files[0];
                if (Directory.Exists(first))
                {
                    return (ClipboardContentType.Folder, $"📁 {Path.GetFileName(first)}");
                }
                return (ClipboardContentType.File, $"📄 {Path.GetFileName(first)}");
            }
            return (ClipboardContentType.File, $"📄 {files.Count} files ({Path.GetFileName(files[0])}...)");
        }

        if (format == "Bitmap" || format == "Image")
        {
            return (ClipboardContentType.Image, "🖼 Image Clipboard Data");
        }

        if (string.IsNullOrWhiteSpace(text))
        {
            return (ClipboardContentType.Text, string.Empty);
        }

        var trimmed = text.Trim();

        // URL Check
        if (UrlRegex.IsMatch(trimmed) || (trimmed.StartsWith("www.") && trimmed.Contains('.')))
        {
            return (ClipboardContentType.URL, trimmed.Length > 80 ? trimmed[..77] + "..." : trimmed);
        }

        // Email Check
        if (EmailRegex.IsMatch(trimmed))
        {
            return (ClipboardContentType.Email, trimmed);
        }

        // HTML Check
        if (HtmlTagRegex.IsMatch(trimmed))
        {
            return (ClipboardContentType.HTML, GenerateTextPreview(trimmed));
        }

        // Code Check (JSON check or keywords/braces heuristics)
        if (IsCodeLike(trimmed))
        {
            return (ClipboardContentType.Code, GenerateTextPreview(trimmed));
        }

        // Default Text or RichText
        var contentType = format == "RichText" ? ClipboardContentType.RichText : ClipboardContentType.Text;
        return (contentType, GenerateTextPreview(trimmed));
    }

    private static bool IsCodeLike(string text)
    {
        if (text.Length < 10) return false;

        // JSON check
        if ((text.StartsWith("{") && text.EndsWith("}")) || (text.StartsWith("[") && text.EndsWith("]")))
        {
            if (text.Contains("\"") && text.Contains(":"))
            {
                return true;
            }
        }

        int matchCount = CodeKeywords.Count(kw => text.Contains(kw, StringComparison.OrdinalIgnoreCase));
        if (matchCount >= 1)
        {
            return true;
        }

        // Semicolon + Braces heuristic
        int braces = text.Count(c => c == '{' || c == '}' || c == '(' || c == ')' || c == ';' || c == '=');
        double density = (double)braces / text.Length;
        if (density > 0.08 && text.Contains(';') && (text.Contains('{') || text.Contains('(')))
        {
            return true;
        }

        return false;
    }

    private static string GenerateTextPreview(string text, int maxLength = 100)
    {
        var singleLine = text.Replace("\r\n", " ").Replace("\n", " ").Replace("\t", " ");
        while (singleLine.Contains("  "))
        {
            singleLine = singleLine.Replace("  ", " ");
        }
        return singleLine.Length <= maxLength ? singleLine : singleLine[..maxLength] + "...";
    }
}
