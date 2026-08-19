using System.Globalization;
using System.Net;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using MASA.ClipboardManager.Core.Interfaces;

namespace MASA.ClipboardManager.Application.Services;

public class DeveloperToolsService : IDeveloperToolsService
{
    private static readonly JsonSerializerOptions IndentedJsonOptions = new()
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    public string FormatJson(string input)
    {
        if (string.IsNullOrWhiteSpace(input)) return input;
        try
        {
            using var doc = JsonDocument.Parse(input);
            return JsonSerializer.Serialize(doc.RootElement, IndentedJsonOptions);
        }
        catch
        {
            return input;
        }
    }

    public string MinifyJson(string input)
    {
        if (string.IsNullOrWhiteSpace(input)) return input;
        try
        {
            using var doc = JsonDocument.Parse(input);
            return JsonSerializer.Serialize(doc.RootElement);
        }
        catch
        {
            return input;
        }
    }

    public string UrlEncode(string input)
    {
        return string.IsNullOrEmpty(input) ? string.Empty : Uri.EscapeDataString(input);
    }

    public string UrlDecode(string input)
    {
        return string.IsNullOrEmpty(input) ? string.Empty : Uri.UnescapeDataString(input);
    }

    public string Base64Encode(string input)
    {
        if (string.IsNullOrEmpty(input)) return string.Empty;
        var bytes = Encoding.UTF8.GetBytes(input);
        return Convert.ToBase64String(bytes);
    }

    public string Base64Decode(string input)
    {
        if (string.IsNullOrEmpty(input)) return string.Empty;
        try
        {
            var bytes = Convert.FromBase64String(input.Trim());
            return Encoding.UTF8.GetString(bytes);
        }
        catch
        {
            return input;
        }
    }

    public string HtmlEncode(string input)
    {
        return string.IsNullOrEmpty(input) ? string.Empty : WebUtility.HtmlEncode(input);
    }

    public string HtmlDecode(string input)
    {
        return string.IsNullOrEmpty(input) ? string.Empty : WebUtility.HtmlDecode(input);
    }

    public string TrimLines(string input)
    {
        if (string.IsNullOrEmpty(input)) return string.Empty;
        var lines = input.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
        return string.Join(Environment.NewLine, lines.Select(l => l.Trim()));
    }

    public string RemoveEmptyLines(string input)
    {
        if (string.IsNullOrEmpty(input)) return string.Empty;
        var lines = input.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
        return string.Join(Environment.NewLine, lines.Where(l => !string.IsNullOrWhiteSpace(l)));
    }

    public string RemoveDuplicateLines(string input)
    {
        if (string.IsNullOrEmpty(input)) return string.Empty;
        var lines = input.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
        var seen = new HashSet<string>();
        var result = new List<string>();
        foreach (var line in lines)
        {
            if (seen.Add(line))
            {
                result.Add(line);
            }
        }
        return string.Join(Environment.NewLine, result);
    }

    public string SortLines(string input, bool ascending = true)
    {
        if (string.IsNullOrEmpty(input)) return string.Empty;
        var lines = input.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
        var ordered = ascending ? lines.OrderBy(l => l) : lines.OrderByDescending(l => l);
        return string.Join(Environment.NewLine, ordered);
    }

    public string ToUpperCase(string input)
    {
        return input?.ToUpper(CultureInfo.CurrentCulture) ?? string.Empty;
    }

    public string ToLowerCase(string input)
    {
        return input?.ToLower(CultureInfo.CurrentCulture) ?? string.Empty;
    }

    public string ToTitleCase(string input)
    {
        return string.IsNullOrEmpty(input) ? string.Empty : CultureInfo.CurrentCulture.TextInfo.ToTitleCase(input.ToLower(CultureInfo.CurrentCulture));
    }

    public string EscapeString(string input)
    {
        if (string.IsNullOrEmpty(input)) return string.Empty;
        return input
            .Replace("\\", "\\\\")
            .Replace("\"", "\\\"")
            .Replace("\r", "\\r")
            .Replace("\n", "\\n")
            .Replace("\t", "\\t");
    }

    public string UnescapeString(string input)
    {
        if (string.IsNullOrEmpty(input)) return string.Empty;
        return input
            .Replace("\\n", "\n")
            .Replace("\\r", "\r")
            .Replace("\\t", "\t")
            .Replace("\\\"", "\"")
            .Replace("\\\\", "\\");
    }
}
