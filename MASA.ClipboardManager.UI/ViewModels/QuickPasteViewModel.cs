using System.Collections.ObjectModel;
using System.Windows.Input;
using MASA.ClipboardManager.Application.Services;
using MASA.ClipboardManager.Core.Enums;
using MASA.ClipboardManager.Core.Interfaces;
using MASA.ClipboardManager.Core.Models;

namespace MASA.ClipboardManager.UI.ViewModels;

public class QuickPasteViewModel : ViewModelBase
{
    private readonly ClipboardService _clipboardService;
    private readonly IPasteSimulator _pasteSimulator;
    private readonly ISettingsRepository _settingsRepo;

    private string _searchQuery = string.Empty;
    private ClipboardItem? _selectedItem;

    public ObservableCollection<ClipboardItem> QuickItems { get; } = new();

    public string SearchQuery
    {
        get => _searchQuery;
        set
        {
            if (SetProperty(ref _searchQuery, value))
            {
                _ = RefreshQuickListAsync();
            }
        }
    }

    public ClipboardItem? SelectedItem
    {
        get => _selectedItem;
        set => SetProperty(ref _selectedItem, value);
    }

    public ICommand PasteSelectedCommand { get; }
    public ICommand PasteByIndexCommand { get; }

    public event Action? RequestClose;

    public QuickPasteViewModel(
        ClipboardService clipboardService,
        IPasteSimulator pasteSimulator,
        ISettingsRepository settingsRepo)
    {
        _clipboardService = clipboardService;
        _pasteSimulator = pasteSimulator;
        _settingsRepo = settingsRepo;

        PasteSelectedCommand = new RelayCommand(ExecutePasteSelected);
        PasteByIndexCommand = new RelayCommand(ExecutePasteByIndex);
    }

    public async Task InitializeAsync()
    {
        SearchQuery = string.Empty;
        await RefreshQuickListAsync();
    }

    public async Task RefreshQuickListAsync()
    {
        IEnumerable<ClipboardItem> items;
        if (string.IsNullOrWhiteSpace(SearchQuery))
        {
            items = await _clipboardService.GetHistoryAsync(15);
        }
        else
        {
            items = await _clipboardService.SearchAsync(SearchQuery, limit: 15);
        }

        QuickItems.Clear();
        foreach (var item in items)
        {
            QuickItems.Add(item);
        }

        SelectedItem = QuickItems.FirstOrDefault();
    }

    private void ExecutePasteSelected()
    {
        if (SelectedItem == null) return;
        PerformPaste(SelectedItem);
    }

    private void ExecutePasteByIndex(object? param)
    {
        if (param is int index && index >= 0 && index < QuickItems.Count)
        {
            PerformPaste(QuickItems[index]);
        }
        else if (param is string strIndex && int.TryParse(strIndex, out var idx) && idx >= 1 && idx <= QuickItems.Count)
        {
            PerformPaste(QuickItems[idx - 1]);
        }
    }

    private async void PerformPaste(ClipboardItem item)
    {
        try
        {
            if (item.ContentType == ClipboardContentType.Image && !string.IsNullOrEmpty(item.ImageStoragePath) && System.IO.File.Exists(item.ImageStoragePath))
            {
                var bitmap = new System.Windows.Media.Imaging.BitmapImage(new Uri(item.ImageStoragePath));
                System.Windows.Clipboard.SetImage(bitmap);
            }
            else if (!string.IsNullOrEmpty(item.PlainTextContent))
            {
                System.Windows.Clipboard.SetText(item.PlainTextContent);
            }

            var settings = await _settingsRepo.LoadSettingsAsync();

            RequestClose?.Invoke();

            if (settings.PasteImmediatelyOnEnter)
            {
                _pasteSimulator.SimulatePaste();
            }
        }
        catch
        {
            // Suppress clipboard lock or paste simulation error
        }
    }
}
