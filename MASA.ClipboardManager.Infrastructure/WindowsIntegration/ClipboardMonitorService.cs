using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using MASA.ClipboardManager.Core.Enums;
using MASA.ClipboardManager.Core.Interfaces;
using MASA.ClipboardManager.Core.Models;

namespace MASA.ClipboardManager.Infrastructure.WindowsIntegration;

public class ClipboardMonitorService : IClipboardMonitor
{
    private readonly ISensitiveDataDetector _sensitiveDetector;
    private readonly IContentTypeDetector _typeDetector;
    private readonly IEncryptionService _encryptionService;
    private readonly ISettingsRepository _settingsRepo;
    private HwndSource? _hwndSource;
    private IntPtr _hwnd = IntPtr.Zero;
    private bool _isPaused = false;
    private bool _isDisposed = false;
    private string? _lastHash = null;

    public event EventHandler<ClipboardItem>? ClipboardChanged;
    public bool IsPaused => _isPaused;

    public ClipboardMonitorService(
        ISensitiveDataDetector sensitiveDetector,
        IContentTypeDetector typeDetector,
        IEncryptionService encryptionService,
        ISettingsRepository settingsRepo)
    {
        _sensitiveDetector = sensitiveDetector;
        _typeDetector = typeDetector;
        _encryptionService = encryptionService;
        _settingsRepo = settingsRepo;
    }

    public void Start()
    {
        if (_hwnd != IntPtr.Zero) return;

        // Create a message-only HwndSource for clipboard updates
        var parameters = new HwndSourceParameters("MASA_Clipboard_Listener")
        {
            HwndSourceHook = WndProc,
            ParentWindow = new IntPtr(-3) // HWND_MESSAGE
        };

        _hwndSource = new HwndSource(parameters);
        _hwnd = _hwndSource.Handle;
        Win32Native.AddClipboardFormatListener(_hwnd);
    }

    public void Stop()
    {
        if (_hwnd != IntPtr.Zero)
        {
            Win32Native.RemoveClipboardFormatListener(_hwnd);
            _hwndSource?.Dispose();
            _hwndSource = null;
            _hwnd = IntPtr.Zero;
        }
    }

    public void Pause() => _isPaused = true;
    public void Resume() => _isPaused = false;

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == Win32Native.WM_CLIPBOARDUPDATE)
        {
            if (!_isPaused)
            {
                // Process on a worker / dispatcher task to avoid blocking clipboard thread
                Task.Run(async () =>
                {
                    await ProcessClipboardChangeAsync();
                });
            }
            handled = true;
        }
        return IntPtr.Zero;
    }

    private async Task ProcessClipboardChangeAsync()
    {
        try
        {
            var settings = await _settingsRepo.LoadSettingsAsync();
            if (!settings.ClipboardMonitoringEnabled || settings.PrivacyMode)
            {
                return;
            }

            // Check source process exclusion
            string sourceProcess = GetForegroundProcessName();
            if (settings.ExcludedApplications.Any(ex => ex.Equals(sourceProcess, StringComparison.OrdinalIgnoreCase)))
            {
                return; // Excluded application
            }

            // Must read Clipboard from STA thread
            ClipboardItem? newItem = null;

            System.Windows.Application.Current?.Dispatcher?.Invoke(() =>
            {
                newItem = ExtractClipboardData(sourceProcess, settings);
            });

            if (newItem == null) return;

            // Compute hash for deduplication
            if (!string.IsNullOrEmpty(newItem.ContentHash) && newItem.ContentHash == _lastHash && settings.PreventDuplicates)
            {
                return;
            }
            _lastHash = newItem.ContentHash;

            ClipboardChanged?.Invoke(this, newItem);
        }
        catch
        {
            // Suppress clipboard lock exceptions or reading conflicts
        }
    }

    private ClipboardItem? ExtractClipboardData(string sourceProcess, AppSettings settings)
    {
        try
        {
            var dataObject = Clipboard.GetDataObject();
            if (dataObject == null) return null;

            // 1. Files / Folders
            if (dataObject.GetDataPresent(DataFormats.FileDrop))
            {
                if (dataObject.GetData(DataFormats.FileDrop) is string[] files && files.Length > 0)
                {
                    var detection = _typeDetector.Detect(null, "FileDrop", files);
                    var joinedFiles = string.Join(Environment.NewLine, files);
                    var hash = ComputeSha256(joinedFiles);

                    var item = new ClipboardItem
                    {
                        ContentType = detection.Type,
                        ContentFormat = "FileDrop",
                        PlainTextContent = joinedFiles,
                        Preview = detection.Preview,
                        SearchableText = joinedFiles,
                        ContentHash = hash,
                        SizeInBytes = joinedFiles.Length * sizeof(char),
                        SourceProcessName = sourceProcess,
                        FileCount = files.Length,
                        FileExtension = files.Length == 1 && File.Exists(files[0]) ? Path.GetExtension(files[0]) : null
                    };

                    if (settings.EnableEncryption)
                    {
                        item.EncryptedPayload = _encryptionService.Encrypt(joinedFiles);
                    }

                    return item;
                }
            }

            // 2. Images
            if (dataObject.GetDataPresent(DataFormats.Bitmap) || Clipboard.ContainsImage())
            {
                var bitmapSource = Clipboard.GetImage();
                if (bitmapSource != null)
                {
                    var (imagePath, sizeBytes) = SaveBitmapToLocalStorage(bitmapSource);
                    var hash = ComputeSha256($"IMAGE_{imagePath}_{sizeBytes}");

                    var item = new ClipboardItem
                    {
                        ContentType = ClipboardContentType.Image,
                        ContentFormat = "Bitmap",
                        Preview = "🖼 Image (" + (sizeBytes / 1024) + " KB)",
                        SearchableText = "Image " + (sizeBytes / 1024) + " KB",
                        ContentHash = hash,
                        SizeInBytes = sizeBytes,
                        ImageStoragePath = imagePath,
                        SourceProcessName = sourceProcess
                    };

                    return item;
                }
            }

            // 3. Text / HTML / RichText
            if (dataObject.GetDataPresent(DataFormats.UnicodeText) || dataObject.GetDataPresent(DataFormats.Text))
            {
                var text = Clipboard.GetText();
                if (string.IsNullOrWhiteSpace(text)) return null;

                // Sensitive Data Check
                var sensitiveCheck = _sensitiveDetector.Detect(text);
                if (sensitiveCheck.IsSensitive)
                {
                    if (settings.DoNotStoreSensitiveData)
                    {
                        return null; // Silently skip storing passwords/API keys
                    }
                }

                string format = "Text";
                if (dataObject.GetDataPresent(DataFormats.Rtf)) format = "RichText";
                else if (dataObject.GetDataPresent(DataFormats.Html)) format = "HTML";

                var (contentType, preview) = _typeDetector.Detect(text, format, null);
                var hash = ComputeSha256(text);

                var item = new ClipboardItem
                {
                    ContentType = contentType,
                    ContentFormat = format,
                    PlainTextContent = text,
                    Preview = preview,
                    SearchableText = text,
                    ContentHash = hash,
                    SizeInBytes = text.Length * sizeof(char),
                    SourceProcessName = sourceProcess,
                    IsSensitive = sensitiveCheck.IsSensitive,
                    SensitiveType = sensitiveCheck.Type
                };

                if (sensitiveCheck.IsSensitive && settings.AutoDeleteSensitiveData)
                {
                    item.AutoDeleteAt = DateTime.UtcNow.AddMinutes(settings.SensitiveAutoDeleteMinutes);
                }

                if (settings.EnableEncryption)
                {
                    item.EncryptedPayload = _encryptionService.Encrypt(text);
                }

                return item;
            }
        }
        catch
        {
            // Clipboard may be temporarily locked by another process
        }

        return null;
    }

    private static (string Path, long Size) SaveBitmapToLocalStorage(BitmapSource bitmap)
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var imagesDir = Path.Combine(appData, "MASA.ClipboardManager", "Images");
        if (!Directory.Exists(imagesDir))
        {
            Directory.CreateDirectory(imagesDir);
        }

        var fileName = $"{Guid.NewGuid():N}.png";
        var filePath = Path.Combine(imagesDir, fileName);

        using (var fileStream = new FileStream(filePath, FileMode.Create))
        {
            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(bitmap));
            encoder.Save(fileStream);
        }

        var fileInfo = new FileInfo(filePath);
        return (filePath, fileInfo.Length);
    }

    private static string GetForegroundProcessName()
    {
        try
        {
            var hwnd = Win32Native.GetForegroundWindow();
            if (hwnd == IntPtr.Zero) return "Unknown";

            Win32Native.GetWindowThreadProcessId(hwnd, out uint processId);
            if (processId == 0) return "Unknown";

            using var process = Process.GetProcessById((int)processId);
            return process.ProcessName + ".exe";
        }
        catch
        {
            return "Unknown";
        }
    }

    private static string ComputeSha256(string rawData)
    {
        using var sha256 = SHA256.Create();
        var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(rawData));
        return Convert.ToHexString(bytes);
    }

    public void Dispose()
    {
        if (_isDisposed) return;
        _isDisposed = true;
        Stop();
    }
}
