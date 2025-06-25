using System.Runtime.InteropServices;
using System.Text;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Bmp;
using SixLabors.ImageSharp.Formats.Png;
using SnapX.Core.Media;
using WaylandSharp;

namespace SnapX.Core.Utils.Native;

public partial class LinuxAPI : NativeAPI
{
    private const string LibX11 = "libX11.so.6";
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

    public override Rectangle GetWindowRectangle(IntPtr windowHandle)
    {
        return GetWindowRectangleX11(windowHandle);
    }
    public override Screen? GetScreen(Point pos)
    {
        var display = XOpenDisplay(null);
        if (display == IntPtr.Zero)
        {
            DebugHelper.WriteLine("Unable to open X11 display.");
            return null;
        }

        var screenCount = XScreenCount(display);
        for (var i = 0; i < screenCount; i++)
        {
            var rootWindow = XRootWindow(display, i);
            XGetGeometry(display, rootWindow, out _, out var x, out var y, out var width, out var height, out _, out _);

            if (pos.X < x || pos.X > x + (int)width || pos.Y < y || pos.Y > y + (int)height) continue;
            DebugHelper.Logger?.Debug("Point {Pos} is within screen {I} bounds", pos, i);
            return new Screen()
            {
                Bounds = new Rectangle(x, y, (int)width, (int)height),
                Name = "NotImplementedName",
                Id = "NotImplementedID"
            };
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
                // Interact with Plasmashell over DBus
                return windows;
            }

            if (IsGNOME())
            {
                // Interact with GNOME Shell
                return windows;
            }

            return windows;
        }

        var display = XOpenDisplay(null);
        if (display == IntPtr.Zero)
        {
            DebugHelper.Logger?.Debug("Unable to open X display");
            return windows;
        }

        var root = XDefaultRootWindow(display);  // Get the root window of the X display

        // Get all the child windows of the root window
        var status = XQueryTree(display, root, out root, out _, out var windowsPtr, out var nchildren);
        if (status == 0)
        {
            DebugHelper.Logger?.Debug("XQueryTree failed");
            XCloseDisplay(display);
            return windows;
        }

        for (uint i = 0; i < nchildren; i++)
        {
            var window = Marshal.ReadIntPtr(windowsPtr, (int)(i * IntPtr.Size));
            var title = GetWindowTitle(display, window);
            XGetGeometry(display, window, out root, out _, out _, out _, out _, out _, out _);

            XGetWindowAttributes(display, window, out var attributes);
            var isVisible = attributes.is_colormap_installed;

            // Active window
            XGetInputFocus(display, out var focusWindow, out _);
            var isActive = focusWindow == window;
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

        XCloseDisplay(display);
        return windows;
    }
    [LibraryImport(LibX11, StringMarshalling = StringMarshalling.Utf16)]
    private static partial IntPtr XOpenDisplay(string? display);

    [LibraryImport(LibX11)]
    private static partial IntPtr XRootWindow(IntPtr display, int screen_number);
    [LibraryImport(LibX11)]
    private static partial IntPtr XDefaultRootWindow(IntPtr display);
    [LibraryImport(LibX11)]
    private static partial IntPtr XScreenOfDisplay(IntPtr display, int screeenNumber);

    [LibraryImport(LibX11)]
    private static partial int XWidthOfScreen(IntPtr screen);

    [LibraryImport(LibX11)]
    private static partial int XHeightOfScreen(IntPtr screen);
    [LibraryImport(LibX11)]
    private static partial int XScreenCount(IntPtr display);

    [LibraryImport(LibX11)]
    private static partial IntPtr XRootWindowOfScreen(IntPtr screen);

    [LibraryImport(LibX11)]
    private static partial IntPtr XDefaultScreenOfDisplay(IntPtr display);
    [LibraryImport(LibX11)]
    private static partial IntPtr XGetImage(IntPtr display, IntPtr drawable, int x, int y, uint width, uint height, long planeMask, int format);

    [LibraryImport(LibX11)]
    private static partial int XGetGeometry(IntPtr display, IntPtr window, out IntPtr root, out int x, out int y, out uint width, out uint height, out uint border_width, out uint depth);

    [LibraryImport(LibX11)]
    private static partial IntPtr XGetInputFocus(IntPtr display, out IntPtr focus_window, out int revert_to);
    [LibraryImport(LibX11)]
    private static partial IntPtr XGetWindowProperty(IntPtr display, IntPtr window, IntPtr property, long offset, long length, [MarshalAs(UnmanagedType.Bool)] bool delete, IntPtr type, out IntPtr prop_return, out uint nitems, out uint bytes_after, out int format);

    [LibraryImport(LibX11)]
    private static partial IntPtr XGetWMName(IntPtr display, IntPtr window, out IntPtr name);
    [LibraryImport(LibX11)]
    private static partial IntPtr XGetSubImage(IntPtr display, IntPtr drawable, int x, int y, uint width, uint height, long planeMask, int format, IntPtr image, int destX, int dextY);
    [LibraryImport(LibX11)]
    private static partial int XGetWMState(IntPtr display, IntPtr window, out IntPtr state);

    [LibraryImport(LibX11)]
    private static partial void XStoreBytes(IntPtr display, IntPtr property, byte[] data, int length);

    [LibraryImport(LibX11)]
    private static partial int XFlush(IntPtr display);
    private static bool IsWindowMinimized(IntPtr display, IntPtr hwnd)
    {
        XGetWMState(display, hwnd, out var state);
        // Minimal state is often represented as iconified
        return state != IntPtr.Zero;
    }

    private const long ALL_PLANES = -1;
    public const int ZPIXMAP = 2;
    internal static Image TakeScreenshotWithX11(Screen screen)
    {
        var display = XOpenDisplay(null);
        if (display == IntPtr.Zero)
        {
            throw new Exception("Unable to open X display.");
        }

        var screenPtr = XScreenOfDisplay(display, 0);
        if (screenPtr == IntPtr.Zero)
        {
            throw new Exception("Unable to open XScreen 0");
        }
        var rootWindow = XRootWindowOfScreen(screenPtr);
        if (rootWindow == IntPtr.Zero)
        {
            throw new Exception("Unable to open root xwindow");
        }

        XGetWindowAttributes(display, rootWindow, out var attributes);
        DebugHelper.Logger?.Debug("x: {AttributesX}", attributes.x);
        DebugHelper.Logger?.Debug("y: {AttributesY}", attributes.y);
        DebugHelper.Logger?.Debug("width: {AttributesWidth}", attributes.width);
        DebugHelper.Logger?.Debug("height: {AttributesHeight}", attributes.height);
        DebugHelper.Logger?.Debug("border_width: {AttributesBorderWidth}", attributes.border_width);
        DebugHelper.Logger?.Debug("depth: {AttributesDepth}", attributes.depth);
        DebugHelper.Logger?.Debug("visual: {AttributesVisual}", attributes.visual);
        DebugHelper.Logger?.Debug("root: {AttributesRoot}", attributes.root);
        DebugHelper.Logger?.Debug("colormap: {AttributesColormap}", attributes.colormap);
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
        var windowTitlePtr = XFetchName(display, window);
        return windowTitlePtr != IntPtr.Zero ? Marshal.PtrToStringAnsi(windowTitlePtr) ?? "Untitled" : "Untitled";
    }
    [LibraryImport(LibX11)]
    private static partial IntPtr XGetSelectionOwner(IntPtr display, IntPtr selection);

    [LibraryImport(LibX11)]
    private static partial void XSetSelectionOwner(IntPtr display, IntPtr selection, IntPtr owner, uint time);
    [LibraryImport(LibX11, StringMarshalling = StringMarshalling.Utf16)]
    private static partial IntPtr XInternAtom(IntPtr display, string type, [MarshalAs(UnmanagedType.Bool)] bool only_if_exists);

    [LibraryImport(LibX11)]
    private static partial int XQueryTree(IntPtr display, IntPtr window, out IntPtr root, out IntPtr parent, out IntPtr windows, out uint nchildren);

    [LibraryImport(LibX11)]
    private static partial IntPtr XFetchName(IntPtr display, IntPtr window);

    [LibraryImport(LibX11)]
    private static partial void XCloseDisplay(IntPtr display);
    [LibraryImport(LibX11)]
    private static partial IntPtr XCreateSimpleWindow(IntPtr display, IntPtr parent, int x, int y, uint width, uint height, uint border_width, ulong border, ulong background);
    [LibraryImport(LibX11)]
    private static partial int XSendEvent(IntPtr display, IntPtr window, [MarshalAs(UnmanagedType.Bool)] bool propagate, int event_mask, ref XEvent xevent);
    [LibraryImport(LibX11)]
    private static partial int XNextEvent(IntPtr display, out XEvent xevent);
    [LibraryImport(LibX11)]
    private static partial int XChangeProperty(IntPtr display, IntPtr window, IntPtr property, IntPtr type, int format, int mode, byte[] data, int nelements);



    [StructLayout(LayoutKind.Sequential)]
    public struct XEvent
    {
        public int type;
        public XSelectionRequestEvent xselectionrequest;
    }
    private const int SelectionRequest = 30;
    private const int SelectionNotify = 31;
    private const int PropModeReplace = 0;
    private const int CurrentTime = 0;
    private static readonly IntPtr XA_STRING = new IntPtr(31);


    [StructLayout(LayoutKind.Sequential)]
    public struct XSelectionRequestEvent
    {
        public int type;
        public IntPtr display;
        public IntPtr requestor;
        public IntPtr selection;
        public IntPtr target;
        public IntPtr property;
        public int time;
    }
    // X11 Constants
    // private static readonly IntPtr XA_PRIMARY = 1;
    private const IntPtr XA_CLIPBOARD = 2;


    public override void CopyText(string text)
    {
        if (IsWayland())
        {
            // call dbus to copy text to clipboard
            DebugHelper.WriteLine("This code will crash SnapX on Wayland.");
            using var wlDisplay = WlDisplay.Connect();
            using var wlRegistry = wlDisplay.GetRegistry();

            wlRegistry.Global += (_, e) =>
            {
                if (e.Interface.Contains(WlInterface.WlOutput.Name))
                {
                    using var wlOutput = wlRegistry.Bind<WlOutput>(e.Name, e.Interface);
                    DebugHelper.WriteLine("inside the wl_output interface");
                    wlOutput.Name += (_, name) =>
                    {
                        // This code will execute whenever the 'Name' event is raised
                        DebugHelper.WriteLine($"WlOutput name received: {name}");
                        // You might store this name in a property of your own object
                        // For example: this.OutputActualName = name;
                    };
                }
                DebugHelper.WriteLine($"{e.Name}:{e.Interface}:{e.Version}");
            };


            wlDisplay.Roundtrip();
            return;
        }
        var display = XOpenDisplay(null);
        if (display == IntPtr.Zero)
        {
            DebugHelper.Logger?.Debug("Unable to open X11 display");
            return;
        }
        var root = XDefaultRootWindow(display);
        var window = XCreateSimpleWindow(display, root, 0, 0, 1, 1, 0, 0, 0);
        var clipboard = XInternAtom(display, "CLIPBOARD", false);
        XSetSelectionOwner(display, clipboard, window, CurrentTime);
        if (XGetSelectionOwner(display, clipboard) != window)
        {
            DebugHelper.WriteLine($"Failed to set X11 selection owner... :(");
            return;
        }
        var textBytes = Encoding.UTF8.GetBytes(text);
        var utf8 = XInternAtom(display, "UTF8_STRING", false);
        while (true)
        {
            XNextEvent(display, out var ev);
            if (ev.type != SelectionRequest) continue;
            var req = ev.xselectionrequest;
            var response = new XEvent();
            response.type = SelectionNotify;
            response.xselectionrequest.display = req.display;
            response.xselectionrequest.requestor = req.requestor;
            response.xselectionrequest.selection = req.selection;
            response.xselectionrequest.target = req.target;
            response.xselectionrequest.property = req.property;
            if (req.target == utf8 || req.target == XA_STRING)
            {
                XChangeProperty(display, req.requestor, req.property, req.target, 8, PropModeReplace, textBytes, textBytes.Length);
            }
            else
            {
                response.xselectionrequest.property = IntPtr.Zero;
            }
            XSendEvent(display, req.requestor, false, 0, ref response);
            XFlush(display);
        }
    }

    public Rectangle GetScreenBounds()
    {
        var display = XOpenDisplay(null);
        if (display == IntPtr.Zero)
            throw new InvalidOperationException("Could not open X11 display.");

        try
        {
            var screenCount = XScreenCount(display);

            var totalWidth = 0;
            var maxHeight = 0;

            for (var i = 0; i < screenCount; i++)
            {
                var screen = XScreenOfDisplay(display, i);
                var width = XWidthOfScreen(screen);
                var height = XHeightOfScreen(screen);

                totalWidth += width; // assuming screens are side-by-side
                if (height > maxHeight)
                    maxHeight = height;
            }

            return new Rectangle(0, 0, totalWidth, maxHeight);
        }
        finally
        {
            XCloseDisplay(display);
        }
    }
    public override void CopyImage(Image image, string? filename)
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
            DebugHelper.Logger?.Debug("Unable to open X11 display");
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
    [LibraryImport(LibX11)]
    private static partial int XQueryPointer(
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
        DebugHelper.Logger?.Debug("Get cursor position");
        var display = XOpenDisplay(null);
        if (display == IntPtr.Zero)
        {
            DebugHelper.WriteException(new InvalidOperationException("Unable to open X11 display."));
        }

        var rootWindow = XDefaultRootWindow(display);

        XQueryPointer(display, rootWindow, out _, out _, out var rootX, out var rootY, out var winX, out var winY, out var mask);

        XCloseDisplay(display);
        DebugHelper.Logger?.Debug("Cursor position: {RootX}, {RootY}, {WinX}, {WinY}, {Mask}", rootX, rootY, winX, winY, mask);
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
