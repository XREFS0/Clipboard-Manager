namespace MASA.ClipboardManager.Infrastructure.WindowsIntegration;

[Flags]
public enum KeyModifiers : uint
{
    None = 0,
    Alt = 1,
    Control = 2,
    Shift = 4,
    Windows = 8
}

public enum VirtualKey : uint
{
    None = 0,
    V = 0x56,
    C = 0x43,
    X = 0x58,
    F = 0x46,
    P = 0x50,
    Space = 0x20,
    Insert = 0x2D,
    F1 = 0x70,
    F2 = 0x71,
    F3 = 0x72,
    F4 = 0x73,
    F5 = 0x74,
    F6 = 0x75,
    F7 = 0x76,
    F8 = 0x77,
    F9 = 0x78,
    F10 = 0x79,
    F11 = 0x7A,
    F12 = 0x7B
}
