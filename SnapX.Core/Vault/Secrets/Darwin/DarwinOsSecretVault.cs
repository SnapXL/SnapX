namespace GnomeStack.Secrets.Darwin;

public class DarwinOsSecretVault : IOsSecretVault
{
    public string? GetSecret(string service, string account)
        => KeyChain.GetSecret(service, account);

    public byte[] GetSecretAsBytes(string service, string account)
        => KeyChain.GetSecretAsBytes(service, account);

    public void SetSecret(string service, string account, string secret)
        => KeyChain.SetSecret(service, account, secret);

    public void SetSecret(string service, string account, byte[] secret)
        => KeyChain.SetSecret(service, account, secret);

    public void DeleteSecret(string service, string account)
        => KeyChain.DeleteSecret(service, account);
}
