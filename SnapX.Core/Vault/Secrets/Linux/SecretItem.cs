using System.Runtime.InteropServices;

namespace GnomeStack.Secrets.Linux;

[StructLayout(LayoutKind.Sequential)]
internal struct SecretItem
{
    [MarshalAs(UnmanagedType.LPStr)]
    public string Label;

    public IntPtr Secret; // Assuming Secret is a pointer to some data

    [MarshalAs(UnmanagedType.I1)]
    public bool Locked;

    public long Created; // Assuming Created is a Unix timestamp

    public long Modified; // Assuming Modified is a Unix timestamp

    public IntPtr Attributes; // Assuming Attributes is a pointer to a GHashTable
}
