using System.Windows.Input;
using MASA.ClipboardManager.Core.Interfaces;

namespace MASA.ClipboardManager.UI.ViewModels;

public class DevToolsViewModel : ViewModelBase
{
    private readonly IDeveloperToolsService _devTools;
    private string _inputText = string.Empty;
    private string _outputText = string.Empty;
    private string _statusMessage = "Select a tool to transform text";

    public string InputText
    {
        get => _inputText;
        set => SetProperty(ref _inputText, value);
    }

    public string OutputText
    {
        get => _outputText;
        set => SetProperty(ref _outputText, value);
    }

    public string StatusMessage
    {
        get => _statusMessage;
        set => SetProperty(ref _statusMessage, value);
    }

    public ICommand FormatJsonCommand { get; }
    public ICommand MinifyJsonCommand { get; }
    public ICommand UrlEncodeCommand { get; }
    public ICommand UrlDecodeCommand { get; }
    public ICommand Base64EncodeCommand { get; }
    public ICommand Base64DecodeCommand { get; }
    public ICommand HtmlEncodeCommand { get; }
    public ICommand HtmlDecodeCommand { get; }
    public ICommand TrimLinesCommand { get; }
    public ICommand RemoveEmptyLinesCommand { get; }
    public ICommand RemoveDuplicatesCommand { get; }
    public ICommand SortLinesAscCommand { get; }
    public ICommand SortLinesDescCommand { get; }
    public ICommand UpperCaseCommand { get; }
    public ICommand LowerCaseCommand { get; }
    public ICommand TitleCaseCommand { get; }
    public ICommand EscapeCommand { get; }
    public ICommand UnescapeCommand { get; }
    public ICommand CopyOutputCommand { get; }
    public ICommand SwapInputOutputCommand { get; }
    public ICommand ClearAllCommand { get; }

    public DevToolsViewModel(IDeveloperToolsService devTools)
    {
        _devTools = devTools;

        FormatJsonCommand = new RelayCommand(() => RunTool("JSON Formatted", () => _devTools.FormatJson(InputText)));
        MinifyJsonCommand = new RelayCommand(() => RunTool("JSON Minified", () => _devTools.MinifyJson(InputText)));
        UrlEncodeCommand = new RelayCommand(() => RunTool("URL Encoded", () => _devTools.UrlEncode(InputText)));
        UrlDecodeCommand = new RelayCommand(() => RunTool("URL Decoded", () => _devTools.UrlDecode(InputText)));
        Base64EncodeCommand = new RelayCommand(() => RunTool("Base64 Encoded", () => _devTools.Base64Encode(InputText)));
        Base64DecodeCommand = new RelayCommand(() => RunTool("Base64 Decoded", () => _devTools.Base64Decode(InputText)));
        HtmlEncodeCommand = new RelayCommand(() => RunTool("HTML Encoded", () => _devTools.HtmlEncode(InputText)));
        HtmlDecodeCommand = new RelayCommand(() => RunTool("HTML Decoded", () => _devTools.HtmlDecode(InputText)));
        TrimLinesCommand = new RelayCommand(() => RunTool("Lines Trimmed", () => _devTools.TrimLines(InputText)));
        RemoveEmptyLinesCommand = new RelayCommand(() => RunTool("Empty Lines Removed", () => _devTools.RemoveEmptyLines(InputText)));
        RemoveDuplicatesCommand = new RelayCommand(() => RunTool("Duplicate Lines Removed", () => _devTools.RemoveDuplicateLines(InputText)));
        SortLinesAscCommand = new RelayCommand(() => RunTool("Lines Sorted Ascending", () => _devTools.SortLines(InputText, true)));
        SortLinesDescCommand = new RelayCommand(() => RunTool("Lines Sorted Descending", () => _devTools.SortLines(InputText, false)));
        UpperCaseCommand = new RelayCommand(() => RunTool("Converted to Uppercase", () => _devTools.ToUpperCase(InputText)));
        LowerCaseCommand = new RelayCommand(() => RunTool("Converted to Lowercase", () => _devTools.ToLowerCase(InputText)));
        TitleCaseCommand = new RelayCommand(() => RunTool("Converted to Title Case", () => _devTools.ToTitleCase(InputText)));
        EscapeCommand = new RelayCommand(() => RunTool("Escaped String", () => _devTools.EscapeString(InputText)));
        UnescapeCommand = new RelayCommand(() => RunTool("Unescaped String", () => _devTools.UnescapeString(InputText)));

        CopyOutputCommand = new RelayCommand(() =>
        {
            if (!string.IsNullOrEmpty(OutputText))
            {
                System.Windows.Clipboard.SetText(OutputText);
                StatusMessage = "Output copied to clipboard!";
            }
        });

        SwapInputOutputCommand = new RelayCommand(() =>
        {
            var temp = InputText;
            InputText = OutputText;
            OutputText = temp;
            StatusMessage = "Swapped Input & Output";
        });

        ClearAllCommand = new RelayCommand(() =>
        {
            InputText = string.Empty;
            OutputText = string.Empty;
            StatusMessage = "Cleared";
        });
    }

    private void RunTool(string actionName, Func<string> transform)
    {
        try
        {
            OutputText = transform();
            StatusMessage = $"Success: {actionName}";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error: {ex.Message}";
        }
    }
}
