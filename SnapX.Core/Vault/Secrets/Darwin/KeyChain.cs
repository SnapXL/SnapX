using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

using static GnomeStack.Secrets.Darwin.NativeMethods;

namespace GnomeStack.Secrets.Darwin;

// most of the source is based upon: https://github.com/mjcheetham/securestorage-dotnet/tree/master/src/Mjcheetham.SecureStorage
// which is under the MIT license.
public static class KeyChain
{
    public static bool DeleteSecret(string service, string account)
    {
        IntPtr passwordData = IntPtr.Zero;
        IntPtr itemRef = IntPtr.Zero;

        try
        {
            SecKeychainFindGenericPassword(
                IntPtr.Zero,
                (uint)service.Length,
                service,
                (uint)account.Length,
                account,
                out _,
                out passwordData,
                out itemRef);

            if (itemRef != IntPtr.Zero)
            {
                ThrowOnError(
                    SecKeychainItemDelete(itemRef));

                return true;
            }

            return false;
        }
        catch (KeyNotFoundException)
        {
            return false;
        }
        finally
        {
            if (passwordData != IntPtr.Zero)
            {
                SecKeychainItemFreeContent(IntPtr.Zero, passwordData);
            }

            if (itemRef != IntPtr.Zero)
            {
                CFRelease(itemRef);
            }
        }
    }

    public static bool DeleteSecret(string service)
    {
        IntPtr passwordData = IntPtr.Zero;
        IntPtr itemRef = IntPtr.Zero;

        try
        {
            SecKeychainFindGenericPassword(
                IntPtr.Zero,
                (uint)service.Length,
                service,
                0,
                null,
                out _,
                out passwordData,
                out itemRef);

            if (itemRef != IntPtr.Zero)
            {
                ThrowOnError(
                    SecKeychainItemDelete(itemRef));

                return true;
            }

            return false;
        }
        catch (KeyNotFoundException)
        {
            return false;
        }
        finally
        {
            if (passwordData != IntPtr.Zero)
            {
                SecKeychainItemFreeContent(IntPtr.Zero, passwordData);
            }

            if (itemRef != IntPtr.Zero)
            {
                CFRelease(itemRef);
            }
        }
    }

    public static void SetSecret(string service, string account, byte[] password)
    {
        IntPtr passwordData = IntPtr.Zero;
        IntPtr itemRef = IntPtr.Zero;

        try
        {
            // Check if an entry already exists in the keychain
            SecKeychainFindGenericPassword(
                IntPtr.Zero,
                (uint)service.Length,
                service,
                (uint)account.Length,
                account,
                out uint _,
                out passwordData,
                out itemRef);

            if (itemRef != IntPtr.Zero)
            {
                ThrowOnError(
                    SecKeychainItemModifyAttributesAndData(
                        itemRef,
                        IntPtr.Zero,
                        (uint)password.Length,
                        password),
                    $"Could not update password for {service}/{account}");
            }
            else
            {
                ThrowOnError(
                    SecKeychainAddGenericPassword(
                        IntPtr.Zero,
                        (uint)service.Length,
                        service,
                        (uint)account.Length,
                        account,
                        (uint)password.Length,
                        password,
                        out itemRef),
                    $"Could not create key chain credential for {service}/{account}");
            }
        }
        finally
        {
            if (passwordData != IntPtr.Zero)
            {
                SecKeychainItemFreeContent(IntPtr.Zero, passwordData);
            }

            if (itemRef != IntPtr.Zero)
            {
                CFRelease(itemRef);
            }
        }
    }

    public static void SetSecret(string service, string account, string password)
        => SetSecret(service, account, Encoding.UTF8.GetBytes(password));

    public static string? GetSecret(string service, string account)
    {
        var data = GetSecretAsBytes(service, account);
        if (data.Length == 0)
            return string.Empty;

        return Encoding.UTF8.GetString(data);
    }

    public static byte[] GetSecretAsBytes(string service, string account)
    {
        IntPtr passwordData = IntPtr.Zero;
        IntPtr itemRef = IntPtr.Zero;

        try
        {
            // Find the item (itemRef) and password (passwordData) in the keychain
            var error = SecKeychainFindGenericPassword(
                IntPtr.Zero,
                (uint)service.Length,
                service,
                (uint)account.Length,
                account,
                out uint passwordLength,
                out passwordData,
                out itemRef);

            // throwing exceptions is expensive
            if (error == NativeMethods.ErrorSecItemNotFound)
                return Array.Empty<byte>();

            ThrowOnError(error);

            // Decode the password from the raw data
            return NativeMethods.ToByteArray(passwordData, passwordLength);
        }
        finally
        {
            if (passwordData != IntPtr.Zero)
            {
                SecKeychainItemFreeContent(IntPtr.Zero, passwordData);
            }

            if (itemRef != IntPtr.Zero)
            {
                CFRelease(itemRef);
            }
        }
    }

    private static byte[] GetAccountNameAttributeData(IntPtr itemRef)
    {
        IntPtr tagArrayPtr = IntPtr.Zero;
        IntPtr formatArrayPtr = IntPtr.Zero;
        IntPtr attrListPtr = IntPtr.Zero; // SecKeychainAttributeList

        try
        {
            // Extract the user name by querying for the item's 'account' attribute
            tagArrayPtr = Marshal.AllocCoTaskMem(sizeof(SecKeychainAttrType));
            Marshal.Copy(new[] { (int)SecKeychainAttrType.AccountItem }, 0, tagArrayPtr, 1);

            formatArrayPtr = Marshal.AllocCoTaskMem(sizeof(CssmDbAttributeFormat));
            Marshal.Copy(new[] { (int)CssmDbAttributeFormat.String }, 0, formatArrayPtr, 1);

            var attributeInfo = new SecKeychainAttributeInfo
            {
                Count = 1,
                Tag = tagArrayPtr,
                Format = formatArrayPtr,
            };

            ThrowOnError(
                SecKeychainItemCopyAttributesAndData(
                    itemRef,
                    ref attributeInfo,
                    IntPtr.Zero,
                    out attrListPtr,
                    out var _,
                    IntPtr.Zero));

            SecKeychainAttributeList attrList = Marshal.PtrToStructure<SecKeychainAttributeList>(attrListPtr);
            Debug.Assert(attrList.Count == 1, "should only be one attribute");

            byte[] attrListArrayBytes = NativeMethods.ToByteArray(
                attrList.Attributes, Marshal.SizeOf<SecKeychainAttribute>() * attrList.Count);

            SecKeychainAttribute[] attributes = NativeMethods.ToStructArray<SecKeychainAttribute>(attrListArrayBytes);
            Debug.Assert(attributes.Length == 1, "should only be one attribute");

            return NativeMethods.ToByteArray(attributes[0].Data, attributes[0].Length);
        }
        finally
        {
            if (tagArrayPtr != IntPtr.Zero)
            {
                Marshal.FreeCoTaskMem(tagArrayPtr);
            }

            if (formatArrayPtr != IntPtr.Zero)
            {
                Marshal.FreeCoTaskMem(formatArrayPtr);
            }

            if (attrListPtr != IntPtr.Zero)
            {
                SecKeychainItemFreeAttributesAndData(attrListPtr, IntPtr.Zero);
            }
        }
    }
}
