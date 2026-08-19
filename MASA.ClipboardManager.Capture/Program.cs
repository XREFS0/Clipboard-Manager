using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Extensions.DependencyInjection;
using MASA.ClipboardManager.Application.Detectors;
using MASA.ClipboardManager.Application.Services;
using MASA.ClipboardManager.Core.Enums;
using MASA.ClipboardManager.Core.Interfaces;
using MASA.ClipboardManager.Core.Models;
using MASA.ClipboardManager.Infrastructure.Database;
using MASA.ClipboardManager.Infrastructure.Encryption;
using MASA.ClipboardManager.Infrastructure.Logging;
using MASA.ClipboardManager.Infrastructure.Repositories;
using MASA.ClipboardManager.Infrastructure.WindowsIntegration;
using MASA.ClipboardManager.UI.ViewModels;
using MASA.ClipboardManager.UI.Views;

namespace MASA.ClipboardManager.Capture;

public class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        var app = new System.Windows.Application();

        // Register application resources (Themes, Converters, Colors)
        var colors = new ResourceDictionary { Source = new Uri("pack://application:,,,/MASA.ClipboardManager.UI;component/Themes/Colors.xaml", UriKind.Absolute) };
        var styles = new ResourceDictionary { Source = new Uri("pack://application:,,,/MASA.ClipboardManager.UI;component/Themes/ModernStyles.xaml", UriKind.Absolute) };
        var converters = new ResourceDictionary { Source = new Uri("pack://application:,,,/MASA.ClipboardManager.UI;component/Themes/Converters.xaml", UriKind.Absolute) };

        app.Resources.MergedDictionaries.Add(colors);
        app.Resources.MergedDictionaries.Add(styles);
        app.Resources.MergedDictionaries.Add(converters);

        // Setup DI
        var services = new ServiceCollection();
        ConfigureServices(services);
        var provider = services.BuildServiceProvider();

        // Initialize Database with rich seed data for screenshots
        var dbFactory = provider.GetRequiredService<SqliteConnectionFactory>();
        dbFactory.InitializeDatabase();
        SeedSampleData(provider);

        var outputDir = @"C:\Users\masa\Desktop\New folder\Screenshots";
        if (!Directory.Exists(outputDir))
        {
            Directory.CreateDirectory(outputDir);
        }

        // 1. Capture MainWindow
        var mainWindow = provider.GetRequiredService<MainWindow>();
        mainWindow.Show();
        mainWindow.UpdateLayout();
        var mainVm = (MainViewModel)mainWindow.DataContext;
        mainVm.RefreshItemsAsync().Wait();
        mainVm.SelectedItem = mainVm.FilteredItems.Count > 0 ? mainVm.FilteredItems[0] : null;
        mainWindow.UpdateLayout();

        DoEvents();
        Thread.Sleep(500);
        DoEvents();

        SaveWindowVisual(mainWindow, Path.Combine(outputDir, "01_Main_Dashboard.png"));
        mainWindow.Hide();

        // 2. Capture QuickPasteWindow
        var quickPasteWin = provider.GetRequiredService<QuickPasteWindow>();
        var quickVm = (QuickPasteViewModel)quickPasteWin.DataContext;
        quickVm.InitializeAsync().Wait();
        quickPasteWin.Show();
        quickPasteWin.UpdateLayout();
        DoEvents();
        Thread.Sleep(300);
        DoEvents();

        SaveWindowVisual(quickPasteWin, Path.Combine(outputDir, "02_Quick_Paste_Overlay.png"));
        quickPasteWin.Hide();

        // 3. Capture DevToolsWindow
        var devToolsWin = provider.GetRequiredService<DevToolsWindow>();
        var devVm = (DevToolsViewModel)devToolsWin.DataContext;
        devVm.InputText = "{\n  \"application\": \"MASA Clipboard Manager\",\n  \"version\": \"1.0.0\",\n  \"features\": [\"Encrypted SQLite\", \"Zero-CPU Listener\", \"Sensitive Detector\", \"DevTools\"]\n}";
        devVm.FormatJsonCommand.Execute(null);
        devToolsWin.Show();
        devToolsWin.UpdateLayout();
        DoEvents();
        Thread.Sleep(300);
        DoEvents();

        SaveWindowVisual(devToolsWin, Path.Combine(outputDir, "03_Developer_Tools.png"));
        devToolsWin.Hide();

        // 4. Capture SettingsWindow
        var settingsWin = provider.GetRequiredService<SettingsWindow>();
        var setVm = (SettingsViewModel)settingsWin.DataContext;
        setVm.InitializeAsync().Wait();
        settingsWin.Show();
        settingsWin.UpdateLayout();
        DoEvents();
        Thread.Sleep(300);
        DoEvents();

        SaveWindowVisual(settingsWin, Path.Combine(outputDir, "04_Settings_and_Security.png"));
        settingsWin.Hide();

        // 5. Capture TextEditorWindow
        var editorWin = provider.GetRequiredService<TextEditorWindow>();
        var editVm = (TextEditorViewModel)editorWin.DataContext;
        editVm.EditorText = "public class MasaClipboardManager\n{\n    public string Name { get; set; } = \"MASA\";\n    public bool IsEncrypted { get; set; } = true;\n    public string StorageEngine { get; set; } = \"SQLite WAL\";\n}";
        editorWin.Show();
        editorWin.UpdateLayout();
        DoEvents();
        Thread.Sleep(300);
        DoEvents();

        SaveWindowVisual(editorWin, Path.Combine(outputDir, "05_Snippet_Editor.png"));
        editorWin.Hide();

        Console.WriteLine("All 5 real UI screenshots captured successfully into Screenshots folder!");
    }

    private static void DoEvents()
    {
        var frame = new System.Windows.Threading.DispatcherFrame();
        System.Windows.Threading.Dispatcher.CurrentDispatcher.BeginInvoke(
            System.Windows.Threading.DispatcherPriority.Background,
            new System.Windows.Threading.DispatcherOperationCallback(f =>
            {
                ((System.Windows.Threading.DispatcherFrame)f).Continue = false;
                return null;
            }), frame);
        System.Windows.Threading.Dispatcher.PushFrame(frame);
    }

    private static void SaveWindowVisual(Window window, string filePath)
    {
        window.Measure(new System.Windows.Size(window.Width, window.Height));
        window.Arrange(new System.Windows.Rect(0, 0, window.Width, window.Height));
        window.UpdateLayout();

        int width = (int)Math.Max(window.ActualWidth, window.Width);
        int height = (int)Math.Max(window.ActualHeight, window.Height);

        var renderBitmap = new RenderTargetBitmap(width, height, 96, 96, PixelFormats.Pbgra32);
        renderBitmap.Render(window);

        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(renderBitmap));

        using var stream = new FileStream(filePath, FileMode.Create);
        encoder.Save(stream);
    }

    private static void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton<SqliteConnectionFactory>();
        services.AddSingleton<IClipboardRepository, ClipboardRepository>();
        services.AddSingleton<ICollectionRepository, CollectionRepository>();
        services.AddSingleton<ISettingsRepository, SettingsRepository>();
        services.AddSingleton<IEncryptionService, DPAPIEncryptionService>();
        services.AddSingleton<ISensitiveDataDetector, SensitiveDataDetector>();
        services.AddSingleton<IContentTypeDetector, ContentTypeDetector>();
        services.AddSingleton<IClipboardMonitor, ClipboardMonitorService>();
        services.AddSingleton<IHotKeyService, HotKeyManager>();
        services.AddSingleton<IStartupService, StartupManager>();
        services.AddSingleton<IPasteSimulator, PasteSimulator>();
        services.AddSingleton<IDeveloperToolsService, DeveloperToolsService>();
        services.AddSingleton<IAppLogger, SafeFileLogger>();

        services.AddSingleton<ClipboardService>();

        services.AddTransient<MainViewModel>();
        services.AddTransient<QuickPasteViewModel>();
        services.AddTransient<DevToolsViewModel>();
        services.AddTransient<SettingsViewModel>();
        services.AddTransient<TextEditorViewModel>();

        services.AddTransient<MainWindow>();
        services.AddTransient<QuickPasteWindow>();
        services.AddTransient<DevToolsWindow>();
        services.AddTransient<SettingsWindow>();
        services.AddTransient<TextEditorWindow>();
    }

    private static void SeedSampleData(IServiceProvider provider)
    {
        var repo = provider.GetRequiredService<IClipboardRepository>();

        var sampleItems = new[]
        {
            new ClipboardItem
            {
                ContentType = ClipboardContentType.Code,
                PlainTextContent = "public static async Task<bool> ProcessClipboardAsync(string payload)\n{\n    // Production grade async processor\n    var result = await ProcessEngine.ExecuteAsync(payload);\n    return result.Success;\n}",
                Preview = "public static async Task<bool> ProcessClipboardAsync(string payload)...",
                SearchableText = "public static async Task<bool> ProcessClipboardAsync",
                SizeInBytes = 210,
                IsPinned = true,
                IsFavorite = true,
                CreatedAt = DateTime.UtcNow.AddMinutes(-5),
                LastUsedAt = DateTime.UtcNow.AddMinutes(-1),
                SourceProcessName = "devenv.exe",
                CollectionId = "col_dev"
            },
            new ClipboardItem
            {
                ContentType = ClipboardContentType.URL,
                PlainTextContent = "https://github.com/MASA/ClipboardManager-Desktop-Enterprise",
                Preview = "https://github.com/MASA/ClipboardManager-Desktop-Enterprise",
                SearchableText = "https://github.com/MASA/ClipboardManager-Desktop-Enterprise",
                SizeInBytes = 56,
                IsPinned = false,
                IsFavorite = true,
                CreatedAt = DateTime.UtcNow.AddMinutes(-15),
                LastUsedAt = DateTime.UtcNow.AddMinutes(-3),
                SourceProcessName = "chrome.exe",
                CollectionId = "col_work"
            },
            new ClipboardItem
            {
                ContentType = ClipboardContentType.Text,
                PlainTextContent = "Best regards,\r\nMASA Engineering Team\r\nSecure & High-Performance Desktop Solutions",
                Preview = "Best regards, MASA Engineering Team...",
                SearchableText = "Best regards, MASA Engineering Team",
                SizeInBytes = 85,
                IsPinned = true,
                IsFavorite = false,
                CreatedAt = DateTime.UtcNow.AddHours(-1),
                LastUsedAt = DateTime.UtcNow.AddMinutes(-10),
                SourceProcessName = "outlook.exe",
                CollectionId = "col_templates"
            },
            new ClipboardItem
            {
                ContentType = ClipboardContentType.File,
                PlainTextContent = "C:\\Projects\\MASA\\MASA.ClipboardManager.sln\r\nC:\\Projects\\MASA\\README.md",
                Preview = "📄 2 files (MASA.ClipboardManager.sln...)",
                SearchableText = "MASA.ClipboardManager.sln README.md",
                SizeInBytes = 82,
                IsPinned = false,
                IsFavorite = false,
                CreatedAt = DateTime.UtcNow.AddHours(-2),
                LastUsedAt = DateTime.UtcNow.AddHours(-1),
                SourceProcessName = "explorer.exe",
                FileCount = 2
            },
            new ClipboardItem
            {
                ContentType = ClipboardContentType.Code,
                PlainTextContent = "SELECT c.Id, c.Name, COUNT(ci.Id) as ItemCount FROM Collections c LEFT JOIN ClipboardItems ci ON ci.CollectionId = c.Id GROUP BY c.Id;",
                Preview = "SELECT c.Id, c.Name, COUNT(ci.Id) as ItemCount FROM Collections...",
                SearchableText = "SELECT c.Id, c.Name, COUNT(ci.Id) as ItemCount",
                SizeInBytes = 135,
                IsPinned = false,
                IsFavorite = true,
                CreatedAt = DateTime.UtcNow.AddHours(-3),
                LastUsedAt = DateTime.UtcNow.AddHours(-2),
                SourceProcessName = "DataGrip.exe",
                CollectionId = "col_dev"
            }
        };

        foreach (var item in sampleItems)
        {
            repo.AddAsync(item).Wait();
        }
    }
}
