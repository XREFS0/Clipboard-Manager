using MASA.ClipboardManager.Core.Enums;

namespace MASA.ClipboardManager.Core.Models;

public class ClipboardItem
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public ClipboardContentType ContentType { get; set; } = ClipboardContentType.Text;
    public string? ContentFormat { get; set; } // "Text", "RichText", "HTML", "Bitmap", "FileDrop"
    public string? PlainTextContent { get; set; }
    public string? EncryptedPayload { get; set; }
    public string? ContentHash { get; set; }
    public string? Preview { get; set; }
    public string? SearchableText { get; set; }
    public long SizeInBytes { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime LastUsedAt { get; set; } = DateTime.UtcNow;
    public bool IsFavorite { get; set; }
    public bool IsPinned { get; set; }
    public bool IsSensitive { get; set; }
    public SensitiveDataType SensitiveType { get; set; } = SensitiveDataType.None;
    public DateTime? AutoDeleteAt { get; set; }
    public string? CollectionId { get; set; }
    public string? ImageStoragePath { get; set; }
    public string? SourceProcessName { get; set; }
    public int FileCount { get; set; }
    public string? FileExtension { get; set; }
}

public class CollectionItem
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;
    public string ColorHex { get; set; } = "#3B82F6";
    public string Icon { get; set; } = "📁";
    public int SortOrder { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public int ItemCount { get; set; }
}

public class AppSettings
{
    public int MaxHistoryItems { get; set; } = 500;
    public int RetentionDays { get; set; } = 30;
    public bool AutoCleanupEnabled { get; set; } = true;
    public bool PreventDuplicates { get; set; } = true;
    public bool KeepFavoritesOnCleanup { get; set; } = true;
    public bool PrivacyMode { get; set; } = false;
    public bool DoNotStoreSensitiveData { get; set; } = true;
    public bool AutoDeleteSensitiveData { get; set; } = true;
    public int SensitiveAutoDeleteMinutes { get; set; } = 5;
    public bool EnableEncryption { get; set; } = true;
    public bool StartWithWindows { get; set; } = false;
    public bool StartMinimized { get; set; } = false;
    public bool CloseToTray { get; set; } = true;
    public bool ClipboardMonitoringEnabled { get; set; } = true;
    public bool PasteImmediatelyOnEnter { get; set; } = true;
    public bool ClearSearchAfterPaste { get; set; } = true;
    public ThemeMode Theme { get; set; } = ThemeMode.Dark;
    public string GlobalHotKey { get; set; } = "Ctrl+Shift+V";
    public string QuickPasteHotKey { get; set; } = "Ctrl+Shift+V";
    public List<string> ExcludedApplications { get; set; } = new()
    {
        "1Password.exe",
        "KeePass.exe",
        "Bitwarden.exe",
        "LastPass.exe",
        "KeePassXC.exe"
    };
}
