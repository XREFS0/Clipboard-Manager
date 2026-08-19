using System.Windows;
using System.Windows.Input;
using MASA.ClipboardManager.UI.ViewModels;

namespace MASA.ClipboardManager.UI.Views;

public partial class QuickPasteWindow : Window
{
    private readonly QuickPasteViewModel _viewModel;

    public QuickPasteWindow(QuickPasteViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = _viewModel;

        _viewModel.RequestClose += () => Hide();
        Loaded += async (s, e) =>
        {
            await _viewModel.InitializeAsync();
            QuickSearchBox.Focus();
        };
    }

    public async void ShowAtCursor()
    {
        await _viewModel.InitializeAsync();
        Show();
        Activate();
        QuickSearchBox.Focus();
    }

    private void Window_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            Hide();
            e.Handled = true;
        }
        else if (e.Key == Key.Enter)
        {
            _viewModel.PasteSelectedCommand.Execute(null);
            e.Handled = true;
        }
        else if (e.Key >= Key.D1 && e.Key <= Key.D9 && (Keyboard.Modifiers == ModifierKeys.None || Keyboard.Modifiers == ModifierKeys.Alt))
        {
            int index = e.Key - Key.D1;
            _viewModel.PasteByIndexCommand.Execute(index);
            e.Handled = true;
        }
    }
}
