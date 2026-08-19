using System.Security.Cryptography;
using System.Text;
using MASA.ClipboardManager.Core.Interfaces;

namespace MASA.ClipboardManager.Infrastructure.Encryption;

public class DPAPIEncryptionService : IEncryptionService
{
    private static readonly byte[] Entropy = Encoding.UTF8.GetBytes("MASA-Clipboard-2026-Secure-Vault");

    public string Encrypt(string plainText)
    {
        if (string.IsNullOrEmpty(plainText)) return string.Empty;
        try
        {
            byte[] plainBytes = Encoding.UTF8.GetBytes(plainText);
            byte[] cipherBytes = ProtectedData.Protect(plainBytes, Entropy, DataProtectionScope.CurrentUser);
            return Convert.ToBase64String(cipherBytes);
        }
        catch
        {
            return plainText; // Fallback gracefully if DPAPI unavailable
        }
    }

    public string Decrypt(string cipherText)
    {
        if (string.IsNullOrEmpty(cipherText)) return string.Empty;
        try
        {
            byte[] cipherBytes = Convert.FromBase64String(cipherText);
            byte[] plainBytes = ProtectedData.Unprotect(cipherBytes, Entropy, DataProtectionScope.CurrentUser);
            return Encoding.UTF8.GetString(plainBytes);
        }
        catch
        {
            return cipherText; // Fallback gracefully
        }
    }

    public byte[] EncryptBytes(byte[] plainBytes)
    {
        if (plainBytes == null || plainBytes.Length == 0) return Array.Empty<byte>();
        return ProtectedData.Protect(plainBytes, Entropy, DataProtectionScope.CurrentUser);
    }

    public byte[] DecryptBytes(byte[] cipherBytes)
    {
        if (cipherBytes == null || cipherBytes.Length == 0) return Array.Empty<byte>();
        return ProtectedData.Unprotect(cipherBytes, Entropy, DataProtectionScope.CurrentUser);
    }
}
