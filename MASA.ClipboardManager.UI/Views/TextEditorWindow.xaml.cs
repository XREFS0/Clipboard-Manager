using System.Windows;
using MASA.ClipboardManager.UI.ViewModels;

namespace MASA.ClipboardManager.UI.Views;

public partial class TextEditorWindow : Window
{
    private readonly TextEditorViewModel _viewModel;

    public TextEditorWindow(TextEditorViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = _viewModel;
        _viewModel.RequestClose += () => Close();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
