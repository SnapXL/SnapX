namespace GnomeStack.Secrets;

public interface IOsSecretVault
{
    string? GetSecret(string service, string account);

    byte[] GetSecretAsBytes(string service, string account);

    void SetSecret(string service, string account, string secret);

    void SetSecret(string service, string account, byte[] secret);

    void DeleteSecret(string service, string account);
}
