using System.Diagnostics.CodeAnalysis;
using System.Management.Automation;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Text;
using Microsoft.Win32;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;
using SnapX.Core.Media;
using SnapX.Core.Utils.Extensions;

namespace SnapX.Core.Utils.Native;

[SupportedOSPlatform("windows")]
public class WindowsAPI : NativeAPI
{
    // Constants for Windows semantics
    private const int SW_HIDE = 0; // Hide the window
    private const int SW_SHOW = 5; // Show the window
    private const int SW_MINIMIZE = 6; // Minimize the window
    private const int SW_RESTORE = 9; // Restore the window
    private const int SW_SHOWDEFAULT = 10;
    private const int RetryTimes = 20;

    private const int RetryDelay = 100;

    // Constants for allocating memory and setting data format
    public const uint CF_TEXT = 1;
    private const uint CF_DIB = 8;
    public const uint CF_UNICODETEXT = 13;
    public const int SRCCOPY = 0x00CC0020;

    public const int GMEM_ZEROINIT = 0x0040;

    private static readonly object ClipboardLock = new();


    public override void ShowWindow(WindowInfo Window)
    {
        var handle = Window.Handle;
        if (handle == IntPtr.Zero)
        {
            throw new InvalidOperationException("Invalid window handle.");
        }

        ShowWindow(handle, SW_SHOW);

    }

    public override void ShowWindow(IntPtr handle)
    {
        if (handle == IntPtr.Zero)
        {
            throw new InvalidOperationException("Invalid window handle.");
        }

        ShowWindow(handle, SW_SHOW);

    }
    [DllImport("user32.dll", SetLastError = true)]
    public static extern bool EnumWindows(EnumWindowsProc enumProc, IntPtr lParam);

    [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
    public static extern int GetWindowText(IntPtr hwnd, StringBuilder lpString, int nMaxCount);

    [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
    public static extern int GetClassName(IntPtr hwnd, StringBuilder lpClassName, int nMaxCount);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool IsWindowVisible(IntPtr hwnd);

    [DllImport("user32.dll")]
    public static extern bool GetWindowPlacement(IntPtr hwnd, ref WINDOWPLACEMENT lpwndpl);

    // Struct to hold window placement (minimized, maximized, etc.)
    [StructLayout(LayoutKind.Sequential)]
    public struct WINDOWPLACEMENT
    {
        public int Length;
        public int ShowCmd;
        public System.Drawing.Point PtMinPosition;
        public System.Drawing.Point PtMaxPosition;
        public RECT rcNormalPosition;
    }
    public static (int X, int Y) GetWindowPosition(IntPtr hwnd)
    {
        RECT rect;
        GetWindowRect(hwnd, out rect);
        return (rect.Left, rect.Top);
    }

    // Method to check if a window is minimized
    public static bool IsWindowMinimized(IntPtr hwnd)
    {
        WINDOWPLACEMENT placement = new WINDOWPLACEMENT();
        placement.Length = Marshal.SizeOf(typeof(WINDOWPLACEMENT));
        if (GetWindowPlacement(hwnd, ref placement))
        {
            return placement.ShowCmd == 2; // SW_MINIMIZE = 2
        }
        return false;
    }

    // Method to check if the window is the active (foreground) window
    public static bool IsWindowActive(IntPtr hwnd)
    {
        var activeWindow = GetForegroundWindow();
        return hwnd == activeWindow;
    }


    // Delegate type for EnumWindowsProc
    public delegate bool EnumWindowsProc(IntPtr hwnd, IntPtr lParam);

    // The method that is called by EnumWindows
    public static bool EnumWindowsCallback(IntPtr hwnd, IntPtr lParam)
    {
        // We are only interested in top-level windows that are visible
        if (!IsWindowVisible(hwnd))
            return true;

        var windowTitle = new StringBuilder(256);
        GetWindowText(hwnd, windowTitle, 256);

        // If the window has a non-empty title, add it to the list
        if (windowTitle.Length > 0)
        {
            var (X, Y) = GetWindowPosition(hwnd);
            var windowRECT = GetWindowRect(hwnd);
            var windowInfo = new WindowInfo
            {
                Handle = hwnd,
                Title = windowTitle.ToString(),
                Rectangle = windowRECT,
                IsVisible = IsWindowVisible(hwnd),
                IsMinimized = IsWindowMinimized(hwnd),
                IsActive = IsWindowActive(hwnd)
            };

            // Add the window to the global list
            windowList.Add(windowInfo);
        }

        return true; // Continue enumeration
    }

    private static List<WindowInfo> windowList = [];

    public override List<WindowInfo> GetWindowList()
    {
        windowList.Clear();
        EnumWindows(EnumWindowsCallback, IntPtr.Zero);
        return windowList;
    }

    public override void CopyText(string text)
    {
        if (!OpenClipboard(IntPtr.Zero))
        {
            throw new AccessViolationException("Failed to open clipboard.");
        }

        // Empty the clipboard
        EmptyClipboard();

        // Allocate global memory for the text
        IntPtr hGlobal = GlobalAlloc(GMEM_ZEROINIT, (uint)(text.Length + 1) * sizeof(char));

        // Lock the memory so that we can copy the text into it
        IntPtr lpGlobal = GlobalLock(hGlobal);

        // Copy the text into the allocated memory
        Marshal.Copy(text.ToCharArray(), 0, lpGlobal, text.Length);

        // Unlock the memory
        GlobalUnlock(hGlobal);

        // Set the clipboard data (CF_TEXT is used for plain text)
        SetClipboardData(CF_TEXT, hGlobal);

        // Close the clipboard
        CloseClipboard();

    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool OpenClipboard(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern bool CloseClipboard();

    [DllImport("user32.dll")]
    private static extern bool EmptyClipboard();

    [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
    private static extern IntPtr GlobalAlloc(int uFlags, uint dwBytes);

    [DllImport("kernel32.dll")]
    private static extern IntPtr GlobalLock(IntPtr hMem);

    [DllImport("kernel32.dll")]
    private static extern bool GlobalUnlock(IntPtr hMem);
    [DllImport("kernel32.dll")]
    private static extern bool GlobalFree(IntPtr hMem);
    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr SetClipboardData(uint uFormat, IntPtr data);

    [DllImport("user32.dll")]
    private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    [DllImport("user32.dll")]
    private static extern bool GetWindowRect(IntPtr hwnd, out RECT rect);
    [StructLayout(LayoutKind.Sequential)]
    public struct WinPoint
    {
        public int X;
        public int Y;
        public WinPoint(int x, int y) { X = x; Y = y; }
    }
    [DllImport("user32.dll")]
    private static extern bool GetCursorPos(out WinPoint lpPoint);

    public override Point GetCursorPosition()
    {
        GetCursorPos(out var LpPoint);
        return new Point(LpPoint.X, LpPoint.Y);
    }
    public override void CopyImage(Image image, string filename = null)
    {
        OpenClipboard(IntPtr.Zero);
        EmptyClipboard();

        using var ms = new MemoryStream();
        var format = image.Metadata.DecodedImageFormat ?? PngFormat.Instance;
        image.Save(ms, format);
        var imageBytes = ms.ToArray();

        var dataSize = (uint)imageBytes.Length;
        var dataPtr = GlobalAlloc(0x0042, dataSize);
        if (dataPtr == IntPtr.Zero) return;

        var lockedData = GlobalLock(dataPtr);
        if (lockedData == IntPtr.Zero)
        {
            GlobalFree(dataPtr);
            return;
        }

        Marshal.Copy(imageBytes, 0, lockedData, imageBytes.Length);
        GlobalUnlock(dataPtr);

        SetClipboardData(CF_DIB, dataPtr);
        CloseClipboard();
    }
    public override Rectangle GetWindowRectangle(WindowInfo Window)
    {
        var handle = Window.Handle;
        if (handle == IntPtr.Zero)
        {
            throw new InvalidOperationException("Invalid window handle.");
        }

        GetWindowRect(handle, out RECT rect);
        return new Rectangle(rect.Left, rect.Top, rect.Right - rect.Left, rect.Bottom - rect.Top);
    }

    public static Rectangle GetWindowRect(IntPtr handle)
    {
        if (handle == IntPtr.Zero)
        {
            throw new InvalidOperationException("Invalid window handle.");
        }

        GetWindowRect(handle, out RECT rect);
        return new Rectangle(rect.Left, rect.Top, rect.Right - rect.Left, rect.Bottom - rect.Top);
    }
    [DllImport("gdi32.dll")]
    public static extern IntPtr CreateCompatibleDC(IntPtr hdc);

    [DllImport("gdi32.dll")]
    public static extern IntPtr CreateCompatibleBitmap(IntPtr hdc, int width, int height);

    [DllImport("gdi32.dll")]
    public static extern IntPtr SelectObject(IntPtr hdc, IntPtr h);

    [DllImport("gdi32.dll")]
    public static extern bool BitBlt(IntPtr hdcDest, int xDest, int yDest, int width, int height, IntPtr hdcSrc, int xSrc, int ySrc, uint rop);

    [DllImport("gdi32.dll")]
    public static extern bool DeleteDC(IntPtr hdc);

    [DllImport("gdi32.dll")]
    public static extern bool DeleteObject(IntPtr hObject);

    [DllImport("user32.dll")]
    public static extern IntPtr GetDC(IntPtr hwnd);
    [DllImport("gdi32.dll")]
    public static extern int GetDIBits(IntPtr hdc, IntPtr hBitmap, uint uStartScan, uint cScanLines, IntPtr lpvBits, ref BITMAPINFO lpbi, uint uUsage);

    [DllImport("user32.dll")]
    public static extern int ReleaseDC(IntPtr hwnd, IntPtr hdc);

    [DllImport("user32.dll")]
    public static extern IntPtr GetForegroundWindow();

    [StructLayout(LayoutKind.Sequential)]
    public struct RECT
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }
    [StructLayout(LayoutKind.Sequential)]
    public struct BITMAPINFO
    {
        public int biSize;
        public int biWidth;
        public int biHeight;
        public short biPlanes;
        public short biBitCount;
        public int biCompression;
        public int biSizeImage;
        public int biXPelsPerMeter;
        public int biYPelsPerMeter;
        public int biClrUsed;
        public int biClrImportant;
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

        SHChangeNotify(HChangeNotifyEventID.SHCNE_ASSOCCHANGED, HChangeNotifyFlags.SHCNF_FLUSH,
            IntPtr.Zero, IntPtr.Zero);
    }
    /// <summary>
    /// Describes the event that has occurred.
    /// Typically, only one event is specified at a time.
    /// If more than one event is specified, the values contained
    /// in the <i>dwItem1</i> and <i>dwItem2</i>
    /// parameters must be the same, respectively, for all specified events.
    /// This parameter can be one or more of the following values.
    /// </summary>
    /// <remarks>
    /// <para><b>Windows NT/2000/XP:</b> <i>dwItem2</i> contains the index
    /// in the system image list that has changed.
    /// <i>dwItem1</i> is not used and should be <see langword="null"/>.</para>
    /// <para><b>Windows 95/98:</b> <i>dwItem1</i> contains the index
    /// in the system image list that has changed.
    /// <i>dwItem2</i> is not used and should be <see langword="null"/>.</para>
    /// </remarks>
    [Flags]
    public enum HChangeNotifyEventID
    {
        /// <summary>
        /// All events have occurred.
        /// </summary>
        SHCNE_ALLEVENTS = 0x7FFFFFFF,

        /// <summary>
        /// A file type association has changed. <see cref="HChangeNotifyFlags.SHCNF_IDLIST"/>
        /// must be specified in the <i>uFlags</i> parameter.
        /// <i>dwItem1</i> and <i>dwItem2</i> are not used and must be <see langword="null"/>.
        /// </summary>
        SHCNE_ASSOCCHANGED = 0x08000000,

        /// <summary>
        /// The attributes of an item or folder have changed.
        /// <see cref="HChangeNotifyFlags.SHCNF_IDLIST"/> or
        /// <see cref="HChangeNotifyFlags.SHCNF_PATH"/> must be specified in <i>uFlags</i>.
        /// <i>dwItem1</i> contains the item or folder that has changed.
        /// <i>dwItem2</i> is not used and should be <see langword="null"/>.
        /// </summary>
        SHCNE_ATTRIBUTES = 0x00000800,

        /// <summary>
        /// A nonfolder item has been created.
        /// <see cref="HChangeNotifyFlags.SHCNF_IDLIST"/> or
        /// <see cref="HChangeNotifyFlags.SHCNF_PATH"/> must be specified in <i>uFlags</i>.
        /// <i>dwItem1</i> contains the item that was created.
        /// <i>dwItem2</i> is not used and should be <see langword="null"/>.
        /// </summary>
        SHCNE_CREATE = 0x00000002,

        /// <summary>
        /// A nonfolder item has been deleted.
        /// <see cref="HChangeNotifyFlags.SHCNF_IDLIST"/> or
        /// <see cref="HChangeNotifyFlags.SHCNF_PATH"/> must be specified in <i>uFlags</i>.
        /// <i>dwItem1</i> contains the item that was deleted.
        /// <i>dwItem2</i> is not used and should be <see langword="null"/>.
        /// </summary>
        SHCNE_DELETE = 0x00000004,

        /// <summary>
        /// A drive has been added.
        /// <see cref="HChangeNotifyFlags.SHCNF_IDLIST"/> or
        /// <see cref="HChangeNotifyFlags.SHCNF_PATH"/> must be specified in <i>uFlags</i>.
        /// <i>dwItem1</i> contains the root of the drive that was added.
        /// <i>dwItem2</i> is not used and should be <see langword="null"/>.
        /// </summary>
        SHCNE_DRIVEADD = 0x00000100,

        /// <summary>
        /// A drive has been added and the Shell should create a new window for the drive.
        /// <see cref="HChangeNotifyFlags.SHCNF_IDLIST"/> or
        /// <see cref="HChangeNotifyFlags.SHCNF_PATH"/> must be specified in <i>uFlags</i>.
        /// <i>dwItem1</i> contains the root of the drive that was added.
        /// <i>dwItem2</i> is not used and should be <see langword="null"/>.
        /// </summary>
        SHCNE_DRIVEADDGUI = 0x00010000,

        /// <summary>
        /// A drive has been removed. <see cref="HChangeNotifyFlags.SHCNF_IDLIST"/> or
        /// <see cref="HChangeNotifyFlags.SHCNF_PATH"/> must be specified in <i>uFlags</i>.
        /// <i>dwItem1</i> contains the root of the drive that was removed.
        /// <i>dwItem2</i> is not used and should be <see langword="null"/>.
        /// </summary>
        SHCNE_DRIVEREMOVED = 0x00000080,

        /// <summary>
        /// Not currently used.
        /// </summary>
        SHCNE_EXTENDED_EVENT = 0x04000000,

        /// <summary>
        /// The amount of free space on a drive has changed.
        /// <see cref="HChangeNotifyFlags.SHCNF_IDLIST"/> or
        /// <see cref="HChangeNotifyFlags.SHCNF_PATH"/> must be specified in <i>uFlags</i>.
        /// <i>dwItem1</i> contains the root of the drive on which the free space changed.
        /// <i>dwItem2</i> is not used and should be <see langword="null"/>.
        /// </summary>
        SHCNE_FREESPACE = 0x00040000,

        /// <summary>
        /// Storage media has been inserted into a drive.
        /// <see cref="HChangeNotifyFlags.SHCNF_IDLIST"/> or
        /// <see cref="HChangeNotifyFlags.SHCNF_PATH"/> must be specified in <i>uFlags</i>.
        /// <i>dwItem1</i> contains the root of the drive that contains the new media.
        /// <i>dwItem2</i> is not used and should be <see langword="null"/>.
        /// </summary>
        SHCNE_MEDIAINSERTED = 0x00000020,

        /// <summary>
        /// Storage media has been removed from a drive.
        /// <see cref="HChangeNotifyFlags.SHCNF_IDLIST"/> or
        /// <see cref="HChangeNotifyFlags.SHCNF_PATH"/> must be specified in <i>uFlags</i>.
        /// <i>dwItem1</i> contains the root of the drive from which the media was removed.
        /// <i>dwItem2</i> is not used and should be <see langword="null"/>.
        /// </summary>
        SHCNE_MEDIAREMOVED = 0x00000040,

        /// <summary>
        /// A folder has been created. <see cref="HChangeNotifyFlags.SHCNF_IDLIST"/>
        /// or <see cref="HChangeNotifyFlags.SHCNF_PATH"/> must be specified in <i>uFlags</i>.
        /// <i>dwItem1</i> contains the folder that was created.
        /// <i>dwItem2</i> is not used and should be <see langword="null"/>.
        /// </summary>
        SHCNE_MKDIR = 0x00000008,

        /// <summary>
        /// A folder on the local computer is being shared via the network.
        /// <see cref="HChangeNotifyFlags.SHCNF_IDLIST"/> or
        /// <see cref="HChangeNotifyFlags.SHCNF_PATH"/> must be specified in <i>uFlags</i>.
        /// <i>dwItem1</i> contains the folder that is being shared.
        /// <i>dwItem2</i> is not used and should be <see langword="null"/>.
        /// </summary>
        SHCNE_NETSHARE = 0x00000200,

        /// <summary>
        /// A folder on the local computer is no longer being shared via the network.
        /// <see cref="HChangeNotifyFlags.SHCNF_IDLIST"/> or
        /// <see cref="HChangeNotifyFlags.SHCNF_PATH"/> must be specified in <i>uFlags</i>.
        /// <i>dwItem1</i> contains the folder that is no longer being shared.
        /// <i>dwItem2</i> is not used and should be <see langword="null"/>.
        /// </summary>
        SHCNE_NETUNSHARE = 0x00000400,

        /// <summary>
        /// The name of a folder has changed.
        /// <see cref="HChangeNotifyFlags.SHCNF_IDLIST"/> or
        /// <see cref="HChangeNotifyFlags.SHCNF_PATH"/> must be specified in <i>uFlags</i>.
        /// <i>dwItem1</i> contains the previous pointer to an item identifier list (PIDL) or name of the folder.
        /// <i>dwItem2</i> contains the new PIDL or name of the folder.
        /// </summary>
        SHCNE_RENAMEFOLDER = 0x00020000,

        /// <summary>
        /// The name of a nonfolder item has changed.
        /// <see cref="HChangeNotifyFlags.SHCNF_IDLIST"/> or
        /// <see cref="HChangeNotifyFlags.SHCNF_PATH"/> must be specified in <i>uFlags</i>.
        /// <i>dwItem1</i> contains the previous PIDL or name of the item.
        /// <i>dwItem2</i> contains the new PIDL or name of the item.
        /// </summary>
        SHCNE_RENAMEITEM = 0x00000001,

        /// <summary>
        /// A folder has been removed.
        /// <see cref="HChangeNotifyFlags.SHCNF_IDLIST"/> or
        /// <see cref="HChangeNotifyFlags.SHCNF_PATH"/> must be specified in <i>uFlags</i>.
        /// <i>dwItem1</i> contains the folder that was removed.
        /// <i>dwItem2</i> is not used and should be <see langword="null"/>.
        /// </summary>
        SHCNE_RMDIR = 0x00000010,

        /// <summary>
        /// The computer has disconnected from a server.
        /// <see cref="HChangeNotifyFlags.SHCNF_IDLIST"/> or
        /// <see cref="HChangeNotifyFlags.SHCNF_PATH"/> must be specified in <i>uFlags</i>.
        /// <i>dwItem1</i> contains the server from which the computer was disconnected.
        /// <i>dwItem2</i> is not used and should be <see langword="null"/>.
        /// </summary>
        SHCNE_SERVERDISCONNECT = 0x00004000,

        /// <summary>
        /// The contents of an existing folder have changed,
        /// but the folder still exists and has not been renamed.
        /// <see cref="HChangeNotifyFlags.SHCNF_IDLIST"/> or
        /// <see cref="HChangeNotifyFlags.SHCNF_PATH"/> must be specified in <i>uFlags</i>.
        /// <i>dwItem1</i> contains the folder that has changed.
        /// <i>dwItem2</i> is not used and should be <see langword="null"/>.
        /// If a folder has been created, deleted, or renamed, use SHCNE_MKDIR, SHCNE_RMDIR, or
        /// SHCNE_RENAMEFOLDER, respectively, instead.
        /// </summary>
        SHCNE_UPDATEDIR = 0x00001000,

        /// <summary>
        /// An image in the system image list has changed.
        /// <see cref="HChangeNotifyFlags.SHCNF_DWORD"/> must be specified in <i>uFlags</i>.
        /// </summary>
        SHCNE_UPDATEIMAGE = 0x00008000
    }

    /// <summary>
    /// Flags that indicate the meaning of the <i>dwItem1</i> and <i>dwItem2</i> parameters.
    /// The uFlags parameter must be one of the following values.
    /// </summary>
    [Flags]
    public enum HChangeNotifyFlags
    {
        /// <summary>
        /// The <i>dwItem1</i> and <i>dwItem2</i> parameters are DWORD values.
        /// </summary>
        SHCNF_DWORD = 0x0003,
        /// <summary>
        /// <i>dwItem1</i> and <i>dwItem2</i> are the addresses of ITEMIDLIST structures that
        /// represent the item(s) affected by the change.
        /// Each ITEMIDLIST must be relative to the desktop folder.
        /// </summary>
        SHCNF_IDLIST = 0x0000,
        /// <summary>
        /// <i>dwItem1</i> and <i>dwItem2</i> are the addresses of null-terminated strings of
        /// maximum length MAX_PATH that contain the full path names
        /// of the items affected by the change.
        /// </summary>
        SHCNF_PATHA = 0x0001,
        /// <summary>
        /// <i>dwItem1</i> and <i>dwItem2</i> are the addresses of null-terminated strings of
        /// maximum length MAX_PATH that contain the full path names
        /// of the items affected by the change.
        /// </summary>
        SHCNF_PATHW = 0x0005,
        /// <summary>
        /// <i>dwItem1</i> and <i>dwItem2</i> are the addresses of null-terminated strings that
        /// represent the friendly names of the printer(s) affected by the change.
        /// </summary>
        SHCNF_PRINTERA = 0x0002,
        /// <summary>
        /// <i>dwItem1</i> and <i>dwItem2</i> are the addresses of null-terminated strings that
        /// represent the friendly names of the printer(s) affected by the change.
        /// </summary>
        SHCNF_PRINTERW = 0x0006,
        /// <summary>
        /// The function should not return until the notification
        /// has been delivered to all affected components.
        /// As this flag modifies other data-type flags, it cannot by used by itself.
        /// </summary>
        SHCNF_FLUSH = 0x1000,
        /// <summary>
        /// The function should begin delivering notifications to all affected components
        /// but should return as soon as the notification process has begun.
        /// As this flag modifies other data-type flags, it cannot by used by itself.
        /// </summary>
        SHCNF_FLUSHNOWAIT = 0x2000
    }
    [DllImport("shell32.dll")]
    public static extern void SHChangeNotify(HChangeNotifyEventID wEventId, HChangeNotifyFlags uFlags, IntPtr dwItem1, IntPtr dwItem2);


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

        SHChangeNotify(HChangeNotifyEventID.SHCNE_ASSOCCHANGED, HChangeNotifyFlags.SHCNF_FLUSH,
            IntPtr.Zero, IntPtr.Zero);
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
        using (RegistryKey rk = RegistryKey.OpenBaseKey(root, RegistryView.Default).CreateSubKey(path))
        {
            if (rk != null)
            {
                rk.SetValue(name, value, RegistryValueKind.String);
            }
        }
    }

    public static void CreateRegistry(string path, int value, RegistryHive root = RegistryHive.CurrentUser)
    {
        CreateRegistry(path, null, value, root);
    }

    public static void CreateRegistry(string path, string name, int value, RegistryHive root = RegistryHive.CurrentUser)
    {
        using (RegistryKey rk = RegistryKey.OpenBaseKey(root, RegistryView.Default).CreateSubKey(path))
        {
            if (rk != null)
            {
                rk.SetValue(name, value, RegistryValueKind.DWord);
            }
        }
    }

    public static void RemoveRegistry(string path, RegistryHive root = RegistryHive.CurrentUser)
    {
        if (!string.IsNullOrEmpty(path))
        {
            using (RegistryKey rk = RegistryKey.OpenBaseKey(root, RegistryView.Default))
            {
                rk.DeleteSubKeyTree(path, false);
            }
        }
    }

    public static object GetValue(string path, string name = null, RegistryHive root = RegistryHive.CurrentUser, RegistryView view = RegistryView.Default)
    {
        try
        {
            using (RegistryKey baseKey = RegistryKey.OpenBaseKey(root, view))
            using (RegistryKey rk = baseKey.OpenSubKey(path))
            {
                if (rk != null)
                {
                    return rk.GetValue(name);
                }
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

            string script = $@"
$WshShell = New-Object -ComObject WScript.Shell
$Shortcut = $WshShell.CreateShortcut('{shortcutPath}')
$Shortcut.TargetPath = '{targetPath}'
$Shortcut.Arguments = '{arguments}'
$Shortcut.WorkingDirectory = '{Path.GetDirectoryName(targetPath)}'
$Shortcut.Save()
";

            using var ps = PowerShell.Create();
            ps.AddScript(script);
            ps.Invoke();

            Console.WriteLine("Shortcut created successfully using PowerShell.");

            return true;
        }

        return false;
    }

    private static string GetShortcutTargetPath(string shortcutPath)
    {
        string script = $@"
$WshShell = New-Object -ComObject WScript.Shell
$Shortcut = $WshShell.CreateShortcut('{shortcutPath}')
$Shortcut.TargetPath
";

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
