using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Text;

namespace GnomeStack.Secrets.Linux;

[SuppressMessage("StyleCop.CSharp.NamingRules", "SA1300:Element should begin with upper-case letter")]
[SuppressMessage("StyleCop.CSharp.OrderingRules", "SA1201:Elements should appear in the correct order")]
[SuppressMessage("Minor Code Smell", "S2344:Enumeration type names should not have \"Flags\" or \"Enum\" suffixes")]
[SuppressMessage("StyleCop.CSharp.NamingRules", "SA1307:Accessible fields should begin with upper-case letter")]
public static class LibSecret
{
    private const string LibSecretName = "libsecret-1.so.0";

    private const string SecretCollectionDefault = "default";

    private static IntPtr s_schemaPtr = IntPtr.Zero;

    [SupportedOSPlatform("linux")]
    public static bool DeleteSecret(string service, string account)
    {
        var schema = GetSchemaPtr();
        var result = secret_password_clear_sync(
            schema,
            IntPtr.Zero,
            out IntPtr error,
            "service",
            service,
            "account",
            account,
            IntPtr.Zero);

        GException.ThrowIfError(error);

        return result;
    }

    [SupportedOSPlatform("linux")]
    public static bool SetSecret(string service, string account, string secret)
    {
        var schema = GetSchemaPtr();
        var result = secret_password_store_sync(
            schema,
            SecretCollectionDefault,
            $"{service}/{account}",
            secret,
            IntPtr.Zero,
            out IntPtr error,
            "service",
            service,
            "account",
            account,
            IntPtr.Zero);

        GException.ThrowIfError(error);

        return result;
    }

    [SupportedOSPlatform("linux")]
    public static bool SetSecret(string service, string account, byte[] secret)
    {
        var schema = GetSchemaPtr();
        var chars = Encoding.UTF8.GetChars(secret);
        var result = secret_password_store_sync(
            schema,
            SecretCollectionDefault,
            $"{service}/{account}",
            chars,
            IntPtr.Zero,
            out IntPtr error,
            "service",
            service,
            "account",
            account,
            IntPtr.Zero);

        Array.Clear(chars, 0, chars.Length);
        GException.ThrowIfError(error);

        return result;
    }

    [SupportedOSPlatform("linux")]
    public static string? GetSecret(string service, string account)
    {
        var schema = GetSchemaPtr();
        var result = secret_password_lookup_sync(
            schema,
            IntPtr.Zero,
            out IntPtr error,
            "service",
            service,
            "account",
            account,
            IntPtr.Zero);

        GException.ThrowIfError(error);

        var secret = Marshal.PtrToStringAnsi(result);
        return secret;
    }

    [SupportedOSPlatform("linux")]
    public static unsafe byte[] GetSecretAsBytes(string service, string account)
    {
        var schema = GetSchemaPtr();
        var result = secret_password_lookup_sync(
            schema,
            IntPtr.Zero,
            out IntPtr error,
            "service",
            service,
            "account",
            account,
            IntPtr.Zero);

        if (error != IntPtr.Zero)
        {
            var gerror = Marshal.PtrToStructure<GError>(error);
            var message = Marshal.PtrToStringAnsi(gerror.Message);
            if (message is not null && message.Length > 0)
                throw new InvalidOperationException(message);
        }

        if (result == IntPtr.Zero)
            return Array.Empty<byte>();

        var sbytes = (sbyte*)result;
        var length = 0;
        while (sbytes[length] != 0)
            length++;

        var bytes = new byte[length];
        Marshal.Copy(result, bytes, 0, length);
        return bytes;
    }

    [SupportedOSPlatform("linux")]
    public static List<LibSecretRecord> ListSecrets(string service)
    {
        var ht = new GHashtable();
        var svcPtr = Marshal.StringToHGlobalAnsi("service");
        ht.Set(svcPtr, service);

        var schema = GetSchemaPtr();
        var flags = (byte)(SecretSearchFlags.All | SecretSearchFlags.Unlock | SecretSearchFlags.LoadSecrets);
        var listPtr = secret_service_search_sync(
            IntPtr.Zero,
            schema,
            ht.Handle,
            flags,
            IntPtr.Zero,
            out IntPtr error);

        GException.ThrowIfError(error);
        ht.Free();

        var list = new GList(listPtr);
        var records = new List<LibSecretRecord>();
        var attributesNamePtr = Marshal.StringToHGlobalAnsi("account");
        for (var i = 0; i < list.Count; i++)
        {
            var itemPtr = list[i];
            var secretPtr = secret_item_get_secret(itemPtr);
            var secretValuePtr = secret_value_get_text(secretPtr);
            var attributesPtr = secret_item_get_attributes(itemPtr);
            var attributes = new GHashtable(attributesPtr);
            var accountPtr = attributes.Get(attributesNamePtr);
            var account = Marshal.PtrToStringAnsi(accountPtr);
            var secret = Marshal.PtrToStringAnsi(secretValuePtr);
            records.Add(new LibSecretRecord(account, secret));
        }

        return records;
    }

#pragma warning disable CS0649
    [DllImport(LibSecretName, CallingConvention = CallingConvention.Cdecl)]
    internal static extern IntPtr secret_service_search_sync(
        IntPtr service,
        IntPtr schema,
        IntPtr attributes,
        uint flags,
        IntPtr cancellable,
        out IntPtr error);

    [DllImport(LibSecretName, CallingConvention = CallingConvention.Cdecl)]
    internal static extern IntPtr secret_item_get_attributes(IntPtr item);

    [DllImport(LibSecretName, CallingConvention = CallingConvention.Cdecl)]
    internal static extern IntPtr secret_value_get_text(IntPtr value);

    [DllImport(LibSecretName, CallingConvention = CallingConvention.Cdecl)]
    internal static extern IntPtr secret_item_get_secret(IntPtr item);

    [DllImport(LibSecretName, CallingConvention = CallingConvention.Cdecl)]
    internal static extern bool secret_password_clear_sync(
        IntPtr schema,
        IntPtr cancellable,
        out IntPtr error,
        string attribute1,
        string value1,
        string attribute2,
        string value2,
        IntPtr end);

    [DllImport(LibSecretName, CallingConvention = CallingConvention.Cdecl)]
    internal static extern IntPtr secret_password_lookup_sync(
        IntPtr schema,
        IntPtr cancellable,
        out IntPtr error,
        string attribute1,
        string value1,
        string attribute2,
        string value2,
        IntPtr end);

    [DllImport(LibSecretName, CallingConvention = CallingConvention.Cdecl)]
    internal static extern IntPtr secret_schema_new(string name, int flags, string attribute1, int type1, string attribute2, int type2, IntPtr end);

    [DllImport(LibSecretName, CallingConvention = CallingConvention.Cdecl)]
    internal static extern bool secret_password_store_sync(
        IntPtr schema,
        string collection,
        string name,
        char[] secret,
        IntPtr cancellable,
        out IntPtr error,
        string attribute1,
        string value1,
        string attribute2,
        string value2,
        IntPtr end);

    [DllImport(LibSecretName, CallingConvention = CallingConvention.Cdecl)]
    internal static extern bool secret_password_store_sync(
        IntPtr schema,
        string collection,
        string name,
        string secret,
        IntPtr cancellable,
        out IntPtr error,
        string attribute1,
        string value1,
        string attribute2,
        string value2,
        IntPtr end);

    private static IntPtr GetSchemaPtr()
    {
        if (s_schemaPtr != IntPtr.Zero)
            return s_schemaPtr;

        s_schemaPtr = secret_schema_new(
            "org.freedesktop.Secret.Generic",
            0,
            "service",
            0,
            "account",
            0,
            IntPtr.Zero);
        return s_schemaPtr;
    }

    [Flags]
    internal enum SecretSchemaFlags
    {
        None = 0,
        DoNotMatchName = 1 << 0,
    }

    internal enum SecretSchemaAttributeType
    {
        String = 0,
        Integer = 1,
        Boolean = 2,
    }

    [Flags]
    internal enum SecretSearchFlags : byte
    {
        None = 0,
        All = 1 << 1,
        Unlock = 1 << 2,
        LoadSecrets = 1 << 3,
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct SecretSchemaAttribute
    {
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 50)]
        public string name;

        public SecretSchemaAttributeType type;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct SecretSchema
    {
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 50)]
        public string name;

        public SecretSchemaFlags flags;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
        public SecretSchemaAttribute[] attributes;
    }
}
