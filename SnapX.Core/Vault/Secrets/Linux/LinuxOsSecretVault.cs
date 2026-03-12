namespace GnomeStack.Secrets.Linux;

public class LinuxOsSecretVault : IOsSecretVault
{
    public string? GetSecret(string service, string account)
        => LibSecret.GetSecret(service, account);

    public byte[] GetSecretAsBytes(string service, string account)
        => LibSecret.GetSecretAsBytes(service, account);

    public void SetSecret(string service, string account, string secret)
        => LibSecret.SetSecret(service, account, secret);

    public void SetSecret(string service, string account, byte[] secret)
        => LibSecret.SetSecret(service, account, secret);

    public void DeleteSecret(string service, string account)
        => LibSecret.DeleteSecret(service, account);
}
