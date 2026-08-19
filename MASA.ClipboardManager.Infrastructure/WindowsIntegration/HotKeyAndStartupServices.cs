using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Interop;
using Microsoft.Win32;
using MASA.ClipboardManager.Core.Interfaces;

namespace MASA.ClipboardManager.Infrastructure.WindowsIntegration;

public class HotKeyManager : IHotKeyService, IDisposable
{
    private HwndSource? _hwndSource;
    private int _hotkeyId = 9001;
    private bool _isRegistered = false;

    public event EventHandler? HotKeyPressed;

    public bool Register(string hotkeyString)
    {
        Unregister();

        if (string.IsNullOrWhiteSpace(hotkeyString)) return false;

        uint modifiers = 0;
        uint vk = 0;

        var parts = hotkeyString.Split(new[] { '+', ' ' }, StringSplitOptions.RemoveEmptyEntries);
        foreach (var part in parts)
        {
            var p = part.Trim().ToUpperInvariant();
            if (p == "CTRL" || p == "CONTROL") modifiers |= Win32Native.MOD_CONTROL;
            else if (p == "ALT") modifiers |= Win32Native.MOD_ALT;
            else if (p == "SHIFT") modifiers |= Win32Native.MOD_SHIFT;
            else if (p == "WIN" || p == "WINDOWS") modifiers |= Win32Native.MOD_WIN;
            else if (Enum.TryParse<VirtualKey>(p, true, out var key))
            {
                vk = (uint)key;
            }
        }

        if (vk == 0)
        {
            // Default fallback to 'V' key code (0x56)
            vk = 0x56;
        }

        var parameters = new HwndSourceParameters("MASA_Hotkey_Listener")
        {
            HwndSourceHook = WndProc,
            ParentWindow = new IntPtr(-3) // HWND_MESSAGE
        };

        _hwndSource = new HwndSource(parameters);
        _isRegistered = Win32Native.RegisterHotKey(_hwndSource.Handle, _hotkeyId, modifiers | Win32Native.MOD_NOREPEAT, vk);
        return _isRegistered;
    }

    public void Unregister()
    {
        if (_hwndSource != null && _isRegistered)
        {
            Win32Native.UnregisterHotKey(_hwndSource.Handle, _hotkeyId);
            _hwndSource.Dispose();
            _hwndSource = null;
            _isRegistered = false;
        }
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == Win32Native.WM_HOTKEY && wParam.ToInt32() == _hotkeyId)
        {
            HotKeyPressed?.Invoke(this, EventArgs.Empty);
            handled = true;
        }
        return IntPtr.Zero;
    }

    public void Dispose()
    {
        Unregister();
    }
}

public class StartupManager : IStartupService
{
    private const string AppName = "MASA Clipboard Manager";
    private const string RegistryKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";

    public bool IsAutoStartEnabled()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RegistryKeyPath, false);
            return key?.GetValue(AppName) != null;
        }
        catch
        {
            return false;
        }
    }

    public void SetAutoStart(bool enable)
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RegistryKeyPath, true);
            if (key == null) return;

            if (enable)
            {
                var exePath = Environment.ProcessPath;
                if (!string.IsNullOrEmpty(exePath))
                {
                    key.SetValue(AppName, $"\"{exePath}\" --minimized");
                }
            }
            else
            {
                key.DeleteValue(AppName, false);
            }
        }
        catch
        {
            // Suppress registry write permission exceptions
        }
    }
}

public class PasteSimulator : IPasteSimulator
{
    public void SimulatePaste()
    {
        // Small delay to ensure focus returns to foreground application
        Thread.Sleep(80);

        // Press Ctrl
        Win32Native.keybd_event(Win32Native.VK_CONTROL, 0, 0, UIntPtr.Zero);
        // Press V
        Win32Native.keybd_event(Win32Native.VK_V, 0, 0, UIntPtr.Zero);
        // Release V
        Win32Native.keybd_event(Win32Native.VK_V, 0, Win32Native.KEYEVENTF_KEYUP, UIntPtr.Zero);
        // Release Ctrl
        Win32Native.keybd_event(Win32Native.VK_CONTROL, 0, Win32Native.KEYEVENTF_KEYUP, UIntPtr.Zero);
    }
}
