using System.Runtime.InteropServices;

namespace GnomeStack.Secrets.Linux;

public class GException : Exception
{
    public GException()
    {
    }

    public GException(string message)
        : base(message)
    {
    }

    public GException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    public int Domain { get; set; }

    public int Code { get; set; }

    public static GException Create(IntPtr error)
    {
        try
        {
            var gerror = Marshal.PtrToStructure<GError>(error);
            var message = Marshal.PtrToStringAnsi(gerror.Message);
            return new GException(message ?? string.Empty)
            {
                Domain = (int)gerror.Domain,
                Code = gerror.Code,
            };
        }
        finally
        {
            NativeMethods.g_error_free(error);
        }
    }

    public static void ThrowIfError(IntPtr error)
    {
        if (error == IntPtr.Zero)
            return;

        throw Create(error);
    }

    private static class NativeMethods
    {
        [DllImport(Libraries.Glib, EntryPoint = "g_error_free", CallingConvention = CallingConvention.Cdecl)]
        public static extern void g_error_free(IntPtr error);
    }
}
