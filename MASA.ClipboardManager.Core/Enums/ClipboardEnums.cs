namespace MASA.ClipboardManager.Core.Enums;

public enum ClipboardContentType
{
    Text = 0,
    URL = 1,
    Email = 2,
    Code = 3,
    HTML = 4,
    Image = 5,
    File = 6,
    Folder = 7,
    RichText = 8,
    Unknown = 9
}

public enum SensitiveDataType
{
    None = 0,
    Password = 1,
    ApiKey = 2,
    JwtToken = 3,
    CreditCard = 4,
    AuthCode = 5,
    PrivateToken = 6
}

public enum ThemeMode
{
    System = 0,
    Dark = 1,
    Light = 2
}
