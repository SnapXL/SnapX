using System.Runtime.InteropServices;
using System.Text;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;
using SnapX.Core.Media;
using WaylandSharp;

namespace SnapX.Core.Utils.Native;

public class LinuxAPI : NativeAPI
{
    private static bool IsWayland()
    {
        var display = Environment.GetEnvironmentVariable("WAYLAND_DISPLAY");
        using var wlDisplay = WlDisplay.Connect();
        using var wlRegistry = wlDisplay.GetRegistry();

        wlRegistry.Global += (_, e) =>
        {
            DebugHelper.WriteLine($"{e.Name}:{e.Interface}:{e.Version}");
        };

        wlDisplay.Roundtrip();
        return !string.IsNullOrEmpty(display);
    }

    private static bool IsPlasma()
    {
        var sessionVersion = Environment.GetEnvironmentVariable("KDE_SESSION_VERSION");
        return !string.IsNullOrEmpty(sessionVersion);
    }

    private static bool IsGNOME()
    {
        var sessionVersion = Environment.GetEnvironmentVariable("SESSIONTYPE");
        return !string.IsNullOrEmpty(sessionVersion) && sessionVersion.Contains("gnome", StringComparison.OrdinalIgnoreCase);
    }

    public static Rectangle GetWindowRectangle(IntPtr windowHandle)
    {
        return GetWindowRectangleX11(windowHandle);
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
            DebugHelper.WriteLine("Unable to open X display.");
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
            DebugHelper.WriteLine("XQueryTree failed.");
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
                X = rect.X,
                Y = rect.Y,
                Width = rect.Width,
                Height = rect.Height,
                Rectangle = rect,
                IsMinimized = IsWindowMinimized(display, window),
                IsActive = isActive
            });
        }

        XCloseDisplay(display);  // Close the display connection
        return windows;
    }
    [DllImport("libX11.so")]
    private static extern IntPtr XOpenDisplay(string? display);

    [DllImport("libX11.so")]
    private static extern IntPtr XRootWindow(IntPtr display, int screen_number);
    [DllImport("libX11.so.6")]
    private static extern IntPtr XDefaultRootWindow(IntPtr display);

    [DllImport("libX11.so")]
    private static extern IntPtr XDefaultScreenOfDisplay(IntPtr display);
    [DllImport("libX11.so.6")]
    private static extern int XGetGeometry(IntPtr display, IntPtr window, out IntPtr root, out int x, out int y, out uint width, out uint height, out uint border_width, out uint depth);

    [DllImport("libX11.so.6")]
    private static extern IntPtr XGetInputFocus(IntPtr display, out IntPtr focus_window, out int revert_to);
    [DllImport("libX11.so.6")]
    private static extern IntPtr XGetWindowProperty(IntPtr display, IntPtr window, IntPtr property, long offset, long length, bool delete, IntPtr type, out IntPtr prop_return, out uint nitems, out uint bytes_after, out int format);

    [DllImport("libX11.so.6")]
    private static extern IntPtr XGetWMName(IntPtr display, IntPtr window, out IntPtr name);

    [DllImport("libX11.so.6")]
    private static extern int XGetWMState(IntPtr display, IntPtr window, out IntPtr state);

    [DllImport("libX11.so")]
    private static extern void XStoreBytes(IntPtr display, IntPtr property, byte[] data, int length);

    [DllImport("libX11.so")]
    private static extern int XFlush(IntPtr display);
    private static bool IsWindowMinimized(IntPtr display, IntPtr hwnd)
    {
        IntPtr state;
        XGetWMState(display, hwnd, out state);
        // Minimal state is often represented as iconified
        return state != IntPtr.Zero;
    }
    private string GetWindowTitle(IntPtr display, IntPtr window)
    {
        IntPtr windowTitlePtr = XFetchName(display, window);
        if (windowTitlePtr != IntPtr.Zero)
        {
            return Marshal.PtrToStringAnsi(windowTitlePtr);
        }
        return "Untitled";
    }
    [DllImport("libX11.so")]
    private static extern IntPtr XGetSelectionOwner(IntPtr display, IntPtr selection);

    [DllImport("libX11.so")]
    private static extern void XSetSelectionOwner(IntPtr display, IntPtr selection, IntPtr owner, uint time);
    [DllImport("libX11.so", CharSet = CharSet.Auto)]
    private static extern IntPtr XInternAtom(IntPtr display, string type, bool only_if_exists);

    [DllImport("libX11.so.6")]
    private static extern int XQueryTree(IntPtr display, IntPtr window, out IntPtr root, out IntPtr parent, out IntPtr windows, out uint nchildren);

    [DllImport("libX11.so.6")]
    private static extern IntPtr XFetchName(IntPtr display, IntPtr window);

    [DllImport("libX11.so.6")]
    private static extern void XCloseDisplay(IntPtr display);
    [DllImport("libX11.so.6")]
    private static extern IntPtr XCreateSimpleWindow(IntPtr display, IntPtr parent, int x, int y, uint width, uint height, uint border_width, ulong border, ulong background);
    [DllImport("libX11.so.6")]
    private static extern int XSendEvent(IntPtr display, IntPtr window, bool propagate, int event_mask, ref XEvent xevent);
    [DllImport("libX11.so.6")]
    private static extern int XNextEvent(IntPtr display, out XEvent xevent);
    [DllImport("libX11.so.6")]
    private static extern int XChangeProperty(IntPtr display, IntPtr window, IntPtr property, IntPtr type, int format, int mode, byte[] data, int nelements);



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
    private static readonly IntPtr XA_PRIMARY = 1;
    private static readonly IntPtr XA_CLIPBOARD = 2;


    public override void CopyText(string text)
    {
        if (IsWayland())
        {
            DebugHelper.WriteLine("X11 code running on XWayland may crash SnapX.");
            return;
        }
        IntPtr display = XOpenDisplay(null);
        if (display == IntPtr.Zero)
        {
            DebugHelper.WriteLine("Could not open X11 display.");
            return;
        }
        IntPtr root = XDefaultRootWindow(display);
        IntPtr window = XCreateSimpleWindow(display, root, 0, 0, 1, 1, 0, 0, 0);
        IntPtr clipboard = XInternAtom(display, "CLIPBOARD", false);
        XSetSelectionOwner(display, clipboard, window, CurrentTime);
        if (XGetSelectionOwner(display, clipboard) != window)
        {
            DebugHelper.WriteLine($"Failed to set X11 selection owner... :(");
            return;
        }
        byte[] textBytes = Encoding.UTF8.GetBytes(text);
        IntPtr utf8 = XInternAtom(display, "UTF8_STRING", false);
        while (true)
        {
            XEvent ev;
            XNextEvent(display, out ev);
            if (ev.type == SelectionRequest)
            {
                XSelectionRequestEvent req = ev.xselectionrequest;
                XEvent response = new XEvent();
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
    }

    public override void CopyImage(Image image, string filename = null)
    {
        using var ms = new MemoryStream();
        if (image.Metadata.DecodedImageFormat != null)
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
            DebugHelper.WriteLine("LinuxAPI.CopyImage - Wayland only code");

            return;
        }

        var display = XOpenDisplay(null);
        if (display == IntPtr.Zero)
        {
            DebugHelper.WriteLine("Unable to open X11 display.");
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
    [DllImport("libX11.so")]
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
        DebugHelper.WriteLine("Get cursor position.");
        IntPtr display = XOpenDisplay(null);
        if (display == IntPtr.Zero)
        {
            DebugHelper.WriteException(new InvalidOperationException("Unable to open X11 display."));
        }

        // Get the root window (typically the main screen)
        IntPtr rootWindow = XRootWindow(display, 0);

        // Query the cursor position
        int rootX, rootY, winX, winY, mask;
        IntPtr root, child;
        XQueryPointer(display, rootWindow, out root, out child, out rootX, out rootY, out winX, out winY, out mask);

        // Close the X11 display
        XCloseDisplay(display);
        DebugHelper.WriteLine($"Cursor position: {rootX}, {rootY}, {winX}, {winY}, {mask}");
        return new Point(rootX, rootY);
    }
    [DllImport("libX11.so")]
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

}
