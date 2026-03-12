using System.Runtime.InteropServices;
using System.Security;
using System.Text;

using GnomeStack.Secrets;
using GnomeStack.Secrets.Darwin;
using GnomeStack.Secrets.Linux;
using GnomeStack.Secrets.Win32;

namespace GnomeStack.Standard;

public static class OsSecretVault
{
    private static readonly Lazy<IOsSecretVault> s_vault = new(() =>
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return new WinOsSecretVault();

        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            return new DarwinOsSecretVault();

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) || RuntimeInformation.IsOSPlatform(OSPlatform.FreeBSD))
            return new LinuxOsSecretVault();

        throw new PlatformNotSupportedException("Only Linux, macOS, Windows, and FreeBSD are supported.");
    });

    public static string? GetSecret(string service, string account)
        => s_vault.Value.GetSecret(service, account);

    public static byte[] GetSecretAsBytes(string service, string account)
        => s_vault.Value.GetSecretAsBytes(service, account);

    public static char[] GetSecretAsChars(string service, string account)
    {
        var bytes = s_vault.Value.GetSecretAsBytes(service, account);
        var chars = Encoding.UTF8.GetChars(bytes);
        Array.Clear(bytes, 0, bytes.Length);
        return chars;
    }

    public static ReadOnlySpan<char> GetSecretAsCharSpan(string service, string account)
        => GetSecretAsChars(service, account);

    public static ReadOnlySpan<byte> GetSecretAsByteSpan(string service, string account)
        => GetSecretAsBytes(service, account);

    public static unsafe SecureString GetSecretAsSecureString(string service, string account)
    {
        var bytes = s_vault.Value.GetSecretAsBytes(service, account);
        var utf8Chars = Encoding.UTF8.GetChars(bytes);
        try
        {
            fixed (char* chars = utf8Chars)
            {
                var ss = new SecureString(chars, utf8Chars.Length);
                return ss;
            }
        }
        finally
        {
            Array.Clear(utf8Chars, 0, utf8Chars.Length);
        }
    }

    public static void SetSecret(string service, string account, string secret)
        => s_vault.Value.SetSecret(service, account, secret);

    public static void SetSecret(string service, string account, byte[] secret)
        => s_vault.Value.SetSecret(service, account, secret);

    public static void DeleteSecret(string service, string account)
        => s_vault.Value.DeleteSecret(service, account);
}
