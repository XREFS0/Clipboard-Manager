using System.Collections.ObjectModel;
using System.Windows.Input;
using MASA.ClipboardManager.Core.Enums;
using MASA.ClipboardManager.Core.Interfaces;
using MASA.ClipboardManager.Core.Models;

namespace MASA.ClipboardManager.UI.ViewModels;

public class SettingsViewModel : ViewModelBase
{
    private readonly ISettingsRepository _settingsRepo;
    private readonly IStartupService _startupService;
    private readonly IHotKeyService _hotkeyService;

    private AppSettings _settings = new();
    private string _newExcludedApp = string.Empty;
    private string _statusMessage = string.Empty;

    public ObservableCollection<string> ExcludedApplications { get; } = new();

    public int MaxHistoryItems
    {
        get => _settings.MaxHistoryItems;
        set
        {
            _settings.MaxHistoryItems = value;
            OnPropertyChanged();
        }
    }

    public int RetentionDays
    {
        get => _settings.RetentionDays;
        set
        {
            _settings.RetentionDays = value;
            OnPropertyChanged();
        }
    }

    public bool AutoCleanupEnabled
    {
        get => _settings.AutoCleanupEnabled;
        set
        {
            _settings.AutoCleanupEnabled = value;
            OnPropertyChanged();
        }
    }

    public bool PreventDuplicates
    {
        get => _settings.PreventDuplicates;
        set
        {
            _settings.PreventDuplicates = value;
            OnPropertyChanged();
        }
    }

    public bool KeepFavoritesOnCleanup
    {
        get => _settings.KeepFavoritesOnCleanup;
        set
        {
            _settings.KeepFavoritesOnCleanup = value;
            OnPropertyChanged();
        }
    }

    public bool PrivacyMode
    {
        get => _settings.PrivacyMode;
        set
        {
            _settings.PrivacyMode = value;
            OnPropertyChanged();
        }
    }

    public bool DoNotStoreSensitiveData
    {
        get => _settings.DoNotStoreSensitiveData;
        set
        {
            _settings.DoNotStoreSensitiveData = value;
            OnPropertyChanged();
        }
    }

    public bool AutoDeleteSensitiveData
    {
        get => _settings.AutoDeleteSensitiveData;
        set
        {
            _settings.AutoDeleteSensitiveData = value;
            OnPropertyChanged();
        }
    }

    public int SensitiveAutoDeleteMinutes
    {
        get => _settings.SensitiveAutoDeleteMinutes;
        set
        {
            _settings.SensitiveAutoDeleteMinutes = value;
            OnPropertyChanged();
        }
    }

    public bool EnableEncryption
    {
        get => _settings.EnableEncryption;
        set
        {
            _settings.EnableEncryption = value;
            OnPropertyChanged();
        }
    }

    public bool StartWithWindows
    {
        get => _settings.StartWithWindows;
        set
        {
            _settings.StartWithWindows = value;
            OnPropertyChanged();
        }
    }

    public bool StartMinimized
    {
        get => _settings.StartMinimized;
        set
        {
            _settings.StartMinimized = value;
            OnPropertyChanged();
        }
    }

    public bool CloseToTray
    {
        get => _settings.CloseToTray;
        set
        {
            _settings.CloseToTray = value;
            OnPropertyChanged();
        }
    }

    public bool ClipboardMonitoringEnabled
    {
        get => _settings.ClipboardMonitoringEnabled;
        set
        {
            _settings.ClipboardMonitoringEnabled = value;
            OnPropertyChanged();
        }
    }

    public bool PasteImmediatelyOnEnter
    {
        get => _settings.PasteImmediatelyOnEnter;
        set
        {
            _settings.PasteImmediatelyOnEnter = value;
            OnPropertyChanged();
        }
    }

    public ThemeMode Theme
    {
        get => _settings.Theme;
        set
        {
            _settings.Theme = value;
            OnPropertyChanged();
        }
    }

    public string GlobalHotKey
    {
        get => _settings.GlobalHotKey;
        set
        {
            _settings.GlobalHotKey = value;
            OnPropertyChanged();
        }
    }

    public string NewExcludedApp
    {
        get => _newExcludedApp;
        set => SetProperty(ref _newExcludedApp, value);
    }

    public string StatusMessage
    {
        get => _statusMessage;
        set => SetProperty(ref _statusMessage, value);
    }

    public ICommand SaveSettingsCommand { get; }
    public ICommand AddExcludedAppCommand { get; }
    public ICommand RemoveExcludedAppCommand { get; }

    public SettingsViewModel(
        ISettingsRepository settingsRepo,
        IStartupService startupService,
        IHotKeyService hotkeyService)
    {
        _settingsRepo = settingsRepo;
        _startupService = startupService;
        _hotkeyService = hotkeyService;

        SaveSettingsCommand = new AsyncRelayCommand(SaveSettingsAsync);
        AddExcludedAppCommand = new RelayCommand(ExecuteAddExcludedApp);
        RemoveExcludedAppCommand = new RelayCommand(ExecuteRemoveExcludedApp);
    }

    public async Task InitializeAsync()
    {
        _settings = await _settingsRepo.LoadSettingsAsync();
        _settings.StartWithWindows = _startupService.IsAutoStartEnabled();

        ExcludedApplications.Clear();
        foreach (var app in _settings.ExcludedApplications)
        {
            ExcludedApplications.Add(app);
        }

        OnPropertyChanged(string.Empty);
    }

    private void ExecuteAddExcludedApp()
    {
        if (!string.IsNullOrWhiteSpace(NewExcludedApp))
        {
            var app = NewExcludedApp.Trim();
            if (!app.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
            {
                app += ".exe";
            }

            if (!ExcludedApplications.Contains(app, StringComparer.OrdinalIgnoreCase))
            {
                ExcludedApplications.Add(app);
                NewExcludedApp = string.Empty;
            }
        }
    }

    private void ExecuteRemoveExcludedApp(object? param)
    {
        if (param is string app && ExcludedApplications.Contains(app))
        {
            ExcludedApplications.Remove(app);
        }
    }

    private async Task SaveSettingsAsync()
    {
        _settings.ExcludedApplications = ExcludedApplications.ToList();
        await _settingsRepo.SaveSettingsAsync(_settings);

        _startupService.SetAutoStart(_settings.StartWithWindows);
        _hotkeyService.Register(_settings.GlobalHotKey);

        StatusMessage = "Settings saved successfully!";
    }
}
