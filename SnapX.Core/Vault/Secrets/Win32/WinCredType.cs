namespace GnomeStack.Secrets.Win32;

public enum WinCredType
{
    Generic = 1,
    DomainPassword = 2,
    DomainCertificate = 3,
    DomainVisiblePassword = 4,
    GenericCertificate = 5,
    DomainExtended = 6,
    Maximum = 7,
    MaximumEx = Maximum + 1000,
}
