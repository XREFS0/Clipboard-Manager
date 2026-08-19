using MASA.ClipboardManager.Application.Detectors;
using MASA.ClipboardManager.Application.Services;
using MASA.ClipboardManager.Core.Enums;
using MASA.ClipboardManager.Infrastructure.Encryption;
using Xunit;

namespace MASA.ClipboardManager.Tests;

public class ContentTypeDetectorTests
{
    private readonly ContentTypeDetector _detector = new();

    [Theory]
    [InlineData("https://github.com/microsoft/terminal", ClipboardContentType.URL)]
    [InlineData("http://localhost:5000/api/v1/health", ClipboardContentType.URL)]
    [InlineData("user@company.com", ClipboardContentType.Email)]
    [InlineData("public static void Main(string[] args) { Console.WriteLine(\"Hello\"); }", ClipboardContentType.Code)]
    [InlineData("function calculateTotal(items) { return items.reduce((a, b) => a + b, 0); }", ClipboardContentType.Code)]
    [InlineData("{\"name\": \"MASA\", \"role\": \"Manager\"}", ClipboardContentType.Code)]
    [InlineData("<div class=\"container\"><p>Hello World</p></div>", ClipboardContentType.HTML)]
    [InlineData("Just a casual plain text message for my friend.", ClipboardContentType.Text)]
    public void Detect_TextContent_ReturnsAccurateType(string text, ClipboardContentType expectedType)
    {
        var (type, _) = _detector.Detect(text, "Text", null);
        Assert.Equal(expectedType, type);
    }

    [Fact]
    public void Detect_FileDrop_ReturnsFileOrFolder()
    {
        var (type, _) = _detector.Detect(null, "FileDrop", new[] { "C:\\Windows\\notepad.exe" });
        Assert.Equal(ClipboardContentType.File, type);
    }
}

public class SensitiveDataDetectorTests
{
    private readonly SensitiveDataDetector _detector = new();

    [Fact]
    public void Detect_GitHubToken_IdentifiedAsApiKey()
    {
        var token = "ghp_1234567890abcdefghijklmnopqrstuvwxyzAB";
        var (isSensitive, type, _) = _detector.Detect(token);

        Assert.True(isSensitive);
        Assert.Equal(SensitiveDataType.ApiKey, type);
    }

    [Fact]
    public void Detect_OpenAiApiKey_IdentifiedAsApiKey()
    {
        var key = "sk-proj-12345678901234567890123456789012345678901234";
        var (isSensitive, type, _) = _detector.Detect(key);

        Assert.True(isSensitive);
        Assert.Equal(SensitiveDataType.ApiKey, type);
    }

    [Fact]
    public void Detect_JwtToken_IdentifiedAsJwt()
    {
        var jwt = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxMjM0NTY3ODkwIiwibmFtZSI6Ik1hc2EiLCJpYXQiOjE1MTYyMzkwMjJ9.4zU5e9...";
        var (isSensitive, type, _) = _detector.Detect(jwt);

        Assert.True(isSensitive);
        Assert.Equal(SensitiveDataType.JwtToken, type);
    }

    [Fact]
    public void Detect_LuhnValidCreditCard_IdentifiedAsCreditCard()
    {
        // 16-digit standard Visa test number with valid Luhn: 4012 8888 8888 1881
        var card = "4012 8888 8888 1881";
        var (isSensitive, type, _) = _detector.Detect(card);

        Assert.True(isSensitive);
        Assert.Equal(SensitiveDataType.CreditCard, type);
    }

    [Fact]
    public void Detect_NormalText_ReturnsNotSensitive()
    {
        var text = "Meeting tomorrow at 10 AM to discuss Q3 roadmap and feature milestones.";
        var (isSensitive, type, _) = _detector.Detect(text);

        Assert.False(isSensitive);
        Assert.Equal(SensitiveDataType.None, type);
    }
}

public class DeveloperToolsServiceTests
{
    private readonly DeveloperToolsService _devTools = new();

    [Fact]
    public void FormatAndMinifyJson_WorksCorrectly()
    {
        var minified = "{\"name\":\"MASA\",\"version\":1}";
        var formatted = _devTools.FormatJson(minified);

        Assert.Contains("\n", formatted);
        Assert.Contains("\"name\": \"MASA\"", formatted);

        var reminified = _devTools.MinifyJson(formatted);
        Assert.Equal(minified, reminified);
    }

    [Fact]
    public void Base64_EncodeAndDecode_WorksCorrectly()
    {
        var text = "MASA Clipboard Manager 2026";
        var encoded = _devTools.Base64Encode(text);
        var decoded = _devTools.Base64Decode(encoded);

        Assert.Equal(text, decoded);
    }

    [Fact]
    public void RemoveDuplicateLines_RemovesDuplicates()
    {
        var input = "Apple\r\nBanana\r\nApple\r\nOrange\r\nBanana";
        var result = _devTools.RemoveDuplicateLines(input);

        Assert.Equal("Apple\r\nBanana\r\nOrange", result);
    }

    [Fact]
    public void SortLines_SortsAscendingAndDescending()
    {
        var input = "Zebra\r\nApple\r\nMonkey";
        var asc = _devTools.SortLines(input, true);
        var desc = _devTools.SortLines(input, false);

        Assert.Equal("Apple\r\nMonkey\r\nZebra", asc);
        Assert.Equal("Zebra\r\nMonkey\r\nApple", desc);
    }
}

public class EncryptionTests
{
    [Fact]
    public void DPAPI_EncryptAndDecrypt_ReturnsOriginalText()
    {
        var service = new DPAPIEncryptionService();
        var plain = "Secret credentials that must be protected in local DB";

        var encrypted = service.Encrypt(plain);
        Assert.NotEqual(plain, encrypted);

        var decrypted = service.Decrypt(encrypted);
        Assert.Equal(plain, decrypted);
    }
}
