using System.Diagnostics.CodeAnalysis;
using System.Management.Automation;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Security;
using System.Text;
using Microsoft.Win32;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;
using SnapX.Core.Media;
using SnapX.Core.Utils.Extensions;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Graphics.Gdi;
using Windows.Win32.Storage.FileSystem;
using Windows.Win32.System.Memory;
using Windows.Win32.UI.Controls;
using Windows.Win32.UI.Shell;
using Windows.Win32.UI.WindowsAndMessaging;

namespace SnapX.Core.Utils.Native;


[SecurityCritical]
[SecuritySafeCritical]
[SupportedOSPlatform("windows10.0.18362")]
public sealed class SafeHICON : SafeHandle
{
    internal HICON Hicon;
    internal SafeHICON(HICON Hicon, bool ownsHandle) : base(IntPtr.Zero, ownsHandle)
    {
        this.Hicon = Hicon;
    }

    public override bool IsInvalid => handle == IntPtr.Zero;

    protected override bool ReleaseHandle()
    {
        return PInvoke.DestroyIcon(Hicon);
    }
}

// Windows 10 version 1903
[SupportedOSPlatform("windows10.0.18362")]
public class WindowsAPI : NativeAPI
{
    // Constants for allocating memory and setting data format
    public const uint CF_TEXT = 1;
    private const uint CF_DIB = 8;
    private const uint CF_HDROP = 15;


    internal static HICON GetFileIcon(string filePath, bool isSmallIcon)
    {
        unsafe
        {
            var shfi = new SHFILEINFOW();
            var flags = SHGFI_FLAGS.SHGFI_ICON;

            if (isSmallIcon)
            {
                flags |= SHGFI_FLAGS.SHGFI_SMALLICON;
            }
            else
            {
                flags |= SHGFI_FLAGS.SHGFI_LARGEICON;
            }
            var shfiPtr = Marshal.AllocHGlobal(sizeof(SHFILEINFOW));
            var shfiUnsafe = (SHFILEINFOW*)shfiPtr;
            PInvoke.SHGetFileInfo(filePath, FILE_FLAGS_AND_ATTRIBUTES.SECURITY_ANONYMOUS, shfiUnsafe,
                (uint)Marshal.SizeOf(shfi), flags);

            var icon = shfi.hIcon;
            PInvoke.DestroyIcon(icon);
            return icon;
        }
    }

    public override Image GetJumboFileIcon(string filePath, bool jumboSize = true)
    {
        unsafe
        {
            SHFILEINFOW shfi;
            PInvoke.SHGetFileInfo(filePath, FILE_FLAGS_AND_ATTRIBUTES.SECURITY_ANONYMOUS, &shfi, (uint)Marshal.SizeOf<SHFILEINFOW>(),
                SHGFI_FLAGS.SHGFI_SYSICONINDEX | SHGFI_FLAGS.SHGFI_USEFILEATTRIBUTES);
            var guid = new Guid("46EB5926-582E-4017-9FDF-E8998DAA0950");
            PInvoke.SHGetImageList(jumboSize ? (int)PInvoke.SHIL_JUMBO : (int)PInvoke.SHIL_EXTRALARGE, in guid, out var pImageList);
            HIMAGELIST imagelist = new((nint)pImageList);
            var hIcon = PInvoke.ImageList_GetIcon(imagelist, shfi.iIcon,
                IMAGE_LIST_DRAW_STYLE.ILD_TRANSPARENT | IMAGE_LIST_DRAW_STYLE.ILD_IMAGE);
            var safeHIcon = new SafeHICON(hIcon, true);
            PInvoke.GetIconInfo(safeHIcon, out var iconInfo);
            var bmp = new BITMAP();
            var width = 0;
            var height = 0;
            var bitsPerPixel = 0;
            if (iconInfo.hbmColor != IntPtr.Zero)
            {
                var nWrittenBytes = PInvoke.GetObject(iconInfo.hbmColor, sizeof(BITMAP), &bmp);
                if (nWrittenBytes > 0)
                {
                    width = bmp.bmWidth;
                    height = bmp.bmHeight;
                    bitsPerPixel = bmp.bmBitsPixel;
                }
            }
            else if (iconInfo.hbmMask != IntPtr.Zero)
            {
                var nWrittenBytes = PInvoke.GetObject(iconInfo.hbmMask, sizeof(BITMAP), &bmp);
                if (nWrittenBytes > 0)
                {
                    width = bmp.bmWidth;
                    height = bmp.bmHeight / 2;
                    bitsPerPixel = 1;
                }
            }

            var totalBytes = width * Math.Abs(height) * (bitsPerPixel / 8);

            var managedArray = new byte[totalBytes];
            Marshal.Copy((IntPtr)bmp.bmBits, managedArray, 0, totalBytes);

            Image img = Image.LoadPixelData<Bgra32>(managedArray, width, height);
            if (iconInfo.hbmColor != IntPtr.Zero) PInvoke.DeleteObject(iconInfo.hbmColor);
            if (iconInfo.hbmMask != IntPtr.Zero) PInvoke.DeleteObject(iconInfo.hbmMask);

            PInvoke.DestroyIcon(hIcon);
            return img;
        }
    }

    public override void ShowWindow(WindowInfo Window)
    {
        var handle = Window.Handle;
        if (handle == IntPtr.Zero)
        {
            throw new InvalidOperationException("Invalid window handle.");
        }

        PInvoke.ShowWindow(new HWND(handle), SHOW_WINDOW_CMD.SW_SHOW);
    }

    public override void ShowWindow(IntPtr hwnd)
    {
        if (hwnd == IntPtr.Zero)
        {
            throw new InvalidOperationException("Invalid window handle.");
        }

        PInvoke.ShowWindow(new HWND(hwnd), SHOW_WINDOW_CMD.SW_SHOW);
    }

    // Method to check if a window is minimized
    public static bool IsWindowMinimized(IntPtr hwnd)
    {
        var placement = new WINDOWPLACEMENT();
        if (PInvoke.GetWindowPlacement(new HWND(hwnd), ref placement))
        {
            return placement.showCmd == SHOW_WINDOW_CMD.SW_MINIMIZE;
        }
        return false;
    }

    // Method to check if the window is the active (foreground) window
    public static bool IsWindowActive(IntPtr hwnd)
    {
        var activeWindow = PInvoke.GetForegroundWindow();
        return hwnd == activeWindow;
    }
    // [UnmanagedCallersOnly]
    private static BOOL EnumWindowsCallback(HWND hwnd, LPARAM lParam)
    {
        // We are only interested in top-level windows that are visible
        if (!PInvoke.IsWindowVisible(hwnd))
            return true;

        var windowTitle = new StringBuilder(256);
        var GcHandle = GCHandle.Alloc(windowTitle, GCHandleType.Pinned);
        var ptr = GcHandle.AddrOfPinnedObject();


        PInvoke.GetWindowText(hwnd, new PWSTR(ptr), 256);

        // If the window has a non-empty title, add it to the list
        if (windowTitle.Length <= 0) return true;
        var windowRECT = GetWindowRect(hwnd);
        var windowInfo = new WindowInfo
        {
            Handle = hwnd,
            Title = windowTitle.ToString(),
            Rectangle = windowRECT,
            IsVisible = PInvoke.IsWindowVisible(hwnd),
            IsMinimized = IsWindowMinimized(hwnd),
            IsActive = IsWindowActive(hwnd)
        };

        // Add the window to the global list
        windowList.Add(windowInfo);

        return true; // Continue enumeration
    }

    private static List<WindowInfo> windowList = [];

    public override List<WindowInfo> GetWindowList()
    {
        windowList.Clear();
        unsafe
        {
            var callback = EnumWindowsCallback;
            // Convert delegate to a function pointer
            var functionPointer =
                (delegate* unmanaged[Stdcall]<HWND, LPARAM, BOOL>)Marshal.GetFunctionPointerForDelegate(callback);

            PInvoke.EnumWindows(functionPointer, IntPtr.Zero);
        }
        return windowList;
    }

    public override void CopyText(string text)
    {
        if (!PInvoke.OpenClipboard(new HWND()))
        {
            throw new AccessViolationException("Failed to open clipboard.");
        }

        PInvoke.EmptyClipboard();

        // Allocate global memory for the text
        var hGlobal = PInvoke.GlobalAlloc(GLOBAL_ALLOC_FLAGS.GMEM_ZEROINIT, (uint)(text.Length + 1) * sizeof(char));

        // Lock the memory so that we can copy the text into it
        unsafe
        {
            var lpGlobal = (IntPtr)PInvoke.GlobalLock(hGlobal);

            // Copy the text into the allocated memory
            Marshal.Copy(text.ToCharArray(), 0, lpGlobal, text.Length);

            PInvoke.GlobalUnlock(hGlobal);

            PInvoke.SetClipboardData(CF_TEXT, new HANDLE((IntPtr)hGlobal));

            PInvoke.CloseClipboard();
        }
    }

    public override Point GetCursorPosition()
    {
        PInvoke.GetCursorPos(out var LpPoint);
        return new Point(LpPoint.X, LpPoint.Y);
    }
    public override void CopyImage(Image image, string? filename = null)
    {
        PInvoke.OpenClipboard(new HWND());
        PInvoke.EmptyClipboard();

        using var ms = new MemoryStream();
        var format = image.Metadata.DecodedImageFormat ?? PngFormat.Instance;
        image.Save(ms, format);
        var imageBytes = ms.ToArray();

        var dataSize = (uint)imageBytes.Length;
        var dataPtr = PInvoke.GlobalAlloc(GLOBAL_ALLOC_FLAGS.GHND, dataSize);
        if (dataPtr == IntPtr.Zero) return;

        unsafe
        {
            var lockedData = (IntPtr)PInvoke.GlobalLock(dataPtr);
            if (lockedData == IntPtr.Zero)
            {
                PInvoke.GlobalFree(dataPtr);
                return;
            }
            Marshal.Copy(imageBytes, 0, lockedData, imageBytes.Length);
            PInvoke.GlobalUnlock(dataPtr);

            PInvoke.SetClipboardData(CF_DIB, new HANDLE((IntPtr)dataPtr));

            if (!string.IsNullOrEmpty(filename))
            {
                int size = Marshal.SystemDefaultCharSize * (filename.Length + 1);
                IntPtr filePathPtr = PInvoke.GlobalAlloc(GLOBAL_ALLOC_FLAGS.GHND, (uint)(20 + size));

                if (filePathPtr != IntPtr.Zero)
                {
                    var hMem = new HGLOBAL(filePathPtr);
                    var ptr = (byte*)PInvoke.GlobalLock(hMem);
                    if (ptr != null)
                    {
                        *(int*)ptr = 20; // DROPFILES.pFiles offset
                        *(int*)(ptr + 4) = 1; // fWide = true (Unicode)

                        var filenamePtr = (IntPtr)(ptr + 20);
                        Marshal.Copy(filename.ToCharArray(), 0, filenamePtr, filename.Length);
                        *(short*)(filenamePtr + (filename.Length * sizeof(char))) = 0; // Null terminator

                        PInvoke.GlobalUnlock(hMem);
                        PInvoke.SetClipboardData(CF_HDROP, new HANDLE(filePathPtr));
                    }
                }
            }

            PInvoke.CloseClipboard();
        }
    }

    public override Rectangle GetWindowRectangle(WindowInfo Window)
    {
        var handle = Window.Handle;
        if (handle == IntPtr.Zero)
        {
            throw new InvalidOperationException("Invalid window handle.");
        }

        return GetWindowRect(handle);
    }

    public static Rectangle GetWindowRect(IntPtr hwnd)
    {
        if (hwnd == IntPtr.Zero)
        {
            throw new InvalidOperationException("Invalid window handle.");
        }

        PInvoke.GetWindowRect(new HWND(hwnd), out RECT rect);
        return new Rectangle(rect.left, rect.top, rect.right - rect.left, rect.bottom - rect.top);
    }

    // Beginning of IntegrationHelper class being integrated into WindowsAPI class

    private static readonly string ApplicationPath = $"\"{AppDomain.CurrentDomain.BaseDirectory}\"";

    private static readonly string ShellExtMenuName = SnapX.AppName;
    private static readonly string ShellExtMenuFiles = $@"Software\Classes\*\shell\{ShellExtMenuName}";
    private static readonly string ShellExtMenuFilesCmd = $@"{ShellExtMenuFiles}\command";
    private static readonly string ShellExtMenuDirectory = $@"Software\Classes\Directory\shell\{ShellExtMenuName}";
    private static readonly string ShellExtMenuDirectoryCmd = $@"{ShellExtMenuDirectory}\command";
    private static readonly string ShellExtDesc = Lang.UploadWithSnapX;
    private static readonly string ShellExtIcon = $"{ApplicationPath},0";
    private static readonly string ShellExtPath = $"{ApplicationPath} \"%1\"";

    private static readonly string ShellExtEditName = "SnapXImageEditor";

    private static readonly string ShellExtEditImage =
        $@"Software\Classes\SystemFileAssociations\image\shell\{ShellExtEditName}";

    private static readonly string ShellExtEditImageCmd = $@"{ShellExtEditImage}\command";
    private static readonly string ShellExtEditDesc = Lang.EditWithSnapX;
    private static readonly string ShellExtEditIcon = $"{ApplicationPath},0";
    private static readonly string ShellExtEditPath = $"{ApplicationPath} -ImageEditor \"%1\"";

    private static readonly string ShellCustomUploaderExtensionPath = @"Software\Classes\.sxcu";
    private static readonly string ShellCustomUploaderExtensionValue = "SnapX.sxcu";

    private static readonly string ShellCustomUploaderAssociatePath =
        $@"Software\Classes\{ShellCustomUploaderExtensionValue}";

    private static readonly string ShellCustomUploaderAssociateValue = "SnapX custom uploader";
    private static readonly string ShellCustomUploaderIconPath = $@"{ShellCustomUploaderAssociatePath}\DefaultIcon";
    private static readonly string ShellCustomUploaderIconValue = $"{ApplicationPath},0";

    private static readonly string ShellCustomUploaderCommandPath =
        $@"{ShellCustomUploaderAssociatePath}\shell\open\command";

    private static readonly string ShellCustomUploaderCommandValue = $"{ApplicationPath} -CustomUploader \"%1\"";

    private static readonly string ShellImageEffectExtensionPath = @"Software\Classes\.sxie";
    private static readonly string ShellImageEffectExtensionValue = "SnapX.sxie";

    private static readonly string ShellImageEffectAssociatePath =
        $@"Software\Classes\{ShellImageEffectExtensionValue}";

    private static readonly string ShellImageEffectAssociateValue = "SnapX image effect";
    private static readonly string ShellImageEffectIconPath = $@"{ShellImageEffectAssociatePath}\DefaultIcon";
    private static readonly string ShellImageEffectIconValue = $"{ApplicationPath},0";
    private static readonly string ShellImageEffectCommandPath = $@"{ShellImageEffectAssociatePath}\shell\open\command";
    private static readonly string ShellImageEffectCommandValue = $"{ApplicationPath} -ImageEffect \"%1\"";

    private static readonly string ChromeNativeMessagingHosts =
        @"SOFTWARE\Google\Chrome\NativeMessagingHosts\io.github.brycensranch.snapx";

    private static readonly string FirefoxNativeMessagingHosts = @"SOFTWARE\Mozilla\NativeMessagingHosts\SnapX";

    private static readonly string ChromeHostManifestFilePath =
        FileHelpers.GetAbsolutePath(Path.Combine("Resources", "host-manifest-chrome.json"));

    private static readonly string FirefoxHostManifestFilePath =
        FileHelpers.GetAbsolutePath(Path.Combine("Resources", "host-manifest-firefox.json"));

    public static bool CheckShellContextMenuButton()
    {
        try
        {
            return CheckStringValue(ShellExtMenuFilesCmd, null, ShellExtPath) &&
                   CheckStringValue(ShellExtMenuDirectoryCmd, null, ShellExtPath);
        }
        catch (Exception e)
        {
            DebugHelper.WriteException(e);
        }

        return false;
    }

    public static void CreateShellContextMenuButton(bool create)
    {
        try
        {
            if (create)
            {
                UnregisterShellContextMenuButton();
                RegisterShellContextMenuButton();
            }
            else
            {
                UnregisterShellContextMenuButton();
            }
        }
        catch (Exception e)
        {
            DebugHelper.WriteException(e);
        }
    }

    private static void RegisterShellContextMenuButton()
    {
        CreateRegistry(ShellExtMenuFiles, ShellExtDesc);
        CreateRegistry(ShellExtMenuFiles, "Icon", ShellExtIcon);
        CreateRegistry(ShellExtMenuFilesCmd, ShellExtPath);

        CreateRegistry(ShellExtMenuDirectory, ShellExtDesc);
        CreateRegistry(ShellExtMenuDirectory, "Icon", ShellExtIcon);
        CreateRegistry(ShellExtMenuDirectoryCmd, ShellExtPath);
    }

    private static void UnregisterShellContextMenuButton()
    {
        RemoveRegistry(ShellExtMenuFiles);
        RemoveRegistry(ShellExtMenuDirectory);
    }

    public static bool CheckEditShellContextMenuButton()
    {
        try
        {
            return CheckStringValue(ShellExtEditImageCmd, null, ShellExtEditPath);
        }
        catch (Exception e)
        {
            DebugHelper.WriteException(e);
        }

        return false;
    }

    public static void CreateEditShellContextMenuButton(bool create)
    {
        try
        {
            if (create)
            {
                UnregisterEditShellContextMenuButton();
                RegisterEditShellContextMenuButton();
            }
            else
            {
                UnregisterEditShellContextMenuButton();
            }
        }
        catch (Exception e)
        {
            DebugHelper.WriteException(e);
        }
    }

    private static void RegisterEditShellContextMenuButton()
    {
        CreateRegistry(ShellExtEditImage, ShellExtEditDesc);
        CreateRegistry(ShellExtEditImage, "Icon", ShellExtEditIcon);
        CreateRegistry(ShellExtEditImageCmd, ShellExtEditPath);
    }

    private static void UnregisterEditShellContextMenuButton()
    {
        RemoveRegistry(ShellExtEditImage);
    }

    public static bool CheckCustomUploaderExtension()
    {
        try
        {
            return CheckStringValue(ShellCustomUploaderExtensionPath, null,
                       ShellCustomUploaderExtensionValue) &&
                   CheckStringValue(ShellCustomUploaderCommandPath, null,
                       ShellCustomUploaderCommandValue);
        }
        catch (Exception e)
        {
            DebugHelper.WriteException(e);
        }

        return false;
    }

    public static void CreateCustomUploaderExtension(bool create)
    {
        try
        {
            if (create)
            {
                UnregisterCustomUploaderExtension();
                RegisterCustomUploaderExtension();
            }
            else
            {
                UnregisterCustomUploaderExtension();
            }
        }
        catch (Exception e)
        {
            DebugHelper.WriteException(e);
        }
    }

    private static void RegisterCustomUploaderExtension()
    {
        CreateRegistry(ShellCustomUploaderExtensionPath, ShellCustomUploaderExtensionValue);
        CreateRegistry(ShellCustomUploaderAssociatePath, ShellCustomUploaderAssociateValue);
        CreateRegistry(ShellCustomUploaderIconPath, ShellCustomUploaderIconValue);
        CreateRegistry(ShellCustomUploaderCommandPath, ShellCustomUploaderCommandValue);
        unsafe
        {
            PInvoke.SHChangeNotify(SHCNE_ID.SHCNE_ASSOCCHANGED, SHCNF_FLAGS.SHCNF_FLUSH);
        }
    }
    private static void UnregisterCustomUploaderExtension()
    {
        RemoveRegistry(ShellCustomUploaderExtensionPath);
        RemoveRegistry(ShellCustomUploaderAssociatePath);
    }

    public static bool CheckImageEffectExtension()
    {
        try
        {
            return CheckStringValue(ShellImageEffectExtensionPath, null,
                       ShellImageEffectExtensionValue) &&
                   CheckStringValue(ShellImageEffectCommandPath, null, ShellImageEffectCommandValue);
        }
        catch (Exception e)
        {
            DebugHelper.WriteException(e);
        }

        return false;
    }

    public static void CreateImageEffectExtension(bool create)
    {
        try
        {
            if (create)
            {
                UnregisterImageEffectExtension();
                RegisterImageEffectExtension();
            }
            else
            {
                UnregisterImageEffectExtension();
            }
        }
        catch (Exception e)
        {
            DebugHelper.WriteException(e);
        }
    }

    private static void RegisterImageEffectExtension()
    {
        CreateRegistry(ShellImageEffectExtensionPath, ShellImageEffectExtensionValue);
        CreateRegistry(ShellImageEffectAssociatePath, ShellImageEffectAssociateValue);
        CreateRegistry(ShellImageEffectIconPath, ShellImageEffectIconValue);
        CreateRegistry(ShellImageEffectCommandPath, ShellImageEffectCommandValue);
        unsafe
        {
            PInvoke.SHChangeNotify(SHCNE_ID.SHCNE_ASSOCCHANGED, SHCNF_FLAGS.SHCNF_FLUSH);
        }
    }

    private static void UnregisterImageEffectExtension()
    {
        RemoveRegistry(ShellImageEffectExtensionPath);
        RemoveRegistry(ShellImageEffectAssociatePath);
    }

    public static bool CheckChromeExtensionSupport()
    {
        try
        {
            return CheckStringValue(ChromeNativeMessagingHosts, null, ChromeHostManifestFilePath) &&
                   File.Exists(ChromeHostManifestFilePath);
        }
        catch (Exception e)
        {
            DebugHelper.WriteException(e);
        }

        return false;
    }

    public static void CreateChromeExtensionSupport(bool create)
    {
        try
        {
            if (create)
            {
                UnregisterChromeExtensionSupport();
                RegisterChromeExtensionSupport();
            }
            else
            {
                UnregisterChromeExtensionSupport();
            }
        }
        catch (Exception e)
        {
            DebugHelper.WriteException(e);
        }
    }

    private static void RegisterChromeExtensionSupport()
    {
        CreateRegistry(ChromeNativeMessagingHosts, ChromeHostManifestFilePath);
    }

    private static void UnregisterChromeExtensionSupport()
    {
        RemoveRegistry(ChromeNativeMessagingHosts);
    }

    public static bool CheckFirefoxAddonSupport()
    {
        try
        {
            return CheckStringValue(FirefoxNativeMessagingHosts, null, FirefoxHostManifestFilePath) &&
                   File.Exists(FirefoxHostManifestFilePath);
        }
        catch (Exception e)
        {
            DebugHelper.WriteException(e);
        }

        return false;
    }

    public static void CreateFirefoxAddonSupport(bool create)
    {
        try
        {
            if (create)
            {
                UnregisterFirefoxAddonSupport();
                RegisterFirefoxAddonSupport();
            }
            else
            {
                UnregisterFirefoxAddonSupport();
            }
        }
        catch (Exception e)
        {
            DebugHelper.WriteException(e);
        }
    }

    private static void RegisterFirefoxAddonSupport()
    {
        CreateRegistry(FirefoxNativeMessagingHosts, FirefoxHostManifestFilePath);
    }

    private static void UnregisterFirefoxAddonSupport()
    {
        RemoveRegistry(FirefoxNativeMessagingHosts);
    }

    [RequiresAssemblyFiles()]
    public static bool CheckSendToMenuButton()
    {
        return CheckShortcut(Environment.SpecialFolder.SendTo, SnapX.AppName, Assembly.GetEntryAssembly()!.Location);
    }

    [RequiresAssemblyFiles()]
    public static bool CreateSendToMenuButton(bool create)
    {
        return SetShortcut(create, Environment.SpecialFolder.SendTo, SnapX.AppName,
            Assembly.GetEntryAssembly()!.Location);
    }
    public static void CreateRegistry(string path, string value, RegistryHive root = RegistryHive.CurrentUser)
    {
        CreateRegistry(path, null, value, root);
    }

    public static void CreateRegistry(string path, string name, string value, RegistryHive root = RegistryHive.CurrentUser)
    {
        using var rk = RegistryKey.OpenBaseKey(root, RegistryView.Default).CreateSubKey(path);
        if (rk != null)
        {
            rk.SetValue(name, value, RegistryValueKind.String);
        }
    }

    public static void CreateRegistry(string path, int value, RegistryHive root = RegistryHive.CurrentUser)
    {
        CreateRegistry(path, null, value, root);
    }

    public static void CreateRegistry(string path, string name, int value, RegistryHive root = RegistryHive.CurrentUser)
    {
        using var rk = RegistryKey.OpenBaseKey(root, RegistryView.Default).CreateSubKey(path);
        if (rk != null)
        {
            rk.SetValue(name, value, RegistryValueKind.DWord);
        }
    }

    public static void RemoveRegistry(string path, RegistryHive root = RegistryHive.CurrentUser)
    {
        if (string.IsNullOrEmpty(path)) return;
        using RegistryKey rk = RegistryKey.OpenBaseKey(root, RegistryView.Default);
        rk.DeleteSubKeyTree(path, false);
    }

    public static object GetValue(string path, string name = null, RegistryHive root = RegistryHive.CurrentUser, RegistryView view = RegistryView.Default)
    {
        try
        {
            using var baseKey = RegistryKey.OpenBaseKey(root, view);
            using var rk = baseKey.OpenSubKey(path);
            if (rk != null)
            {
                return rk.GetValue(name);
            }
        }
        catch (Exception e)
        {
            DebugHelper.WriteException(e);
        }

        return null;
    }

    public static string GetValueString(string path, string name = null, RegistryHive root = RegistryHive.CurrentUser, RegistryView view = RegistryView.Default)
    {
        return GetValue(path, name, root, view) as string;
    }

    public static int? GetValueDWord(string path, string name = null, RegistryHive root = RegistryHive.CurrentUser, RegistryView view = RegistryView.Default)
    {
        return (int?)GetValue(path, name, root, view);
    }

    public static bool CheckStringValue(string path, string name = null, string value = null, RegistryHive root = RegistryHive.CurrentUser, RegistryView view = RegistryView.Default)
    {
        var registryValue = GetValueString(path, name, root, view);

        return registryValue != null && (value == null || registryValue.Equals(value, StringComparison.OrdinalIgnoreCase));
    }

    public static string SearchProgramPath(string fileName)
    {
        // First method: HKEY_CLASSES_ROOT\Applications\{fileName}\shell\{command}\command

        string[] commands = ["open", "edit"];

        foreach (string command in commands)
        {
            string path = $@"HKEY_CLASSES_ROOT\Applications\{fileName}\shell\{command}\command";
            string value = Registry.GetValue(path, null, null) as string;

            if (!string.IsNullOrEmpty(value))
            {
                string filePath = value.ParseQuoteString();

                if (File.Exists(filePath))
                {
                    DebugHelper.WriteLine("Found program with first method: " + filePath);
                    return filePath;
                }
            }
        }

        // Second method: HKEY_CURRENT_USER\Software\Classes\Local Settings\Software\Microsoft\Windows\Shell\MuiCache

        using RegistryKey programs =
            Registry.CurrentUser.OpenSubKey(
                @"Software\Classes\Local Settings\Software\Microsoft\Windows\Shell\MuiCache");
        if (programs != null)
        {
            foreach (string filePath in programs.GetValueNames())
            {
                string programPath = filePath;

                if (!string.IsNullOrEmpty(programPath))
                {
                    foreach (string trim in new string[] { ".ApplicationCompany", ".FriendlyAppName" })
                    {
                        if (programPath.EndsWith(trim, StringComparison.OrdinalIgnoreCase))
                        {
                            programPath = programPath.Remove(programPath.Length - trim.Length);
                        }
                    }

                    if (programPath.EndsWith(fileName, StringComparison.OrdinalIgnoreCase) && File.Exists(programPath))
                    {
                        DebugHelper.WriteLine("Found program with second method: " + programPath);
                        return programPath;
                    }
                }
            }
        }

        return null;
    }
    public static bool SetShortcut(bool create, Environment.SpecialFolder specialFolder, string shortcutName, string targetPath, string arguments = "")
    {
        string shortcutPath = GetShortcutPath(specialFolder, shortcutName);
        return SetShortcut(create, shortcutPath, targetPath, arguments);
    }

    public static bool SetShortcut(bool create, string shortcutPath, string targetPath, string arguments = "")
    {
        try
        {
            if (create)
            {
                return CreateShortcut(shortcutPath, targetPath, arguments);
            }
            else
            {
                return DeleteShortcut(shortcutPath);
            }
        }
        catch (Exception e)
        {
            DebugHelper.WriteException(e);
            e.ShowError();
        }

        return false;
    }

    public static bool CheckShortcut(Environment.SpecialFolder specialFolder, string shortcutName, string targetPath)
    {
        string shortcutPath = GetShortcutPath(specialFolder, shortcutName);
        return CheckShortcut(shortcutPath, targetPath);
    }

    public static bool CheckShortcut(string shortcutPath, string targetPath)
    {
        if (!string.IsNullOrEmpty(shortcutPath) && !string.IsNullOrEmpty(targetPath) && File.Exists(shortcutPath))
        {
            try
            {
                string shortcutTargetPath = GetShortcutTargetPath(shortcutPath);
                return !string.IsNullOrEmpty(shortcutTargetPath) && shortcutTargetPath.Equals(targetPath, StringComparison.OrdinalIgnoreCase);
            }
            catch (Exception e)
            {
                DebugHelper.WriteException(e);
            }
        }

        return false;
    }

    private static string GetShortcutPath(Environment.SpecialFolder specialFolder, string shortcutName)
    {
        string folderPath = Environment.GetFolderPath(specialFolder);

        if (!shortcutName.EndsWith(".lnk", StringComparison.OrdinalIgnoreCase))
        {
            shortcutName += ".lnk";
        }

        return Path.Combine(folderPath, shortcutName);
    }

    private static bool CreateShortcut(string shortcutPath, string targetPath, string arguments = "")
    {
        if (!string.IsNullOrEmpty(shortcutPath) && !string.IsNullOrEmpty(targetPath) && File.Exists(targetPath))
        {
            DeleteShortcut(shortcutPath);

            string script = $"""
                             $WshShell = New-Object -ComObject WScript.Shell
                             $Shortcut = $WshShell.CreateShortcut('{shortcutPath}')
                             $Shortcut.TargetPath = '{targetPath}'
                             $Shortcut.Arguments = '{arguments}'
                             $Shortcut.WorkingDirectory = '{Path.GetDirectoryName(targetPath)}'
                             $Shortcut.Save()
                             """;

            using var ps = PowerShell.Create();
            ps.AddScript(script);
            ps.Invoke();

            DebugHelper.WriteLine("Shortcut created successfully using PowerShell.");

            return true;
        }

        return false;
    }

    private static string GetShortcutTargetPath(string shortcutPath)
    {
        string script = $"""
                         $WshShell = New-Object -ComObject WScript.Shell
                         $Shortcut = $WshShell.CreateShortcut('{shortcutPath}')
                         $Shortcut.TargetPath
                         """;

        using var ps = PowerShell.Create();
        ps.AddScript(script);
        var result = ps.Invoke();

        if (result.Count > 0)
        {
            return result[0].ToString();
        }
        else
        {
            throw new InvalidOperationException("Failed to retrieve shortcut target path.");
        }
    }

    private static bool DeleteShortcut(string shortcutPath)
    {
        if (!string.IsNullOrEmpty(shortcutPath) && File.Exists(shortcutPath))
        {
            File.Delete(shortcutPath);
            return true;
        }

        return false;
    }
}
