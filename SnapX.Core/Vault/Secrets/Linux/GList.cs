using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace GnomeStack.Secrets.Linux;

[SuppressMessage("StyleCop.CSharp.NamingRules", "SA1300:Element should begin with upper-case letter")]
public sealed class GList
{
    private readonly IntPtr list;

    private bool disposed;

    internal GList(IntPtr list)
    {
        this.list = list;
    }

    public int Count
    {
        get
        {
            if (this.list == IntPtr.Zero)
                return 0;

            return g_list_length(this.list);
        }
    }

    public IntPtr this[int index]
    {
        get
        {
            if (index < 0 || index >= this.Count)
                throw new ArgumentOutOfRangeException(nameof(index));

            return g_list_nth_data(this.list, index);
        }
    }

    public static implicit operator GList(IntPtr list)
    {
        return new GList(list);
    }

    public void Free()
    {
        if (this.disposed)
            return;

        if (this.list != IntPtr.Zero)
            g_list_free(this.list);

        this.disposed = true;
    }

    [DllImport(Libraries.Glib, CallingConvention = CallingConvention.Cdecl)]
    private static extern IntPtr g_list_nth_data(IntPtr list, int n);

    [DllImport(Libraries.Glib, CallingConvention = CallingConvention.Cdecl)]
    private static extern void g_list_free(IntPtr list);

    [DllImport(Libraries.Glib, CallingConvention = CallingConvention.Cdecl)]
    private static extern int g_list_length(IntPtr list);
}
