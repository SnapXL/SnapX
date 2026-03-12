using System.Runtime.InteropServices;
using System.Text;

namespace GnomeStack.Secrets.Win32;

public static class WinCredManager
{
    public static void SetSecret(
        string service,
        string account,
        byte[] secret)
    {
        SetSecret(
            service,
            account,
            secret,
            false,
            (string?)null,
            WinCredPersistence.Enterprise);
    }

    [CLSCompliant(false)]
    public static void SetSecret(
        string service,
        string account,
        byte[] secret,
        bool serviceAsKey,
        string? comment,
        WinCredPersistence persistence)
    {
        var targetName = serviceAsKey ? service : $"{service}/{account}";
        IntPtr data = Marshal.AllocCoTaskMem(secret.Length);
        try
        {
            if (secret.Length > 0)
            {
                Marshal.Copy(secret, 0, data, secret.Length);
            }

            var nativeCredential = new NativeCredential
            {
                AttributeCount = 0,
                Type = WinCredType.Generic,
                TargetName = targetName,
                CredentialBlob = data,
                CredentialBlobSize = secret.Length,
                Persist = persistence,
                UserName = account,
            };

            var isSet = NativeMethods.WriteCredential(
                ref nativeCredential,
                0);

            int errorCode = Marshal.GetLastWin32Error();
            if (isSet)
                return;

            throw new InvalidOperationException($"WriteCredential failed with error code {errorCode}");
        }
        finally
        {
            if (data != IntPtr.Zero)
                Marshal.FreeCoTaskMem(data);
        }
    }

    public static void SetSecret(
        string service,
        string account,
        string secret)
    {
        SetSecret(
            service,
            account,
            secret,
            false,
            null,
            WinCredPersistence.Enterprise);
    }

    [CLSCompliant(false)]
    public static void SetSecret(
        string service,
        string account,
        string secret,
        bool serviceAsKey,
        string? comment,
        WinCredPersistence persistence)
    {
        var bytes = Encoding.UTF8.GetBytes(secret);
        SetSecret(service, account, bytes, serviceAsKey, comment, persistence);
    }

    public static byte[] GetSecretAsBytes(string service, string account)
    {
        var targetName = $"{service}/{account}";
        var isRead = NativeMethods.ReadCredential(targetName, WinCredType.Generic, 0, out var credentialPtr);
        int errorCode = Marshal.GetLastWin32Error();
        if (!isRead)
            throw new InvalidOperationException($"ReadCredential failed with error code {errorCode}");

        using var credentialHandle = new CredentialHandle(credentialPtr);
        return credentialHandle.GetSecretAsBytes();
    }

    public static string? GetSecret(string service, string account)
    {
        var targetName = $"{service}/{account}";
        var isRead = NativeMethods.ReadCredential(targetName, WinCredType.Generic, 0, out var credentialPtr);
        if (!isRead)
            return null;

        using var credentialHandle = new CredentialHandle(credentialPtr);
        return credentialHandle.GetSecret();
    }

    public static WinCredSecret GetCredential(string service, string account)
    {
        var targetName = $"{service}/{account}";
        var isRead = NativeMethods.ReadCredential(targetName, WinCredType.Generic, 0, out var credentialPtr);
        int errorCode = Marshal.GetLastWin32Error();
        if (!isRead)
            throw new InvalidOperationException($"ReadCredential failed with error code {errorCode}");

        using var credentialHandle = new CredentialHandle(credentialPtr);
        return credentialHandle.AllocateCredential();
    }

    public static void DeleteSecret(string service, string account, bool serviceAsKey = false)
    {
        var targetName = serviceAsKey ? service : $"{service}/{account}";
        var isDeleted = NativeMethods.DeleteCredential(targetName, WinCredType.Generic, 0);
        int errorCode = Marshal.GetLastWin32Error();
        if (isDeleted)
            return;

        throw new InvalidOperationException($"DeleteCredential failed with error code {errorCode}");
    }

    public static WinCredSecret[] EnumerateCredentials()
    {
        if (Environment.OSVersion.Version.Major <= 6)
        {
            string message = "Retrieving all credentials is only possible on Windows version Vista or later.";
            throw new NotSupportedException(message);
        }

        var isEnumerated = NativeMethods.EnumerateCredentials(null, 0, out var count, out var credentialPtrs);
        int errorCode = Marshal.GetLastWin32Error();
        if (!isEnumerated)
            throw new InvalidOperationException($"EnumerateCredentials failed with error code {errorCode}");

        using var credentialHandle = new CredentialHandle(credentialPtrs);
        return credentialHandle.AllocateCredentials(count);
    }
}
