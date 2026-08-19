using MASA.ClipboardManager.Core.Enums;
using MASA.ClipboardManager.Core.Interfaces;
using MASA.ClipboardManager.Core.Models;

namespace MASA.ClipboardManager.Application.Services;

public class ClipboardService
{
    private readonly IClipboardRepository _clipboardRepo;
    private readonly IClipboardMonitor _clipboardMonitor;
    private readonly ISettingsRepository _settingsRepo;
    private readonly IEncryptionService _encryptionService;
    private readonly IAppLogger? _logger;

    public event EventHandler<ClipboardItem>? ItemAdded;
    public event EventHandler<string>? ItemDeleted;

    public ClipboardService(
        IClipboardRepository clipboardRepo,
        IClipboardMonitor clipboardMonitor,
        ISettingsRepository settingsRepo,
        IEncryptionService encryptionService,
        IAppLogger? logger = null)
    {
        _clipboardRepo = clipboardRepo;
        _clipboardMonitor = clipboardMonitor;
        _settingsRepo = settingsRepo;
        _encryptionService = encryptionService;
        _logger = logger;

        _clipboardMonitor.ClipboardChanged += OnClipboardChanged;
    }

    public void Start()
    {
        _logger?.LogInfo("Clipboard monitoring service started.");
        _clipboardMonitor.Start();
    }

    public void Stop()
    {
        _logger?.LogInfo("Clipboard monitoring service stopped.");
        _clipboardMonitor.Stop();
    }

    public void TogglePause()
    {
        if (_clipboardMonitor.IsPaused)
        {
            _clipboardMonitor.Resume();
            _logger?.LogInfo("Clipboard monitoring resumed.");
        }
        else
        {
            _clipboardMonitor.Pause();
            _logger?.LogInfo("Clipboard monitoring paused.");
        }
    }

    public bool IsPaused => _clipboardMonitor.IsPaused;

    private async void OnClipboardChanged(object? sender, ClipboardItem item)
    {
        try
        {
            var settings = await _settingsRepo.LoadSettingsAsync();

            // Duplicate detection check
            if (settings.PreventDuplicates && !string.IsNullOrEmpty(item.ContentHash))
            {
                var existing = await _clipboardRepo.GetByHashAsync(item.ContentHash);
                if (existing != null)
                {
                    await _clipboardRepo.UpdateLastUsedAsync(existing.Id, DateTime.UtcNow);
                    return;
                }
            }

            await _clipboardRepo.AddAsync(item);
            _logger?.LogInfo($"Clipboard item added. Type: {item.ContentType}, Size: {item.SizeInBytes} bytes");

            // Enforce max limit & cleanup if enabled
            if (settings.AutoCleanupEnabled)
            {
                await _clipboardRepo.CleanupOldItemsAsync(settings.MaxHistoryItems, settings.RetentionDays, settings.KeepFavoritesOnCleanup);
            }

            ItemAdded?.Invoke(this, item);
        }
        catch (Exception ex)
        {
            _logger?.LogError("Error storing clipboard item in database", ex);
        }
    }

    public async Task<IEnumerable<ClipboardItem>> GetHistoryAsync(int limit = 100, int offset = 0)
    {
        var items = (await _clipboardRepo.GetRecentAsync(limit, offset)).ToList();
        DecryptIfNeeded(items);
        return items;
    }

    public async Task<IEnumerable<ClipboardItem>> GetFavoritesAsync()
    {
        var items = (await _clipboardRepo.GetFavoritesAsync()).ToList();
        DecryptIfNeeded(items);
        return items;
    }

    public async Task<IEnumerable<ClipboardItem>> GetPinnedAsync()
    {
        var items = (await _clipboardRepo.GetPinnedAsync()).ToList();
        DecryptIfNeeded(items);
        return items;
    }

    public async Task<IEnumerable<ClipboardItem>> GetByCollectionAsync(string collectionId)
    {
        var items = (await _clipboardRepo.GetByCollectionAsync(collectionId)).ToList();
        DecryptIfNeeded(items);
        return items;
    }

    public async Task<IEnumerable<ClipboardItem>> SearchAsync(string query, ClipboardContentType? type = null, bool? isFavorite = null, string? collectionId = null, int limit = 100)
    {
        var items = (await _clipboardRepo.SearchAsync(query, type, isFavorite, collectionId, limit)).ToList();
        DecryptIfNeeded(items);
        return items;
    }

    public async Task ToggleFavoriteAsync(string id)
    {
        await _clipboardRepo.ToggleFavoriteAsync(id);
    }

    public async Task TogglePinAsync(string id)
    {
        await _clipboardRepo.TogglePinAsync(id);
    }

    public async Task SetCollectionAsync(string id, string? collectionId)
    {
        await _clipboardRepo.SetCollectionAsync(id, collectionId);
    }

    public async Task DeleteItemAsync(string id)
    {
        await _clipboardRepo.DeleteAsync(id);
        ItemDeleted?.Invoke(this, id);
    }

    public async Task ClearAllAsync(bool keepFavorites = true)
    {
        await _clipboardRepo.ClearAllAsync(keepFavorites);
    }

    public async Task UpdateItemTextAsync(string id, string newText)
    {
        var item = await _clipboardRepo.GetByIdAsync(id);
        if (item != null)
        {
            item.PlainTextContent = newText;
            item.SearchableText = newText;
            item.SizeInBytes = newText.Length * sizeof(char);
            item.Preview = newText.Length > 100 ? newText[..97] + "..." : newText;
            item.EncryptedPayload = _encryptionService.Encrypt(newText);
            item.LastUsedAt = DateTime.UtcNow;

            await _clipboardRepo.UpdateAsync(item);
        }
    }

    private void DecryptIfNeeded(List<ClipboardItem> items)
    {
        foreach (var item in items)
        {
            if (string.IsNullOrEmpty(item.PlainTextContent) && !string.IsNullOrEmpty(item.EncryptedPayload))
            {
                item.PlainTextContent = _encryptionService.Decrypt(item.EncryptedPayload);
            }
        }
    }
}
