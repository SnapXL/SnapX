using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace GnomeStack.Secrets.Linux;

[SuppressMessage("StyleCop.CSharp.NamingRules", "SA1300:Element should begin with upper-case letter")]
internal sealed class GHashtable
{
    private readonly IntPtr handle;

    private bool disposed;

    public GHashtable()
    {
        this.handle = g_hash_table_new(IntPtr.Zero, IntPtr.Zero);
    }

    public GHashtable(IntPtr handle)
    {
        this.handle = handle;
    }

    public IntPtr Handle => this.handle;

    public int Count
    {
        get
        {
            if (this.handle == IntPtr.Zero)
                return 0;

            return g_hash_table_size(this.handle);
        }
    }

    public static implicit operator GHashtable(IntPtr handle)
    {
        return new GHashtable(handle);
    }

    public static implicit operator IntPtr(GHashtable hashtable)
    {
        return hashtable.handle;
    }

    public string? GetAsString(IntPtr key)
    {
        if (this.handle == IntPtr.Zero)
            return null;

        IntPtr resultPtr = g_hash_table_lookup(this.handle, key);
        return Marshal.PtrToStringAnsi(resultPtr);
    }

    public IntPtr Get(IntPtr key)
    {
        if (this.handle == IntPtr.Zero)
            return IntPtr.Zero;

        return g_hash_table_lookup(this.handle, key);
    }

    public void Set(IntPtr key, string value)
    {
        if (this.handle == IntPtr.Zero)
            return;

        IntPtr valuePtr = Marshal.StringToHGlobalAnsi(value);

        g_hash_table_replace(this.handle, key, valuePtr);
    }

    public void Free()
    {
        if (this.disposed)
            return;

        if (this.handle != IntPtr.Zero)
            g_hash_table_destroy(this.handle);

        this.disposed = true;
    }

    [DllImport(Libraries.Glib, CallingConvention = CallingConvention.Cdecl)]
    internal static extern int g_hash_table_size(IntPtr hashTable);

    [DllImport(Libraries.Glib, CallingConvention = CallingConvention.Cdecl)]
    internal static extern void g_hash_table_replace(IntPtr hashTable, IntPtr key, IntPtr value);

    [DllImport(Libraries.Glib, CallingConvention = CallingConvention.Cdecl)]
    internal static extern IntPtr g_hash_table_new(IntPtr hashFunc, IntPtr equalFunc);

    [DllImport(Libraries.Glib, CallingConvention = CallingConvention.Cdecl)]
    internal static extern void g_hash_table_destroy(IntPtr hashTable);

    [DllImport(Libraries.Glib, CallingConvention = CallingConvention.Cdecl)]
    internal static extern IntPtr g_hash_table_lookup(IntPtr hashTable, IntPtr key);
}
