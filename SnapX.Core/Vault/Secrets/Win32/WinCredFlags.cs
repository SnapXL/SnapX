using System.Diagnostics.CodeAnalysis;

namespace GnomeStack.Secrets.Win32;

[SuppressMessage("Minor Code Smell", "S2344:Enumeration type names should not have \"Flags\" or \"Enum\" suffixes")]
public enum WinCredFlags
{
    None = 0x0,
    PromptNow = 0x2,
    UsernameTarget = 0x4,
}
