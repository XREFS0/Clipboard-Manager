using System.Windows;
using MASA.ClipboardManager.UI.ViewModels;

namespace MASA.ClipboardManager.UI.Views;

public partial class SettingsWindow : Window
{
    private readonly SettingsViewModel _viewModel;

    public SettingsWindow(SettingsViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = _viewModel;
        Loaded += async (s, e) => await _viewModel.InitializeAsync();
    }
}
