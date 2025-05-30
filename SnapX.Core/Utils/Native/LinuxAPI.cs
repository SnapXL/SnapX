using System.Runtime.InteropServices;
using System.Text;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Bmp;
using SixLabors.ImageSharp.Formats.Png;
using SnapX.Core.Media;

namespace SnapX.Core.Utils.Native;

public class LinuxAPI : NativeAPI
{
    internal const string LibX11 = "libx11.so.6";
    internal static bool IsWayland()
    {
        var display = Environment.GetEnvironmentVariable("WAYLAND_DISPLAY");
        return !string.IsNullOrEmpty(display);
    }

    internal static bool IsPlasma()
    {
        var sessionVersion = Environment.GetEnvironmentVariable("KDE_SESSION_VERSION");
        return !string.IsNullOrEmpty(sessionVersion);
    }

    internal static bool IsGNOME()
    {
        var sessionVersion = Environment.GetEnvironmentVariable("SESSIONTYPE");
        return !string.IsNullOrEmpty(sessionVersion) && sessionVersion.Contains("gnome", StringComparison.OrdinalIgnoreCase);
    }

    public static Rectangle GetWindowRectangle(IntPtr windowHandle)
    {
        return GetWindowRectangleX11(windowHandle);
    }

    public override Screen GetScreen(Point pos)
    {

        IntPtr display = XOpenDisplay(null);
        if (display == IntPtr.Zero)
        {
            Console.WriteLine("Unable to open X11 display.");
            return null;
        }

        int screenCount = XScreenCount(display);
        for (int i = 0; i < screenCount; i++)
        {
            IntPtr rootWindow = XRootWindow(display, i);
            IntPtr geometryRoot;
            int x, y;
            uint width, height, borderWidth, depth;
            XGetGeometry(display, rootWindow, out geometryRoot, out x, out y, out width, out height, out borderWidth, out depth);

            if (pos.X >= x && pos.X <= x + (int)width && pos.Y >= y && pos.Y <= y + (int)height)
            {
                DebugHelper.Logger?.Debug($"Point {pos} is within screen {i} bounds.");
                return new Screen()
                {
                    Bounds = new Rectangle(x, y, (int)width, (int)height),
                    Name = "NotImplementedName",
                    Id = "NotImplementedID"
                };
            }
        }

        XCloseDisplay(display);
        return null;
    }

    public override List<WindowInfo> GetWindowList()
    {
        var windows = new List<WindowInfo>();
        if (IsWayland())
        {
            if (IsPlasma())
            {

                return windows;
            }

            if (IsGNOME())
            {
                return windows;
            }

            return windows;
        }

        var display = XOpenDisplay(null);
        if (display == IntPtr.Zero)
        {
            DebugHelper.Logger?.Debug("Unable to open X display.");
            return windows;
        }

        var root = XDefaultRootWindow(display);  // Get the root window of the X display
        IntPtr parent;
        IntPtr windowsPtr;
        uint nchildren;

        // Get all the child windows of the root window
        int status = XQueryTree(display, root, out root, out parent, out windowsPtr, out nchildren);
        if (status == 0)
        {
            DebugHelper.Logger?.Debug("XQueryTree failed.");
            XCloseDisplay(display);
            return windows;
        }

        // Iterate through the list of child windows
        for (uint i = 0; i < nchildren; i++)
        {
            IntPtr window = Marshal.ReadIntPtr(windowsPtr, (int)(i * IntPtr.Size));
            string title = GetWindowTitle(display, window);
            IntPtr namePtr = IntPtr.Zero;
            IntPtr propReturn;
            uint nitems;
            uint bytesAfter;
            int format;
            int x, y;
            XWindowAttributes attributes;
            uint width, height, borderWidth, depth;
            XGetGeometry(display, window, out root, out x, out y, out width, out height, out borderWidth, out depth);

            XGetWindowAttributes(display, window, out attributes);
            bool isVisible = attributes.is_colormap_installed;

            // Active window
            IntPtr focusWindow;
            int revertTo;
            XGetInputFocus(display, out focusWindow, out revertTo);
            bool isActive = focusWindow == window;
            var rect = GetWindowRectangle(window);
            windows.Add(new WindowInfo
            {
                Handle = window,
                Title = title,
                IsVisible = isVisible,
                Rectangle = rect,
                IsMinimized = IsWindowMinimized(display, window),
                IsActive = isActive
            });
        }

        XCloseDisplay(display);  // Close the display connection
        return windows;
    }
    [DllImport(LibX11)]
    private static extern IntPtr XOpenDisplay(string? display);

    [DllImport(LibX11)]
    private static extern IntPtr XRootWindow(IntPtr display, int screen_number);
    [DllImport(LibX11)]
    private static extern IntPtr XDefaultRootWindow(IntPtr display);
    [DllImport(LibX11)]
    private static extern IntPtr XScreenOfDisplay(IntPtr display, int screeenNumber);

    [DllImport(LibX11)]
    private static extern int XWidthOfScreen(IntPtr screen);

    [DllImport(LibX11)]
    private static extern int XHeightOfScreen(IntPtr screen);
    [DllImport(LibX11)]
    private static extern int XScreenCount(IntPtr display);

    [DllImport(LibX11)]
    private static extern IntPtr XRootWindowOfScreen(IntPtr screen);

    [DllImport(LibX11)]
    private static extern IntPtr XDefaultScreenOfDisplay(IntPtr display);
    [DllImport(LibX11)]
    private static extern IntPtr XGetImage(IntPtr display, IntPtr drawable, int x, int y, uint width, uint height, long planeMask, int format);

    [DllImport(LibX11)]
    private static extern int XGetGeometry(IntPtr display, IntPtr window, out IntPtr root, out int x, out int y, out uint width, out uint height, out uint border_width, out uint depth);

    [DllImport(LibX11)]
    private static extern IntPtr XGetInputFocus(IntPtr display, out IntPtr focus_window, out int revert_to);
    [DllImport(LibX11)]
    private static extern IntPtr XGetWindowProperty(IntPtr display, IntPtr window, IntPtr property, long offset, long length, bool delete, IntPtr type, out IntPtr prop_return, out uint nitems, out uint bytes_after, out int format);

    [DllImport(LibX11)]
    private static extern IntPtr XGetWMName(IntPtr display, IntPtr window, out IntPtr name);
    [DllImport(LibX11)]
    private static extern IntPtr XGetSubImage(IntPtr display, IntPtr drawable, int x, int y, uint width, uint height, long planeMask, int format, IntPtr image, int destX, int dextY);


    [DllImport(LibX11)]
    private static extern int XGetWMState(IntPtr display, IntPtr window, out IntPtr state);

    [DllImport(LibX11)]
    private static extern void XStoreBytes(IntPtr display, IntPtr property, byte[] data, int length);

    [DllImport(LibX11)]
    private static extern int XFlush(IntPtr display);
    private static bool IsWindowMinimized(IntPtr display, IntPtr hwnd)
    {
        IntPtr state;
        XGetWMState(display, hwnd, out state);
        // Minimal state is often represented as iconified
        return state != IntPtr.Zero;
    }
    internal const long ALL_PLANES = -1;
    internal const int ZPIXMAP = 2;
    internal static Image TakeScreenshotWithX11(Screen screen)
    {
        IntPtr display = XOpenDisplay(null);
        if (display == IntPtr.Zero)
        {
            throw new Exception("Unable to open X display.");
        }

        IntPtr screenPtr = XScreenOfDisplay(display, 0);
        if (screenPtr == IntPtr.Zero)
        {
            throw new Exception("Unable to open XScreen 0");
        }
        DebugHelper.Logger?.Debug(screenPtr.ToString());
        IntPtr rootWindow = XRootWindowOfScreen(screenPtr);
        if (rootWindow == IntPtr.Zero)
        {
            throw new Exception("Unable to open root xwindow");
        }
        DebugHelper.Logger?.Debug(rootWindow.ToString());

        var attributes = new XWindowAttributes();
        XGetWindowAttributes(display, rootWindow, out attributes);
        DebugHelper.Logger?.Debug($"x: {attributes.x}");
        DebugHelper.Logger?.Debug($"y: {attributes.y}");
        DebugHelper.Logger?.Debug($"width: {attributes.width}");
        DebugHelper.Logger?.Debug($"height: {attributes.height}");
        DebugHelper.Logger?.Debug($"border_width: {attributes.border_width}");
        DebugHelper.Logger?.Debug($"depth: {attributes.depth}");
        DebugHelper.Logger?.Debug($"visual: {attributes.visual}");
        DebugHelper.Logger?.Debug($"root: {attributes.root}");
        DebugHelper.Logger?.Debug($"colormap: {attributes.colormap}");
        var screenBounds = screen.Bounds;
        IntPtr imagePtr = XGetImage(display, rootWindow, screenBounds.X, screenBounds.Y, (uint)screenBounds.Width, (uint)screenBounds.Height, ALL_PLANES, ZPIXMAP);
        if (imagePtr == IntPtr.Zero)
        {
            throw new Exception("Unable to capture screen image.");
        }
        // TODO: Implement Pure X11 screenshots
        // var xImage = Marshal.PtrToStructure<XImage>(imagePtr);

        // var image = Image.LoadPixelData<Rgba32>(xImage.data , screen.Width, screen.Height);

        XCloseDisplay(display);
        return Image.Load("error");
    }
    private static string GetWindowTitle(IntPtr display, IntPtr window)
    {
        IntPtr windowTitlePtr = XFetchName(display, window);
        if (windowTitlePtr != IntPtr.Zero)
        {
            return Marshal.PtrToStringAnsi(windowTitlePtr);
        }
        return "Untitled";
    }
    [DllImport(LibX11)]
    private static extern IntPtr XGetSelectionOwner(IntPtr display, IntPtr selection);

    [DllImport(LibX11)]
    private static extern void XSetSelectionOwner(IntPtr display, IntPtr selection, IntPtr owner, uint time);
    [DllImport(LibX11, CharSet = CharSet.Auto)]
    private static extern IntPtr XInternAtom(IntPtr display, string type, bool only_if_exists);

    [DllImport(LibX11)]
    private static extern int XQueryTree(IntPtr display, IntPtr window, out IntPtr root, out IntPtr parent, out IntPtr windows, out uint nchildren);

    [DllImport(LibX11)]
    private static extern IntPtr XFetchName(IntPtr display, IntPtr window);

    [DllImport(LibX11)]
    private static extern void XCloseDisplay(IntPtr display);

    // X11 Constants
    private static readonly IntPtr XA_PRIMARY = 1;
    private static readonly IntPtr XA_CLIPBOARD = 2;

    public override void CopyText(string text)
    {
        if (IsWayland())
        {
            // call dbus to copy text to clipboard

        }
        IntPtr display = XOpenDisplay(null);
        if (display == IntPtr.Zero)
        {
            DebugHelper.Logger?.Debug("Unable to open X11 display.");
            return;
        }

        IntPtr rootWindow = XRootWindow(display, 0);  // Get the root window for the default screen
        IntPtr selection = XA_CLIPBOARD;

        byte[] textBytes = Encoding.UTF8.GetBytes(text);

        // Set the clipboard content by sending the data to the X server
        XSetSelectionOwner(display, selection, rootWindow, 0);
        XStoreBytes(display, selection, textBytes, textBytes.Length);
        XFlush(display);  // Ensure the data is written to the clipboard

        DebugHelper.Logger?.Debug("Text copied to clipboard.");
    }

    public override void CopyImage(Image image, string filename = null)
    {
        using var ms = new MemoryStream();
        if (image.Metadata.DecodedImageFormat != null && !(image.Metadata.DecodedImageFormat is BmpFormat))
        {
            image.Save(ms, image.Metadata.DecodedImageFormat);
        }
        else
        {
            image.Save(ms, new PngEncoder());
        }

        var imageBytes = ms.ToArray();
        if (IsWayland())
        {
            DebugHelper.Logger?.Debug("LinuxAPI.CopyImage - Wayland only code");
            // var wlDisplay = WlDisplay.Connect();
            // var wlRegistry = wlDisplay.GetRegistry();
            // DebugHelper.Logger?.Debug($"WlDisplay connected to WL {wlRegistry.Version}");
            //
            // wlDisplay.Roundtrip();

            return;
        }

        var display = XOpenDisplay(null);
        if (display == IntPtr.Zero)
        {
            DebugHelper.Logger?.Debug("Unable to open X11 display.");
            return;
        }

        var rootWindow = XRootWindow(display, 0);
        var selection = XA_CLIPBOARD;

        var xaString = XInternAtom(display, "STRING", false);

        XSetSelectionOwner(display, selection, rootWindow, 0);
        XStoreBytes(display, selection, imageBytes, imageBytes.Length);

        if (!string.IsNullOrEmpty(filename))
        {
            var filenameBytes = Encoding.UTF8.GetBytes(filename);
            XStoreBytes(display, xaString, filenameBytes, filenameBytes.Length);
        }

        XFlush(display);
    }
    private static Rectangle GetWindowRectangleX11(IntPtr windowHandle)
    {
        IntPtr display = XOpenDisplay(null);
        if (display == IntPtr.Zero)
            throw new InvalidOperationException("Unable to open X11 display.");

        var attributes = new XWindowAttributes();
        if (XGetWindowAttributes(display, windowHandle, out attributes) != 0)
        {
            return new Rectangle(attributes.x, attributes.y, attributes.width, attributes.height);
        }

        throw new InvalidOperationException("Unable to get window attributes.");
    }
    [DllImport(LibX11)]
    private static extern int XQueryPointer(
        IntPtr display,
        IntPtr window,
        out IntPtr root,
        out IntPtr child,
        out int rootX,
        out int rootY,
        out int winX,
        out int winY,
        out int mask
    );

    public override Point GetCursorPosition()
    {
        DebugHelper.Logger?.Debug("Get cursor position.");
        IntPtr display = XOpenDisplay(null);
        if (display == IntPtr.Zero)
        {
            DebugHelper.WriteException(new InvalidOperationException("Unable to open X11 display."));
        }

        IntPtr rootWindow = XDefaultRootWindow(display);

        int rootX, rootY, winX, winY, mask;
        IntPtr root, child;
        XQueryPointer(display, rootWindow, out root, out child, out rootX, out rootY, out winX, out winY, out mask);

        XCloseDisplay(display);
        DebugHelper.Logger?.Debug($"Cursor position: {rootX}, {rootY}, {winX}, {winY}, {mask}");
        return new Point(rootX, rootY);
    }
    [DllImport(LibX11)]
    private static extern int XGetWindowAttributes(IntPtr display, IntPtr window, out XWindowAttributes attributes);

    [StructLayout(LayoutKind.Sequential)]
    public struct XWindowAttributes
    {
        public int x, y;
        public int width, height;
        public int border_width, depth;
        public IntPtr visual;
        public IntPtr root;
        public uint class_type;
        public int colormap;
        public IntPtr visualid;
        public bool is_colormap_installed;
        public bool is_border_pixmap_installed;
        public bool is_bounding_shape_installed;
        public bool is_shape_installed;
    }
    [StructLayout(LayoutKind.Sequential)]
    internal unsafe struct XImage
    {
        // ReSharper disable MemberCanBePrivate.Global
        public int width;
        public int height;
        public int xoffset;
        public int format;
        public byte* data;
        public int byte_order;
        public int bitmap_unit;
        public int bitmap_bit_order;
        public int bitmap_pad;
        public int depth;
        public int bytes_per_line;
        public int bits_per_pixel;
        public uint red_mask;
        public uint green_mask;
        public uint blue_mask;
        public nint obdata;
        // ReSharper restore MemberCanBePrivate.Global
    }

}
