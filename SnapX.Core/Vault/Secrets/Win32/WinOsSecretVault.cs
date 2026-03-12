namespace GnomeStack.Secrets.Win32;

public class WinOsSecretVault : IOsSecretVault
{
    public string? GetSecret(string service, string account)
        => WinCredManager.GetSecret(service, account);

    public byte[] GetSecretAsBytes(string service, string account)
        => WinCredManager.GetSecretAsBytes(service, account);

    public void SetSecret(string service, string account, string secret)
        => WinCredManager.SetSecret(service, account, secret);

    public void SetSecret(string service, string account, byte[] secret)
        => WinCredManager.SetSecret(service, account, secret);

    public void DeleteSecret(string service, string account)
        => WinCredManager.DeleteSecret(service, account);
}
