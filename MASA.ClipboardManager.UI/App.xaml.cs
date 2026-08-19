using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using MASA.ClipboardManager.Application.Detectors;
using MASA.ClipboardManager.Application.Services;
using MASA.ClipboardManager.Core.Interfaces;
using MASA.ClipboardManager.Infrastructure.Database;
using MASA.ClipboardManager.Infrastructure.Encryption;
using MASA.ClipboardManager.Infrastructure.Logging;
using MASA.ClipboardManager.Infrastructure.Repositories;
using MASA.ClipboardManager.Infrastructure.WindowsIntegration;
using MASA.ClipboardManager.UI.ViewModels;
using MASA.ClipboardManager.UI.Views;

namespace MASA.ClipboardManager.UI;

public partial class App : System.Windows.Application
{
    private static System.Threading.Mutex? _singleInstanceMutex;
    private IServiceProvider? _serviceProvider;
    private System.Windows.Forms.NotifyIcon? _trayIcon;
    private MainWindow? _mainWindow;
    private QuickPasteWindow? _quickPasteWindow;

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        const string appGuid = "Global\\MASA_Clipboard_Manager_App_Instance_Guid";
        _singleInstanceMutex = new System.Threading.Mutex(true, appGuid, out bool createdNew);
        if (!createdNew)
        {
            Shutdown();
            return;
        }

        var services = new ServiceCollection();
        ConfigureServices(services);
        _serviceProvider = services.BuildServiceProvider();

        // Initialize Database & Logger
        var dbFactory = _serviceProvider.GetRequiredService<SqliteConnectionFactory>();
        dbFactory.InitializeDatabase();
        SafeFileLogger.Initialize();

        // Start Clipboard Monitoring
        var clipboardService = _serviceProvider.GetRequiredService<ClipboardService>();
        clipboardService.Start();

        // Register Global Hotkey
        var hotkeyService = _serviceProvider.GetRequiredService<IHotKeyService>();
        var settingsRepo = _serviceProvider.GetRequiredService<ISettingsRepository>();
        var settings = await settingsRepo.LoadSettingsAsync();
        hotkeyService.Register(settings.GlobalHotKey);

        hotkeyService.HotKeyPressed += (s, ev) =>
        {
            Dispatcher.Invoke(() =>
            {
                if (_quickPasteWindow == null)
                {
                    _quickPasteWindow = _serviceProvider.GetRequiredService<QuickPasteWindow>();
                }
                _quickPasteWindow.ShowAtCursor();
            });
        };

        // Initialize System Tray
        InitializeTrayIcon(clipboardService);

        // Show Main Window if not started minimized
        _mainWindow = _serviceProvider.GetRequiredService<MainWindow>();
        if (!settings.StartMinimized)
        {
            _mainWindow.Show();
        }
    }

    private void ConfigureServices(IServiceCollection services)
    {
        // Core & Infrastructure
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

        // Application Services
        services.AddSingleton<ClipboardService>();

        // ViewModels
        services.AddTransient<MainViewModel>();
        services.AddTransient<QuickPasteViewModel>();
        services.AddTransient<DevToolsViewModel>();
        services.AddTransient<SettingsViewModel>();
        services.AddTransient<TextEditorViewModel>();

        // Views
        services.AddTransient<MainWindow>();
        services.AddTransient<QuickPasteWindow>();
        services.AddTransient<DevToolsWindow>();
        services.AddTransient<SettingsWindow>();
        services.AddTransient<TextEditorWindow>();
    }

    private void InitializeTrayIcon(ClipboardService clipboardService)
    {
        _trayIcon = new System.Windows.Forms.NotifyIcon
        {
            Icon = System.Drawing.SystemIcons.Application,
            Visible = true,
            Text = "MASA Clipboard Manager"
        };

        var contextMenu = new System.Windows.Forms.ContextMenuStrip();
        contextMenu.Items.Add("Open Clipboard Manager", null, (s, e) =>
        {
            _mainWindow?.Show();
            _mainWindow?.Activate();
        });

        contextMenu.Items.Add("Quick Paste (Overlay)", null, (s, e) =>
        {
            if (_quickPasteWindow == null)
            {
                _quickPasteWindow = _serviceProvider?.GetRequiredService<QuickPasteWindow>();
            }
            _quickPasteWindow?.ShowAtCursor();
        });

        contextMenu.Items.Add("Toggle Pause Monitoring", null, (s, e) =>
        {
            clipboardService.TogglePause();
        });

        contextMenu.Items.Add(new System.Windows.Forms.ToolStripSeparator());

        contextMenu.Items.Add("Settings", null, (s, e) =>
        {
            var settingsWin = _serviceProvider?.GetRequiredService<SettingsWindow>();
            settingsWin?.Show();
        });

        contextMenu.Items.Add("Exit", null, (s, e) =>
        {
            _trayIcon.Visible = false;
            _trayIcon.Dispose();
            clipboardService.Stop();
            Shutdown();
        });

        _trayIcon.ContextMenuStrip = contextMenu;
        _trayIcon.DoubleClick += (s, e) =>
        {
            _mainWindow?.Show();
            _mainWindow?.Activate();
        };
    }

    protected override void OnExit(ExitEventArgs e)
    {
        if (_trayIcon != null)
        {
            _trayIcon.Visible = false;
            _trayIcon.Dispose();
        }

        if (_singleInstanceMutex != null)
        {
            try
            {
                _singleInstanceMutex.ReleaseMutex();
            }
            catch
            {
                // Ignore if mutex was already abandoned or released
            }
            _singleInstanceMutex.Dispose();
        }

        base.OnExit(e);
    }
}
