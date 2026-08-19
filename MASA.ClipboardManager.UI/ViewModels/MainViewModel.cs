using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Input;
using MASA.ClipboardManager.Application.Services;
using MASA.ClipboardManager.Core.Enums;
using MASA.ClipboardManager.Core.Interfaces;
using MASA.ClipboardManager.Core.Models;

namespace MASA.ClipboardManager.UI.ViewModels;

public class MainViewModel : ViewModelBase
{
    private readonly ClipboardService _clipboardService;
    private readonly ICollectionRepository _collectionRepo;
    private readonly ISettingsRepository _settingsRepo;
    private readonly IPasteSimulator _pasteSimulator;
    private readonly IDeveloperToolsService _devTools;

    private string _searchQuery = string.Empty;
    private ClipboardContentType? _selectedTypeFilter;
    private CollectionItem? _selectedCollection;
    private ClipboardItem? _selectedItem;
    private bool _showFavoritesOnly;
    private bool _showPinnedOnly;
    private bool _isMonitoringActive = true;
    private bool _isPrivacyMode = false;
    private int _totalItemCount;
    private string _statusMessage = "Ready";

    public ObservableCollection<ClipboardItem> FilteredItems { get; } = new();
    public ObservableCollection<CollectionItem> Collections { get; } = new();

    public string SearchQuery
    {
        get => _searchQuery;
        set
        {
            if (SetProperty(ref _searchQuery, value))
            {
                _ = RefreshItemsAsync();
            }
        }
    }

    public ClipboardContentType? SelectedTypeFilter
    {
        get => _selectedTypeFilter;
        set
        {
            if (SetProperty(ref _selectedTypeFilter, value))
            {
                _ = RefreshItemsAsync();
            }
        }
    }

    public CollectionItem? SelectedCollection
    {
        get => _selectedCollection;
        set
        {
            if (SetProperty(ref _selectedCollection, value))
            {
                _ = RefreshItemsAsync();
            }
        }
    }

    public ClipboardItem? SelectedItem
    {
        get => _selectedItem;
        set => SetProperty(ref _selectedItem, value);
    }

    public bool ShowFavoritesOnly
    {
        get => _showFavoritesOnly;
        set
        {
            if (SetProperty(ref _showFavoritesOnly, value))
            {
                _ = RefreshItemsAsync();
            }
        }
    }

    public bool ShowPinnedOnly
    {
        get => _showPinnedOnly;
        set
        {
            if (SetProperty(ref _showPinnedOnly, value))
            {
                _ = RefreshItemsAsync();
            }
        }
    }

    public bool IsMonitoringActive
    {
        get => _isMonitoringActive;
        set => SetProperty(ref _isMonitoringActive, value);
    }

    public bool IsPrivacyMode
    {
        get => _isPrivacyMode;
        set => SetProperty(ref _isPrivacyMode, value);
    }

    public int TotalItemCount
    {
        get => _totalItemCount;
        set => SetProperty(ref _totalItemCount, value);
    }

    public string StatusMessage
    {
        get => _statusMessage;
        set => SetProperty(ref _statusMessage, value);
    }

    // Commands
    public ICommand SearchCommand { get; }
    public ICommand CopyItemCommand { get; }
    public ICommand PasteItemCommand { get; }
    public ICommand DeleteItemCommand { get; }
    public ICommand ToggleFavoriteCommand { get; }
    public ICommand TogglePinCommand { get; }
    public ICommand ClearAllCommand { get; }
    public ICommand ToggleMonitoringCommand { get; }
    public ICommand TogglePrivacyModeCommand { get; }
    public ICommand OpenFileCommand { get; }
    public ICommand OpenFolderCommand { get; }
    public ICommand OpenUrlCommand { get; }
    public ICommand RefreshCommand { get; }
    public ICommand FilterByTypeCommand { get; }
    public ICommand AddCollectionCommand { get; }
    public ICommand AssignCollectionCommand { get; }

    public MainViewModel(
        ClipboardService clipboardService,
        ICollectionRepository collectionRepo,
        ISettingsRepository settingsRepo,
        IPasteSimulator pasteSimulator,
        IDeveloperToolsService devTools)
    {
        _clipboardService = clipboardService;
        _collectionRepo = collectionRepo;
        _settingsRepo = settingsRepo;
        _pasteSimulator = pasteSimulator;
        _devTools = devTools;

        _clipboardService.ItemAdded += (s, item) =>
        {
            System.Windows.Application.Current?.Dispatcher?.Invoke(() =>
            {
                _ = RefreshItemsAsync();
            });
        };

        _clipboardService.ItemDeleted += (s, id) =>
        {
            System.Windows.Application.Current?.Dispatcher?.Invoke(() =>
            {
                var existing = FilteredItems.FirstOrDefault(i => i.Id == id);
                if (existing != null) FilteredItems.Remove(existing);
            });
        };

        SearchCommand = new AsyncRelayCommand(RefreshItemsAsync);
        CopyItemCommand = new RelayCommand(ExecuteCopyItem);
        PasteItemCommand = new RelayCommand(ExecutePasteItem);
        DeleteItemCommand = new AsyncRelayCommand(ExecuteDeleteItemAsync);
        ToggleFavoriteCommand = new AsyncRelayCommand(ExecuteToggleFavoriteAsync);
        TogglePinCommand = new AsyncRelayCommand(ExecuteTogglePinAsync);
        ClearAllCommand = new AsyncRelayCommand(ExecuteClearAllAsync);
        ToggleMonitoringCommand = new RelayCommand(ExecuteToggleMonitoring);
        TogglePrivacyModeCommand = new AsyncRelayCommand(ExecuteTogglePrivacyModeAsync);
        OpenFileCommand = new RelayCommand(ExecuteOpenFile);
        OpenFolderCommand = new RelayCommand(ExecuteOpenFolder);
        OpenUrlCommand = new RelayCommand(ExecuteOpenUrl);
        RefreshCommand = new AsyncRelayCommand(RefreshAllAsync);
        FilterByTypeCommand = new RelayCommand(ExecuteFilterByType);
        AddCollectionCommand = new AsyncRelayCommand(ExecuteAddCollectionAsync);
        AssignCollectionCommand = new AsyncRelayCommand(ExecuteAssignCollectionAsync);
    }

    public async Task InitializeAsync()
    {
        var settings = await _settingsRepo.LoadSettingsAsync();
        IsMonitoringActive = settings.ClipboardMonitoringEnabled;
        IsPrivacyMode = settings.PrivacyMode;

        await LoadCollectionsAsync();
        await RefreshItemsAsync();
    }

    public async Task RefreshAllAsync()
    {
        await LoadCollectionsAsync();
        await RefreshItemsAsync();
    }

    public async Task LoadCollectionsAsync()
    {
        var collections = await _collectionRepo.GetAllAsync();
        Collections.Clear();
        foreach (var c in collections)
        {
            Collections.Add(c);
        }
    }

    public async Task RefreshItemsAsync()
    {
        IEnumerable<ClipboardItem> items;

        if (ShowPinnedOnly)
        {
            items = await _clipboardService.GetPinnedAsync();
        }
        else if (ShowFavoritesOnly)
        {
            items = await _clipboardService.GetFavoritesAsync();
        }
        else if (SelectedCollection != null)
        {
            items = await _clipboardService.GetByCollectionAsync(SelectedCollection.Id);
        }
        else if (!string.IsNullOrWhiteSpace(SearchQuery) || SelectedTypeFilter.HasValue)
        {
            items = await _clipboardService.SearchAsync(SearchQuery, SelectedTypeFilter);
        }
        else
        {
            items = await _clipboardService.GetHistoryAsync(200);
        }

        FilteredItems.Clear();
        foreach (var item in items)
        {
            FilteredItems.Add(item);
        }

        TotalItemCount = FilteredItems.Count;
        StatusMessage = $"{TotalItemCount} items loaded";
    }

    private void ExecuteCopyItem(object? param)
    {
        var item = param as ClipboardItem ?? SelectedItem;
        if (item == null) return;

        try
        {
            if (item.ContentType == ClipboardContentType.Image && !string.IsNullOrEmpty(item.ImageStoragePath) && File.Exists(item.ImageStoragePath))
            {
                var bitmap = new System.Windows.Media.Imaging.BitmapImage(new Uri(item.ImageStoragePath));
                System.Windows.Clipboard.SetImage(bitmap);
            }
            else if ((item.ContentType == ClipboardContentType.File || item.ContentType == ClipboardContentType.Folder) && !string.IsNullOrEmpty(item.PlainTextContent))
            {
                var paths = item.PlainTextContent.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);
                var stringCollection = new System.Collections.Specialized.StringCollection();
                stringCollection.AddRange(paths);
                System.Windows.Clipboard.SetFileDropList(stringCollection);
            }
            else if (!string.IsNullOrEmpty(item.PlainTextContent))
            {
                System.Windows.Clipboard.SetText(item.PlainTextContent);
            }

            StatusMessage = "Copied to clipboard";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Copy failed: {ex.Message}";
        }
    }

    private void ExecutePasteItem(object? param)
    {
        ExecuteCopyItem(param);
        _pasteSimulator.SimulatePaste();
        StatusMessage = "Pasted to active window";
    }

    private async Task ExecuteDeleteItemAsync(object? param)
    {
        var item = param as ClipboardItem ?? SelectedItem;
        if (item == null) return;

        await _clipboardService.DeleteItemAsync(item.Id);
        FilteredItems.Remove(item);
        TotalItemCount = FilteredItems.Count;
        StatusMessage = "Item deleted";
    }

    private async Task ExecuteToggleFavoriteAsync(object? param)
    {
        var item = param as ClipboardItem ?? SelectedItem;
        if (item == null) return;

        item.IsFavorite = !item.IsFavorite;
        await _clipboardService.ToggleFavoriteAsync(item.Id);
        StatusMessage = item.IsFavorite ? "Added to favorites" : "Removed from favorites";
    }

    private async Task ExecuteTogglePinAsync(object? param)
    {
        var item = param as ClipboardItem ?? SelectedItem;
        if (item == null) return;

        item.IsPinned = !item.IsPinned;
        await _clipboardService.TogglePinAsync(item.Id);
        await RefreshItemsAsync();
    }

    private async Task ExecuteClearAllAsync()
    {
        if (System.Windows.MessageBox.Show("Are you sure you want to clear clipboard history? (Favorites and Pinned items will be preserved)", "Clear History", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
        {
            await _clipboardService.ClearAllAsync(keepFavorites: true);
            await RefreshItemsAsync();
            StatusMessage = "History cleared";
        }
    }

    private void ExecuteToggleMonitoring()
    {
        _clipboardService.TogglePause();
        IsMonitoringActive = !_clipboardService.IsPaused;
        StatusMessage = IsMonitoringActive ? "Monitoring Active" : "Monitoring Paused";
    }

    private async Task ExecuteTogglePrivacyModeAsync()
    {
        var settings = await _settingsRepo.LoadSettingsAsync();
        settings.PrivacyMode = !settings.PrivacyMode;
        await _settingsRepo.SaveSettingsAsync(settings);

        IsPrivacyMode = settings.PrivacyMode;
        StatusMessage = IsPrivacyMode ? "Privacy Mode ENABLED (Recording stopped)" : "Privacy Mode DISABLED";
    }

    private void ExecuteOpenFile(object? param)
    {
        var item = param as ClipboardItem ?? SelectedItem;
        if (item == null || string.IsNullOrWhiteSpace(item.PlainTextContent)) return;

        var firstPath = item.PlainTextContent.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
        if (firstPath != null && (File.Exists(firstPath) || Directory.Exists(firstPath)))
        {
            Process.Start(new ProcessStartInfo { FileName = firstPath, UseShellExecute = true });
        }
    }

    private void ExecuteOpenFolder(object? param)
    {
        var item = param as ClipboardItem ?? SelectedItem;
        if (item == null || string.IsNullOrWhiteSpace(item.PlainTextContent)) return;

        var firstPath = item.PlainTextContent.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
        if (firstPath != null)
        {
            var folder = Directory.Exists(firstPath) ? firstPath : Path.GetDirectoryName(firstPath);
            if (folder != null && Directory.Exists(folder))
            {
                Process.Start(new ProcessStartInfo { FileName = folder, UseShellExecute = true });
            }
        }
    }

    private void ExecuteOpenUrl(object? param)
    {
        var item = param as ClipboardItem ?? SelectedItem;
        if (item == null || string.IsNullOrWhiteSpace(item.PlainTextContent)) return;

        var url = item.PlainTextContent.Trim();
        if (url.StartsWith("http://") || url.StartsWith("https://"))
        {
            Process.Start(new ProcessStartInfo { FileName = url, UseShellExecute = true });
        }
    }

    private void ExecuteFilterByType(object? param)
    {
        if (param is string typeStr)
        {
            if (typeStr == "All")
            {
                SelectedTypeFilter = null;
                ShowFavoritesOnly = false;
                ShowPinnedOnly = false;
                SelectedCollection = null;
            }
            else if (typeStr == "Favorites")
            {
                ShowFavoritesOnly = true;
                ShowPinnedOnly = false;
                SelectedTypeFilter = null;
                SelectedCollection = null;
            }
            else if (typeStr == "Pinned")
            {
                ShowPinnedOnly = true;
                ShowFavoritesOnly = false;
                SelectedTypeFilter = null;
                SelectedCollection = null;
            }
            else if (Enum.TryParse<ClipboardContentType>(typeStr, true, out var type))
            {
                SelectedTypeFilter = type;
                ShowFavoritesOnly = false;
                ShowPinnedOnly = false;
                SelectedCollection = null;
            }
        }
    }

    private async Task ExecuteAddCollectionAsync(object? param)
    {
        var name = param as string ?? "New Collection";
        var col = new CollectionItem
        {
            Name = name,
            ColorHex = "#6366F1",
            Icon = "📁"
        };
        await _collectionRepo.AddAsync(col);
        await LoadCollectionsAsync();
    }

    private async Task ExecuteAssignCollectionAsync(object? param)
    {
        if (SelectedItem != null && param is string collectionId)
        {
            await _clipboardService.SetCollectionAsync(SelectedItem.Id, collectionId);
            SelectedItem.CollectionId = collectionId;
            StatusMessage = "Moved to collection";
        }
    }
}
