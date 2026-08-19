using System.Windows;
using MASA.ClipboardManager.UI.ViewModels;

namespace MASA.ClipboardManager.UI.Views;

public partial class DevToolsWindow : Window
{
    public DevToolsWindow(DevToolsViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
