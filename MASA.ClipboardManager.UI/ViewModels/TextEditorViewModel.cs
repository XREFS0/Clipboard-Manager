using System.Windows.Input;
using MASA.ClipboardManager.Application.Services;
using MASA.ClipboardManager.Core.Interfaces;
using MASA.ClipboardManager.Core.Models;

namespace MASA.ClipboardManager.UI.ViewModels;

public class TextEditorViewModel : ViewModelBase
{
    private readonly ClipboardService _clipboardService;
    private readonly IDeveloperToolsService _devTools;
    private ClipboardItem? _currentItem;

    private string _editorText = string.Empty;
    private string _findText = string.Empty;
    private string _replaceText = string.Empty;
    private string _statusMessage = "Ready";

    public string EditorText
    {
        get => _editorText;
        set
        {
            if (SetProperty(ref _editorText, value))
            {
                UpdateStats();
            }
        }
    }

    public string FindText
    {
        get => _findText;
        set => SetProperty(ref _findText, value);
    }

    public string ReplaceText
    {
        get => _replaceText;
        set => SetProperty(ref _replaceText, value);
    }

    public string StatusMessage
    {
        get => _statusMessage;
        set => SetProperty(ref _statusMessage, value);
    }

    public ICommand SaveAndCopyCommand { get; }
    public ICommand ReplaceAllCommand { get; }
    public ICommand TrimSpacesCommand { get; }
    public ICommand UpperCaseCommand { get; }
    public ICommand LowerCaseCommand { get; }
    public ICommand RemoveEmptyLinesCommand { get; }
    public ICommand RemoveDuplicatesCommand { get; }
    public ICommand SortLinesCommand { get; }
    public ICommand FormatJsonCommand { get; }

    public event Action? RequestClose;

    public TextEditorViewModel(
        ClipboardService clipboardService,
        IDeveloperToolsService devTools)
    {
        _clipboardService = clipboardService;
        _devTools = devTools;

        SaveAndCopyCommand = new AsyncRelayCommand(ExecuteSaveAndCopyAsync);
        ReplaceAllCommand = new RelayCommand(ExecuteReplaceAll);
        TrimSpacesCommand = new RelayCommand(() => EditorText = _devTools.TrimLines(EditorText));
        UpperCaseCommand = new RelayCommand(() => EditorText = _devTools.ToUpperCase(EditorText));
        LowerCaseCommand = new RelayCommand(() => EditorText = _devTools.ToLowerCase(EditorText));
        RemoveEmptyLinesCommand = new RelayCommand(() => EditorText = _devTools.RemoveEmptyLines(EditorText));
        RemoveDuplicatesCommand = new RelayCommand(() => EditorText = _devTools.RemoveDuplicateLines(EditorText));
        SortLinesCommand = new RelayCommand(() => EditorText = _devTools.SortLines(EditorText, true));
        FormatJsonCommand = new RelayCommand(() => EditorText = _devTools.FormatJson(EditorText));
    }

    public void LoadItem(ClipboardItem item)
    {
        _currentItem = item;
        EditorText = item.PlainTextContent ?? string.Empty;
        UpdateStats();
    }

    private void UpdateStats()
    {
        int lines = EditorText.Split('\n').Length;
        int chars = EditorText.Length;
        StatusMessage = $"Lines: {lines} | Characters: {chars}";
    }

    private void ExecuteReplaceAll()
    {
        if (string.IsNullOrEmpty(FindText)) return;
        EditorText = EditorText.Replace(FindText, ReplaceText ?? string.Empty);
        StatusMessage = "Replaced occurrences";
    }

    private async Task ExecuteSaveAndCopyAsync()
    {
        if (_currentItem != null)
        {
            await _clipboardService.UpdateItemTextAsync(_currentItem.Id, EditorText);
        }

        System.Windows.Clipboard.SetText(EditorText);
        StatusMessage = "Saved and copied to clipboard!";
        RequestClose?.Invoke();
    }
}
