using System.Security.Cryptography;
using GnomeStack.Standard;
using SimpleBase;

namespace SnapX.Core;

public static class MasterKeyManager
{
    private const string ServiceName = SnapXL.AppName;
    private const string AccountName = "MasterEncryptionKey";
    private const int KeySize = 32; // 256-bit

    public static byte[] GetOrGenerateKey()
    {
        var z85Key = OsSecretVault.GetSecret(ServiceName, AccountName);

        if (string.IsNullOrEmpty(z85Key)) return GenerateAndStoreKey();
        try
        {
            var key = Base85.Z85.Decode(z85Key);
            if (key.Length == KeySize)
            {
                // DebugHelper.WriteLine($"[Vault] Key successfully recovered via Z85. Hash: {BitConverter.ToString(key[..4])}...");
                return key;
            }
        }
        catch (Exception ex)
        {
            DebugHelper.WriteLine($"[Vault] Secret was not valid Z85: {ex.Message}");
        }

        return GenerateAndStoreKey();
    }

    private static byte[] GenerateAndStoreKey()
    {
        var newKey = new byte[KeySize];
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(newKey);
        }
        var z85Key = Base85.Z85.Encode(newKey);

        OsSecretVault.SetSecret(ServiceName, AccountName, z85Key);

        return newKey;
    }

    public static void ResetKey() => OsSecretVault.DeleteSecret(ServiceName, AccountName);
}
