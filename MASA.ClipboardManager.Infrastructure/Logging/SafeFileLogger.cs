using System.IO;
using MASA.ClipboardManager.Core.Interfaces;

namespace MASA.ClipboardManager.Infrastructure.Logging;

public class SafeFileLogger : IAppLogger
{
    private static readonly object LockObj = new();
    private static string? _logFilePath;

    public static void Initialize()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var masaFolder = Path.Combine(appData, "MASA.ClipboardManager", "Logs");
        if (!Directory.Exists(masaFolder))
        {
            Directory.CreateDirectory(masaFolder);
        }
        _logFilePath = Path.Combine(masaFolder, $"log_{DateTime.UtcNow:yyyyMMdd}.txt");
    }

    public void LogInfo(string message)
    {
        WriteLog("INFO", message);
    }

    public void LogWarning(string message)
    {
        WriteLog("WARN", message);
    }

    public void LogError(string message, Exception? ex = null)
    {
        var formatted = ex != null ? $"{message} | Exception: {ex.GetType().Name}: {ex.Message}" : message;
        WriteLog("ERROR", formatted);
    }

    private static void WriteLog(string level, string message)
    {
        try
        {
            if (_logFilePath == null) Initialize();

            var entry = $"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss.fff}] [{level}] {message}{Environment.NewLine}";

            lock (LockObj)
            {
                File.AppendAllText(_logFilePath!, entry);
            }
        }
        catch
        {
            // Suppress logging IO errors
        }
    }
}
