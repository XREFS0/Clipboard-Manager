using System.ComponentModel;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using MASA.ClipboardManager.Core.Interfaces;
using MASA.ClipboardManager.UI.ViewModels;

namespace MASA.ClipboardManager.UI.Views;

public partial class MainWindow : Window
{
    private readonly MainViewModel _viewModel;
    private readonly IServiceProvider _serviceProvider;
    private readonly ISettingsRepository _settingsRepo;

    public MainWindow(
        MainViewModel viewModel,
        IServiceProvider serviceProvider,
        ISettingsRepository settingsRepo)
    {
        InitializeComponent();
        _viewModel = viewModel;
        _serviceProvider = serviceProvider;
        _settingsRepo = settingsRepo;
        DataContext = _viewModel;

        Loaded += async (s, e) => await _viewModel.InitializeAsync();
    }

    private void OpenDevTools_Click(object sender, RoutedEventArgs e)
    {
        var devToolsWindow = _serviceProvider.GetRequiredService<DevToolsWindow>();
        devToolsWindow.Owner = this;
        devToolsWindow.Show();
    }

    private void OpenSettings_Click(object sender, RoutedEventArgs e)
    {
        var settingsWindow = _serviceProvider.GetRequiredService<SettingsWindow>();
        settingsWindow.Owner = this;
        settingsWindow.ShowDialog();
    }

    private void EditText_Click(object sender, RoutedEventArgs e)
    {
        if (_viewModel.SelectedItem != null)
        {
            var editorWindow = _serviceProvider.GetRequiredService<TextEditorWindow>();
            var editorVm = (TextEditorViewModel)editorWindow.DataContext;
            editorVm.LoadItem(_viewModel.SelectedItem);
            editorWindow.Owner = this;
            editorWindow.ShowDialog();
            _ = _viewModel.RefreshItemsAsync();
        }
    }

    protected override async void OnClosing(CancelEventArgs e)
    {
        var settings = await _settingsRepo.LoadSettingsAsync();
        if (settings.CloseToTray)
        {
            e.Cancel = true;
            Hide();
        }
        else
        {
            base.OnClosing(e);
        }
    }
}
