namespace GnomeStack.Secrets.Win32;

public struct WinCredSecret
{
    public WinCredSecret(WinCredType type, string service, string? account, string? password, string? comment)
    {
        this.Type = type;
        this.Service = service;
        this.Account = account;
        this.Password = password;
        this.Comment = comment;
    }

    public WinCredType Type { get; }

    public string Service { get; }

    public string? Account { get; }

    public string? Password { get; }

    public string? Comment { get; }
}
