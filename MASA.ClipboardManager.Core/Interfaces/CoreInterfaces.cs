using MASA.ClipboardManager.Core.Enums;
using MASA.ClipboardManager.Core.Models;

namespace MASA.ClipboardManager.Core.Interfaces;

public interface IClipboardRepository
{
    Task<ClipboardItem?> GetByIdAsync(string id);
    Task<ClipboardItem?> GetByHashAsync(string contentHash);
    Task<IEnumerable<ClipboardItem>> GetRecentAsync(int limit = 100, int offset = 0);
    Task<IEnumerable<ClipboardItem>> GetFavoritesAsync();
    Task<IEnumerable<ClipboardItem>> GetPinnedAsync();
    Task<IEnumerable<ClipboardItem>> GetByCollectionAsync(string collectionId);
    Task<IEnumerable<ClipboardItem>> SearchAsync(string query, ClipboardContentType? type = null, bool? isFavorite = null, string? collectionId = null, int limit = 100);
    Task<int> AddAsync(ClipboardItem item);
    Task<int> UpdateAsync(ClipboardItem item);
    Task<int> UpdateLastUsedAsync(string id, DateTime lastUsedAt);
    Task<int> ToggleFavoriteAsync(string id);
    Task<int> TogglePinAsync(string id);
    Task<int> SetCollectionAsync(string id, string? collectionId);
    Task<int> DeleteAsync(string id);
    Task<int> ClearAllAsync(bool keepFavorites = true);
    Task<int> CleanupOldItemsAsync(int maxItems, int retentionDays, bool keepFavorites = true);
    Task<int> DeleteExpiredSensitiveItemsAsync();
    Task<int> GetTotalCountAsync();
}

public interface ICollectionRepository
{
    Task<IEnumerable<CollectionItem>> GetAllAsync();
    Task<CollectionItem?> GetByIdAsync(string id);
    Task<int> AddAsync(CollectionItem collection);
    Task<int> UpdateAsync(CollectionItem collection);
    Task<int> DeleteAsync(string id);
}

public interface ISettingsRepository
{
    Task<AppSettings> LoadSettingsAsync();
    Task SaveSettingsAsync(AppSettings settings);
}

public interface IEncryptionService
{
    string Encrypt(string plainText);
    string Decrypt(string cipherText);
    byte[] EncryptBytes(byte[] plainBytes);
    byte[] DecryptBytes(byte[] cipherBytes);
}

public interface ISensitiveDataDetector
{
    (bool IsSensitive, SensitiveDataType Type, string Reason) Detect(string content);
}

public interface IContentTypeDetector
{
    (ClipboardContentType Type, string? Preview) Detect(string? text, string format, IEnumerable<string>? fileList);
}

public interface IDeveloperToolsService
{
    string FormatJson(string input);
    string MinifyJson(string input);
    string UrlEncode(string input);
    string UrlDecode(string input);
    string Base64Encode(string input);
    string Base64Decode(string input);
    string HtmlEncode(string input);
    string HtmlDecode(string input);
    string TrimLines(string input);
    string RemoveEmptyLines(string input);
    string RemoveDuplicateLines(string input);
    string SortLines(string input, bool ascending = true);
    string ToUpperCase(string input);
    string ToLowerCase(string input);
    string ToTitleCase(string input);
    string EscapeString(string input);
    string UnescapeString(string input);
}

public interface IHotKeyService
{
    event EventHandler? HotKeyPressed;
    bool Register(string hotkeyString);
    void Unregister();
}

public interface IStartupService
{
    bool IsAutoStartEnabled();
    void SetAutoStart(bool enable);
}

public interface IPasteSimulator
{
    void SimulatePaste();
}

public interface IClipboardMonitor : IDisposable
{
    event EventHandler<ClipboardItem>? ClipboardChanged;
    bool IsPaused { get; }
    void Start();
    void Stop();
    void Pause();
    void Resume();
}
