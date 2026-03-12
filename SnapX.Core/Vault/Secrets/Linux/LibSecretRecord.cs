namespace GnomeStack.Secrets.Linux;

public readonly struct LibSecretRecord
{
    public LibSecretRecord(string? account, string? secret)
    {
        this.Account = account ?? string.Empty;
        this.Secret = secret ?? string.Empty;
    }

    public string Account { get; }

    public string Secret { get; }
}
