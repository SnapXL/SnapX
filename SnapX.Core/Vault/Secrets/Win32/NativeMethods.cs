using System.Runtime.InteropServices;

namespace GnomeStack.Secrets.Win32;

internal static class NativeMethods
{
    [DllImport("Advapi32.dll", EntryPoint = "CredReadW", CharSet = CharSet.Unicode, SetLastError = true)]
    internal static extern bool ReadCredential([In] string target, [In] WinCredType type, [In] int reservedFlag, out IntPtr credentialPtr);

    [DllImport("Advapi32.dll", EntryPoint = "CredWriteW", CharSet = CharSet.Unicode, SetLastError = true)]
    internal static extern bool WriteCredential([In] ref NativeCredential userCredential, [In] uint flags);

    [DllImport("Advapi32.dll", EntryPoint = "CredFree", SetLastError = true)]
    internal static extern bool FreeCredential([In] IntPtr credentialPointer);

    [DllImport("Advapi32.dll", EntryPoint = "CredDeleteW", SetLastError = true, CharSet = CharSet.Unicode)]
    internal static extern bool DeleteCredential([In] string target, [In] WinCredType type, [In] int reservedFlag);

    [DllImport("Advapi32.dll", EntryPoint = "CredEnumerateW", SetLastError = true, CharSet = CharSet.Unicode)]
    internal static extern bool EnumerateCredentials([In] string? filter, [In] int flags, out int count, out IntPtr credentialPtrs);
}
