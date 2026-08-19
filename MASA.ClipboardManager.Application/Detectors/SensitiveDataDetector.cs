using System.Text.RegularExpressions;
using MASA.ClipboardManager.Core.Enums;
using MASA.ClipboardManager.Core.Interfaces;

namespace MASA.ClipboardManager.Application.Detectors;

public class SensitiveDataDetector : ISensitiveDataDetector
{
    // API Keys regexes
    private static readonly Regex GitHubTokenRegex = new(@"\bgh[pousr]_[A-Za-z0-9_]{36,255}\b", RegexOptions.Compiled);
    private static readonly Regex OpenAiKeyRegex = new(@"\bsk-[a-zA-Z0-9]{20,T3BlbkFJ[a-zA-Z0-9]{20,}\b|\bsk-proj-[a-zA-Z0-9_-]{40,}\b", RegexOptions.Compiled);
    private static readonly Regex GoogleApiKeyRegex = new(@"\bAIza[0-9A-Za-z-_]{35}\b", RegexOptions.Compiled);
    private static readonly Regex AwsAccessKeyRegex = new(@"\bAKIA[0-9A-Z]{16}\b", RegexOptions.Compiled);
    private static readonly Regex StripeKeyRegex = new(@"\b(sk|pk)_(test|live)_[0-9a-zA-Z]{24,}\b", RegexOptions.Compiled);
    private static readonly Regex SlackTokenRegex = new(@"\bxox[baprs]-[0-9a-zA-Z]{10,48}\b", RegexOptions.Compiled);
    
    // JWT Token regex
    private static readonly Regex JwtRegex = new(@"\beyJ[A-Za-z0-9-_]+\.eyJ[A-Za-z0-9-_]+\.[A-Za-z0-9-_]+\b", RegexOptions.Compiled);
    
    // Credit Card regex (Digits with optional spaces/dashes)
    private static readonly Regex CreditCardCandidateRegex = new(@"(?:\b\d{4}[ -]?\d{4}[ -]?\d{4}[ -]?\d{4}\b|\b\d{13,19}\b)", RegexOptions.Compiled);

    // OTP / Auth Code regex
    private static readonly Regex OtpRegex = new(@"\b(?:code|otp|auth|pin|verification)\s*(?:is|:|=)?\s*([0-9]{4,8})\b", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    // Private Keys
    private static readonly Regex PrivateKeyRegex = new(@"-----BEGIN (RSA|EC|DSA|OPENSSH|PRIVATE) KEY-----", RegexOptions.Compiled);

    public (bool IsSensitive, SensitiveDataType Type, string Reason) Detect(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            return (false, SensitiveDataType.None, string.Empty);
        }

        var trimmed = content.Trim();

        // 1. Check Private Keys
        if (PrivateKeyRegex.IsMatch(trimmed))
        {
            return (true, SensitiveDataType.PrivateToken, "Private Cryptographic Key detected");
        }

        // 2. Check API Keys
        if (GitHubTokenRegex.IsMatch(trimmed))
        {
            return (true, SensitiveDataType.ApiKey, "GitHub Access Token detected");
        }
        if (OpenAiKeyRegex.IsMatch(trimmed))
        {
            return (true, SensitiveDataType.ApiKey, "OpenAI API Key detected");
        }
        if (GoogleApiKeyRegex.IsMatch(trimmed))
        {
            return (true, SensitiveDataType.ApiKey, "Google API Key detected");
        }
        if (AwsAccessKeyRegex.IsMatch(trimmed))
        {
            return (true, SensitiveDataType.ApiKey, "AWS Access Key detected");
        }
        if (StripeKeyRegex.IsMatch(trimmed))
        {
            return (true, SensitiveDataType.ApiKey, "Stripe API Key detected");
        }
        if (SlackTokenRegex.IsMatch(trimmed))
        {
            return (true, SensitiveDataType.ApiKey, "Slack Token detected");
        }

        // 3. Check JWT
        if (JwtRegex.IsMatch(trimmed))
        {
            return (true, SensitiveDataType.JwtToken, "JSON Web Token (JWT) detected");
        }

        // 4. Check Credit Card with Luhn validation
        var digitsOnly = Regex.Replace(trimmed, @"[\s-]", "");
        if (digitsOnly.Length >= 13 && digitsOnly.Length <= 19 && digitsOnly.All(char.IsDigit) && IsValidLuhn(digitsOnly))
        {
            return (true, SensitiveDataType.CreditCard, "Credit Card Number detected");
        }

        // 5. Check OTP
        if (OtpRegex.IsMatch(trimmed))
        {
            return (true, SensitiveDataType.AuthCode, "One-Time Password (OTP) detected");
        }

        // 6. Check High Entropy / Password candidate (single line, 12-64 chars, mixed casing, numbers, symbols)
        if (!trimmed.Contains('\n') && !trimmed.Contains(' ') && trimmed.Length >= 12 && trimmed.Length <= 64)
        {
            if (IsPasswordLike(trimmed))
            {
                return (true, SensitiveDataType.Password, "High-entropy password/secret pattern detected");
            }
        }

        return (false, SensitiveDataType.None, string.Empty);
    }

    private static bool IsValidLuhn(string digits)
    {
        int sum = 0;
        bool alternate = false;
        for (int i = digits.Length - 1; i >= 0; i--)
        {
            if (!char.IsDigit(digits[i])) return false;
            int n = digits[i] - '0';
            if (alternate)
            {
                n *= 2;
                if (n > 9)
                {
                    n -= 9;
                }
            }
            sum += n;
            alternate = !alternate;
        }
        return sum > 0 && sum % 10 == 0;
    }

    private static bool IsPasswordLike(string str)
    {
        bool hasUpper = str.Any(char.IsUpper);
        bool hasLower = str.Any(char.IsLower);
        bool hasDigit = str.Any(char.IsDigit);
        bool hasSymbol = str.Any(c => !char.IsLetterOrDigit(c));

        int criteria = (hasUpper ? 1 : 0) + (hasLower ? 1 : 0) + (hasDigit ? 1 : 0) + (hasSymbol ? 1 : 0);
        return criteria >= 3 && CalculateShannonEntropy(str) > 3.2;
    }

    private static double CalculateShannonEntropy(string s)
    {
        var map = new Dictionary<char, int>();
        foreach (char c in s)
        {
            if (!map.ContainsKey(c))
                map[c] = 1;
            else
                map[c]++;
        }

        double entropy = 0.0;
        int len = s.Length;
        foreach (var pair in map)
        {
            double p = (double)pair.Value / len;
            entropy -= p * Math.Log2(p);
        }
        return entropy;
    }
}
