using System.Runtime.InteropServices;
using System.Text;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;
using SnapX.Core.Media;

namespace SnapX.Core.Utils.Native;

public partial class LinuxAPI : NativeAPI
{
    private const string LibX11 = "libX11.so.6";
    const string XRandR = "libXrandr.so.2";
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
        return !string.IsNullOrEmpty(sessionVersion)
            && sessionVersion.Contains("gnome", StringComparison.OrdinalIgnoreCase);
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

        try
        {
            var rootWindow = XDefaultRootWindow(display);

            int monitorCount = 0;
            IntPtr monitorsPtr = XRRGetMonitors(display, rootWindow, true, out monitorCount);
            if (monitorsPtr == IntPtr.Zero || monitorCount == 0)
            {
                DebugHelper.WriteLine("Failed to get monitors via XRRGetMonitors or no monitors found.");
                return null;
            }

            try
            {
                DebugHelper.WriteLine($"Number of X11 monitors (XRRGetMonitors): {monitorCount}");
                var monitorStructSize = Marshal.SizeOf<XRRMonitorInfo>();

                for (var i = 0; i < monitorCount; i++)
                {
                    var monitorPtr = IntPtr.Add(monitorsPtr, i * monitorStructSize);
                    var monitorInfo = Marshal.PtrToStructure<XRRMonitorInfo>(monitorPtr);

                    var bounds = new Rectangle(monitorInfo.x, monitorInfo.y, monitorInfo.width, monitorInfo.height);
                    var monitorName = string.Empty;
                    var atomNamePtr = XGetAtomName(display, monitorInfo.name);
                    if (atomNamePtr != IntPtr.Zero)
                    {
                        monitorName = Marshal.PtrToStringAnsi(atomNamePtr) ?? "Unnamed";
                    }

                    var name = $"Monitor_{i} ({monitorName})";
                    DebugHelper.WriteLine($"{name}: {bounds}");

                    if (pos.X < bounds.X || pos.X >= bounds.X + bounds.Width ||
                        pos.Y < bounds.Y || pos.Y >= bounds.Y + bounds.Height) continue;
                    DebugHelper.Logger?.Debug("Point {Pos} is within monitor bounds", pos);
                    var width = monitorInfo.width;
                    var height = monitorInfo.height;
                    var x = monitorInfo.x;
                    var y = monitorInfo.y;
                    var mwidth = monitorInfo.mwidth;
                    var mheight = monitorInfo.mheight;

                    double dpi = 0;
                    double diagonalInches = 0;
                    if (mwidth > 0 && mheight > 0)
                    {
                        diagonalInches = Math.Sqrt(mwidth * mwidth + mheight * mheight) / 25.4;
                        var resolutionDiagonal = Math.Sqrt(width * width + height * height);
                        dpi = resolutionDiagonal / diagonalInches;
                    }

                    var orientation = width >= height ? ScreenOrientation.Landscape : ScreenOrientation.Portrait;

                    return new Screen
                    {
                        Id = $"X11_{i}",
                        Index = i,
                        Name = monitorName,
                        Bounds = new Rectangle(x, y, width, height),
                        DPI = dpi,
                        DiagonalSizeInches = diagonalInches,
                        Orientation = orientation,
                        IsPrimary = monitorInfo.primary != 0,
                        SessionType = SessionType.X11
                    };
                }
            }
            finally
            {
                XRRFreeMonitors(monitorsPtr);
            }
        }
        finally
        {
            XCloseDisplay(display);
        }

        return null;
    }

    private static readonly Lock X11Sync = new();


    public override List<WindowInfo> GetWindowList()
    {
        DebugHelper.WriteLine($"GetWindowList called");
        var windows = new List<WindowInfo>();
        lock (X11Sync)
        {
            // if (IsWayland())
            // {
            //     if (IsPlasma())
            //     {
            //         // Interact with Plasmashell over DBus
            //         return windows;
            //     }
            //
            //     if (IsGNOME())
            //     {
            //         // Interact with GNOME Shell
            //         return windows;
            //     }
            //
            //     return windows;
            // }

            var display = XOpenDisplay(null);
            if (display == IntPtr.Zero)
            {
                DebugHelper.Logger?.Debug("Unable to open X display");
                return windows;
            }

            var root = XDefaultRootWindow(display); // Get the root window of the X display

            // Get all the child windows of the root window
            var status = XQueryTree(
                display,
                root,
                out root,
                out _,
                out var windowsPtr,
                out var nchildren
            );
            if (status == 0 || windowsPtr == IntPtr.Zero)
            {
                DebugHelper.Logger?.Debug("XQueryTree failed");
                XCloseDisplay(display);
                return windows;
            }

            DebugHelper.Logger?.Debug("XQueryTree returned: " + status);
            DebugHelper.Logger?.Debug("nchildren: " + nchildren);

            for (uint i = 0; i < nchildren; i++)
            {
                var window = Marshal.ReadIntPtr(windowsPtr, (int)(i * IntPtr.Size));

                var title = GetWindowTitle(display, window);
                // if (title == string.Empty || title == "Untitled")
                // {
                //     DebugHelper.Logger?.Debug("Window is untitled, skipping");
                //     continue;
                // }

                XGetWindowAttributes(display, window, out var attributes);
                if (attributes.map_state != MapState.IsViewable)
                {
                    continue;
                }

                if (attributes.width <= 1 || attributes.height <= 1)
                {
                    continue;
                }

                // Active window
                XGetInputFocus(display, out var focusWindow, out _);
                var isActive = focusWindow == window;
                var rect = GetWindowRectangle(window);
                windows.Add(
                    new WindowInfo
                    {
                        Handle = window,
                        Title = title,
                        IsVisible = true,
                        Rectangle = rect,
                        // IsMinimized = IsWindowMinimized(display, window),
                        IsActive = isActive,
                    }
                );
            }

            XCloseDisplay(display);
        }

        return windows;
    }

    [LibraryImport(LibX11, StringMarshalling = StringMarshalling.Utf16)]
    internal static partial IntPtr XOpenDisplay(string? display);

    [LibraryImport(LibX11)]
    internal static partial IntPtr XRootWindow(IntPtr display, int screen_number);

    [LibraryImport(LibX11)]
    internal static partial IntPtr XDefaultRootWindow(IntPtr display);

    [LibraryImport(LibX11)]
    internal static partial int XEventsQueued(IntPtr display, int mode); // mode 0 for QueuedAfterReading,
                                                                         // 1 for QueuedAlready, 2 for QueuedAfterFlush

    [LibraryImport(LibX11)]
    internal static partial int XSelectInput(IntPtr display, IntPtr w, long event_mask);

    [LibraryImport(LibX11)]
    internal static partial IntPtr XScreenOfDisplay(IntPtr display, int screeenNumber);

    [LibraryImport(LibX11)]
    internal static partial int XWidthOfScreen(IntPtr screen);

    [LibraryImport(LibX11)]
    internal static partial int XHeightOfScreen(IntPtr screen);

    [LibraryImport(LibX11)]
    internal static partial int XScreenCount(IntPtr display);

    [LibraryImport(LibX11)]
    internal static partial IntPtr XRootWindowOfScreen(IntPtr screen);

    [LibraryImport(LibX11)]
    internal static partial IntPtr XDefaultScreenOfDisplay(IntPtr display);

    [LibraryImport(LibX11)]
    internal static partial IntPtr XGetImage(
        IntPtr display,
        IntPtr drawable,
        int x,
        int y,
        uint width,
        uint height,
        long planeMask,
        int format
    );

    [LibraryImport(LibX11)]
    internal static partial int XGetGeometry(
        IntPtr display,
        IntPtr window,
        out IntPtr root,
        out int x,
        out int y,
        out uint width,
        out uint height,
        out uint border_width,
        out uint depth
    );

    [LibraryImport(LibX11)]
    internal static partial IntPtr XGetInputFocus(
        IntPtr display,
        out IntPtr focus_window,
        out int revert_to
    );

    [LibraryImport(LibX11)]
    internal static partial int XGetWindowProperty(
        IntPtr display,
        IntPtr window,
        IntPtr property,
        IntPtr long_offset,
        IntPtr long_length,
        [MarshalAs(UnmanagedType.Bool)] bool delete,
        IntPtr req_type,
        out IntPtr actual_type_return,
        out int actual_format_return,
        out IntPtr nitems_return,
        out IntPtr bytes_after_return,
        out IntPtr prop_return
    );


    [LibraryImport(LibX11)]
    internal static partial IntPtr XGetWMName(IntPtr display, IntPtr window, out IntPtr name);

    [LibraryImport(LibX11)]
    internal static partial IntPtr XGetSubImage(
        IntPtr display,
        IntPtr drawable,
        int x,
        int y,
        uint width,
        uint height,
        long planeMask,
        int format,
        IntPtr image,
        int destX,
        int dextY
    );

    [LibraryImport(LibX11)]
    internal static partial int XGetWMState(IntPtr display, IntPtr window, out IntPtr state);
    [LibraryImport(LibX11)]
    internal static partial IntPtr XGetAtomName(IntPtr display, IntPtr atom); // Returns char*, needs marshalling


    [LibraryImport(LibX11)]
    internal static partial void XStoreBytes(
        IntPtr display,
        IntPtr property,
        byte[] data,
        int length
    );

    [LibraryImport(LibX11)]
    internal static partial int XFlush(IntPtr display);
    [LibraryImport(LibX11)]
    internal static partial int XDestroyWindow(IntPtr display, IntPtr w);
    [LibraryImport(LibX11)]
    internal static partial int XFree(IntPtr data);
    private static bool IsWindowMinimized(IntPtr display, IntPtr hwnd)
    {
        XGetWMState(display, hwnd, out var state);
        // Minimal state is often represented as iconified
        return state != IntPtr.Zero;
    }

    private const uint ALL_PLANES = 0xFFFFFFFF;
    public const int ZPIXMAP = 2;

    internal static Image TakeScreenshotWithX11(Screen screen)
    {
        DebugHelper.WriteLine($"Screenshotting screen {screen.Name} with {screen.Resolution} ({screen.Id})");

        var display = XOpenDisplay(null);
        if (display == IntPtr.Zero)
            throw new Exception("Unable to open X display.");

        var screenPtr = XScreenOfDisplay(display, screen.Index);
        if (screenPtr == IntPtr.Zero)
            throw new Exception($"Unable to open XScreen {screen.Index}");

        var rootWindow = XRootWindowOfScreen(screenPtr);
        if (rootWindow == IntPtr.Zero)
            throw new Exception("Unable to open root xwindow");

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
        var imagePtr = XGetImage(
            display,
            rootWindow,
            screenBounds.X,
            screenBounds.Y,
            (uint)screenBounds.Width,
            (uint)screenBounds.Height,
            ALL_PLANES,
            ZPIXMAP
        );

        if (imagePtr == IntPtr.Zero)
            throw new Exception("Unable to capture screen image using X11.");

        var xImage = Marshal.PtrToStructure<XImage>(imagePtr);

        var width = xImage.width;
        var height = xImage.height;
        var bpp = xImage.bits_per_pixel;  // Bits per pixel (important for interpreting format)
        DebugHelper.Logger?.Debug($"XImage: width={width}, height={height}, bits_per_pixel={bpp}");
        DebugHelper.Logger?.Debug($"XImage masks: red=0x{xImage.red_mask:X}, green=0x{xImage.green_mask:X}, blue=0x{xImage.blue_mask:X}");

        var bytesPerPixel = bpp / 8;

        var pixelData = new byte[width * height * 4];

        var rawPixels = new byte[width * height * bytesPerPixel];
        Marshal.Copy(xImage.data, rawPixels, 0, rawPixels.Length);

        for (var y = 0; y < height; y++)
        {
            if (y % 100 == 0)
                DebugHelper.Logger?.Debug($"Processing row {y}/{height}");

            for (var x = 0; x < width; x++)
            {
                // Calculate the offset for the current pixel (byte-by-byte)
                var pixelOffset = (y * width + x) * bytesPerPixel;

                // Extract the raw pixel value from the data buffer
                ulong pixel = 0;
                for (var byteIndex = 0; byteIndex < bytesPerPixel; byteIndex++)
                {
                    pixel |= (ulong)rawPixels[pixelOffset + byteIndex] << (8 * byteIndex); // assuming little-endian
                }
                var rawPixelBytes = new byte[bytesPerPixel];
                for (var byteIndex = 0; byteIndex < bytesPerPixel; byteIndex++)
                {
                    var currentByte = rawPixels[pixelOffset + byteIndex];
                    rawPixelBytes[byteIndex] = currentByte;
                    pixel |= (ulong)currentByte << (8 * byteIndex); // assuming little-endian
                }
                if ((x == 0 && y == 0) || (x == width / 2 && y == height / 2))
                {
                    DebugHelper.Logger?.Debug(
                        "Pixel at ({X},{Y}): Raw Bytes: {RawBytes}, Combined pixel ulong: 0x{Pixel:X}, BytesPerPixel: {BPP}",
                        x, y,
                        BitConverter.ToString(rawPixelBytes),
                        pixel,
                        bytesPerPixel
                    );

                    DebugHelper.Logger?.Debug(
                        "Masks - Red: 0x{RedMask:X}, Green: 0x{GreenMask:X}, Blue: 0x{BlueMask:X}",
                        xImage.red_mask, xImage.green_mask, xImage.blue_mask
                    );
                }

                var r = ExtractColorComponent(pixel, xImage.red_mask);
                var g = ExtractColorComponent(pixel, xImage.green_mask);
                var b = ExtractColorComponent(pixel, xImage.blue_mask);

                var idx = (y * width + x) * 4;
                pixelData[idx + 0] = r;
                pixelData[idx + 1] = g;
                pixelData[idx + 2] = b;
                pixelData[idx + 3] = 255;
            }
        }

        // Create ImageSharp image from the processed pixel data
        var image = Image.LoadPixelData<Rgba32>(pixelData, width, height);

        // Clean up
        XDestroyImage(imagePtr);
        XCloseDisplay(display);

        return image;
    }
    internal Image? TakeScreenshotOfX11Window(WindowInfo window)
    {
        DebugHelper.WriteLine($"Screenshotting window '{window.Title}' ({window.Handle:X}) with bounds {window.Rectangle}");

        var display = XOpenDisplay(null);
        if (display == IntPtr.Zero)
            throw new Exception("Unable to open X display.");

        var gotWindowAttributes = XGetWindowAttributes(display, window.Handle, out var attributes);
        if (gotWindowAttributes == 0) throw new Exception("Unable to get window attributes.");
        int width = attributes.width;
        int height = attributes.height;

        DebugHelper.Logger?.Debug("Window Attributes: width={Width}, height={Height}, depth={Depth}", width, height, attributes.depth);
        DebugHelper.Logger?.Debug("visual: {Visual}, colormap: {Colormap}", attributes.visual, attributes.colormap);

        // Capture the image from the window using XGetImage
        var imagePtr = XGetImage(
            display,
            window.Handle,
            0, 0,
            (uint)width,
            (uint)height,
            ALL_PLANES,
            ZPIXMAP
        );

        if (imagePtr == IntPtr.Zero)
            throw new Exception("Unable to capture window image using XGetImage.");

        var xImage = Marshal.PtrToStructure<XImage>(imagePtr);
        var bpp = xImage.bits_per_pixel;
        var bytesPerPixel = bpp / 8;

        DebugHelper.Logger?.Debug($"XImage: width={xImage.width}, height={xImage.height}, bits_per_pixel={bpp}");
        DebugHelper.Logger?.Debug($"XImage masks: red=0x{xImage.red_mask:X}, green=0x{xImage.green_mask:X}, blue=0x{xImage.blue_mask:X}");

        var rawPixels = new byte[width * height * bytesPerPixel];
        Marshal.Copy(xImage.data, rawPixels, 0, rawPixels.Length);

        var pixelData = new byte[width * height * 4];

        for (var y = 0; y < height; y++)
        {
            if (y % 100 == 0)
                DebugHelper.Logger?.Debug("Processing row {Y}/{Height}", y, height);

            for (var x = 0; x < width; x++)
            {
                var pixelOffset = (y * width + x) * bytesPerPixel;

                ulong pixel = 0;
                var rawPixelBytes = new byte[bytesPerPixel];

                for (var byteIndex = 0; byteIndex < bytesPerPixel; byteIndex++)
                {
                    var currentByte = rawPixels[pixelOffset + byteIndex];
                    rawPixelBytes[byteIndex] = currentByte;
                    pixel |= (ulong)currentByte << (8 * byteIndex); // assuming little-endian
                }

                if ((x == 0 && y == 0) || (x == width / 2 && y == height / 2))
                {
                    DebugHelper.Logger?.Debug(
                        "Pixel at ({X},{Y}): Raw Bytes: {RawBytes}, Combined pixel ulong: 0x{Pixel:X}, BytesPerPixel: {Bpp}",
                        x, y,
                        BitConverter.ToString(rawPixelBytes),
                        pixel,
                        bytesPerPixel
                    );
                    DebugHelper.Logger?.Debug(
                        "Masks - Red: 0x{RedMask:X}, Green: 0x{GreenMask:X}, Blue: 0x{BlueMask:X}",
                        xImage.red_mask, xImage.green_mask, xImage.blue_mask
                    );
                }

                var r = ExtractColorComponent(pixel, xImage.red_mask);
                var g = ExtractColorComponent(pixel, xImage.green_mask);
                var b = ExtractColorComponent(pixel, xImage.blue_mask);

                var idx = (y * width + x) * 4;
                pixelData[idx + 0] = r;
                pixelData[idx + 1] = g;
                pixelData[idx + 2] = b;
                pixelData[idx + 3] = 255;
            }
        }

        var image = Image.LoadPixelData<Rgba32>(pixelData, width, height);

        XDestroyImage(imagePtr);
        XCloseDisplay(display);

        return image;
    }

    static byte ExtractColorComponent(ulong pixel, ulong mask)
    {
        if (mask == 0)
            return 0;

        var shift = GetShift(mask);
        var component = (pixel & mask) >> shift;

        // Normalize component to 8 bits if mask uses less than 8 bits
        var maskBits = CountBits(mask);
        if (maskBits == 0)
            return 0;

        if (maskBits == 8)
            return (byte)component;
        DebugHelper.Logger?.Debug(
            "Extracting color component from pixel: {0:X}, mask: {1:X}, shift: {2}, component: {3}, scaled: {4}",
            pixel, mask, shift, component, (byte)((component * 255) / (ulong)((1 << maskBits) - 1))
        );
        // Scale component up to 8 bits
        return (byte)((component * 255) / (ulong)((1 << maskBits) - 1));
    }
    static int CountBits(ulong mask)
    {
        int count = 0;
        while (mask != 0)
        {
            count += (int)(mask & 1);
            mask >>= 1;
        }
        return count;
    }
    private static readonly IntPtr XA_WM_NAME = new IntPtr(39); // Usually 39, but can be obtained via XInternAtom
    [StructLayout(LayoutKind.Sequential)]
    public struct XTextProperty
    {
        public IntPtr value;
        public IntPtr encoding;
        public int format;
        public IntPtr nitems;
    }

    [LibraryImport(LibX11)]
    internal static partial int XGetTextProperty(IntPtr display, IntPtr window, out XTextProperty textProp, IntPtr property);

    string GetWindowTitle(IntPtr display, IntPtr window)
    {
        var netWmName = XInternAtom(display, "_NET_WM_NAME", false);
        var utf8String = XInternAtom(display, "UTF8_STRING", false);

        IntPtr actualType, prop;
        int actualFormat;
        IntPtr nItems, bytesAfter;

        int status = XGetWindowProperty(
            display,
            window,
            netWmName,
            IntPtr.Zero,
            new IntPtr(1024),
            false,
            utf8String,
            out actualType,
            out actualFormat,
            out nItems,
            out bytesAfter,
            out prop
        );

        if (status == 0 && prop != IntPtr.Zero)
        {
            try
            {
                return Marshal.PtrToStringUTF8(prop) ?? "Untitled";
            }
            finally
            {
                XFree(prop);
            }
        }

        // fallback: WM_NAME
        XTextProperty textProp;
        if (XGetTextProperty(display, window, out textProp, XA_WM_NAME) != 0 && textProp.value != IntPtr.Zero)
        {
            try
            {
                return Marshal.PtrToStringAnsi(textProp.value) ?? "Untitled";
            }
            finally
            {
                XFree(textProp.value);
            }
        }

        return "Untitled";
    }




    [LibraryImport(LibX11)]
    internal static partial IntPtr XGetSelectionOwner(IntPtr display, IntPtr selection);

    [LibraryImport(LibX11)]
    internal static partial void XSetSelectionOwner(
        IntPtr display,
        IntPtr selection,
        IntPtr owner,
        uint time
    );

    [LibraryImport(LibX11, StringMarshalling = StringMarshalling.Utf16)]
    internal static partial IntPtr XInternAtom(
        IntPtr display,
        string type,
        [MarshalAs(UnmanagedType.Bool)] bool only_if_exists
    );

    [LibraryImport(LibX11)]
    internal static partial int XQueryTree(
        IntPtr display,
        IntPtr window,
        out IntPtr root,
        out IntPtr parent,
        out IntPtr windows,
        out uint nchildren
    );

    [LibraryImport(LibX11, EntryPoint = "XFetchName", StringMarshalling = StringMarshalling.Utf8)]
    internal static partial int XFetchName(IntPtr display, IntPtr window, out IntPtr windowName);

    [LibraryImport(LibX11)]
    internal static partial int XDestroyImage(IntPtr ximage);

    [LibraryImport(LibX11)]
    internal static partial void XCloseDisplay(IntPtr display);
    [StructLayout(LayoutKind.Sequential)]
    internal unsafe struct XRRMonitorInfo
    {
        public IntPtr name;
        public int primary;
        public int automatic;
        public int nOutput;
        public int x;
        public int y;
        public int width;
        public int height;
        public int mwidth;
        public int mheight;
        public IntPtr* Outputs;
    }
    [DllImport(XRandR)]
    public static extern IntPtr XRRGetMonitors(IntPtr dpy, IntPtr window, bool get_active, out int nmonitors);

    [DllImport(XRandR)]
    public static extern void XRRFreeMonitors(IntPtr monitors);


    [LibraryImport(LibX11)]
    internal static partial IntPtr XCreateSimpleWindow(
        IntPtr display,
        IntPtr parent,
        int x,
        int y,
        uint width,
        uint height,
        uint border_width,
        ulong border,
        ulong background
    );

    [DllImport(LibX11)]
    internal static extern int XSendEvent(
        IntPtr display,
        IntPtr window,
        [MarshalAs(UnmanagedType.Bool)] bool propagate,
        int event_mask,
        ref XEvent xevent
    );
    [LibraryImport(LibX11)]
    internal static partial int XSendEvent(IntPtr display, IntPtr window, [MarshalAs(UnmanagedType.Bool)] bool propagate, long event_mask, IntPtr xevent_ptr);
    [DllImport(LibX11)]
    internal static extern int XNextEvent(IntPtr display, out XEvent xevent);

    [LibraryImport(LibX11)]
    internal static partial int XChangeProperty(
        IntPtr display,
        IntPtr window,
        IntPtr property,
        IntPtr type,
        int format,
        int mode,
        byte[] data,
        int nelements
    );

    [StructLayout(LayoutKind.Sequential)]
    internal struct XEvent
    {
        public int type;

        public XSelectionRequestEvent xselectionrequest;

        public XSelectionClearEvent xselectionclear;
    }


    internal const int SelectionRequest = 30;
    internal const int SelectionNotify = 31;
    internal const int PropModeReplace = 0;
    internal const int CurrentTime = 0;
    private static readonly IntPtr XA_STRING = new(31);

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
    internal const IntPtr XA_CLIPBOARD = 2;

    public override void CopyText(string text)
    {
        if (IsWayland())
        {
            // using var wlDisplay = WlDisplay.Connect();
            // using var wlRegistry = wlDisplay.GetRegistry();
            //
            // wlRegistry.Global += (_, e) =>
            // {
            //     // DebugHelper.Logger.Debug($"{e.Name}:{e.Interface}:{e.Version}");
            // };

            // wlDisplay.Roundtrip();
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
            if (ev.type != SelectionRequest)
                continue;
            var req = ev.xselectionrequest;
            var response = new XEvent
            {
                type = SelectionNotify
            };
            response.xselectionrequest.display = req.display;
            response.xselectionrequest.requestor = req.requestor;
            response.xselectionrequest.selection = req.selection;
            response.xselectionrequest.target = req.target;
            response.xselectionrequest.property = req.property;
            if (req.target == utf8 || req.target == XA_STRING)
            {
                XChangeProperty(
                    display,
                    req.requestor,
                    req.property,
                    req.target,
                    8,
                    PropModeReplace,
                    textBytes,
                    textBytes.Length
                );
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
        // Save the image in a format that ImageSharp understands for re-loading/processing
        // Using PngEncoder for internal consistency as the clipboard will provide PNG
        image.Save(ms, new PngEncoder());
        // This is important: reload the image from memory to ensure it's in a known state
        var imageForClipboard = Image.Load<Rgba32>(ms.ToArray());


        if (IsWayland())
        {
            DebugHelper.Logger?.Debug("LinuxAPI.CopyImage - Wayland only code");
            // For Wayland, you'd need wl-clipboard or similar native Wayland protocols.
            // This X11 implementation does not apply to Wayland.
            // return;
        }

        try
        {
            // Get the singleton instance of the clipboard handler and set the image
            X11ClipboardHandler.Instance.SetImage(imageForClipboard, filename);
            DebugHelper.Logger?.Debug("X11 image clipboard initiated.");
        }
        catch (Exception ex)
        {
            DebugHelper.Logger?.Error($"Failed to set X11 clipboard image: {ex.Message}");
        }
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
    internal static partial int XQueryPointer(
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
    [LibraryImport(LibX11)]
    public static partial ulong XGetPixel(IntPtr ximage, int x, int y);

    public override Point GetCursorPosition()
    {
        DebugHelper.Logger?.Debug("Get cursor position");
        var display = XOpenDisplay(null);
        if (display == IntPtr.Zero)
        {
            DebugHelper.WriteException(
                new InvalidOperationException("Unable to open X11 display.")
            );
        }

        var rootWindow = XDefaultRootWindow(display);

        XQueryPointer(
            display,
            rootWindow,
            out _,
            out _,
            out var rootX,
            out var rootY,
            out var winX,
            out var winY,
            out var mask
        );

        XCloseDisplay(display);
        DebugHelper.Logger?.Debug(
            "Cursor position: {RootX}, {RootY}, {WinX}, {WinY}, {Mask}",
            rootX,
            rootY,
            winX,
            winY,
            mask
        );
        return new Point(rootX, rootY);
    }
    static int GetShift(ulong mask)
    {
        if (mask == 0)
            return 0;

        int shift = 0;
        while ((mask & 1u) == 0 && shift < 32)
        {
            shift++;
            mask >>= 1;
        }

        return shift;
    }


    [DllImport(LibX11)]
    private static extern int XGetWindowAttributes(
        IntPtr display,
        IntPtr window,
        out XWindowAttributes attributes
    );

    internal enum MapState
    {
        IsUnmapped = 0,
        IsUnviewable = 1,
        IsViewable = 2
    }
    internal enum Gravity
    {
        ForgetGravity = 0,
        NorthWestGravity = 1,
        NorthGravity = 2,
        NorthEastGravity = 3,
        WestGravity = 4,
        CenterGravity = 5,
        EastGravity = 6,
        SouthWestGravity = 7,
        SouthGravity = 8,
        SouthEastGravity = 9,
        StaticGravity = 10
    }
    [StructLayout(LayoutKind.Sequential)]
    internal struct XWindowAttributes
    {
        internal int x;
        internal int y;
        internal int width;
        internal int height;
        internal int border_width;
        internal int depth;
        internal IntPtr visual;
        internal IntPtr root;
        internal int c_class;
        internal Gravity bit_gravity;
        internal Gravity win_gravity;
        internal int backing_store;
        internal IntPtr backing_planes;
        internal IntPtr backing_pixel;
        internal int save_under;
        internal IntPtr colormap;
        internal int map_installed;
        internal MapState map_state;
        internal IntPtr all_event_masks;
        internal IntPtr your_event_mask;
        internal IntPtr do_not_propagate_mask;
        internal int override_direct;
        internal IntPtr screen;
    }
    [StructLayout(LayoutKind.Sequential)]
#pragma warning disable CA1815 // Override equals and operator equals on value types
    internal unsafe struct XImage
#pragma warning restore CA1815 // Override equals and operator equals on value types
    {
        public int width, height; /* size of image */
        public int xoffset; /* number of pixels offset in X direction */
        public int format; /* XYBitmap, XYPixmap, ZPixmap */
        public IntPtr data; /* pointer to image data */
        public int byte_order; /* data byte order, LSBFirst, MSBFirst */
        public int bitmap_unit; /* quant. of scanline 8, 16, 32 */
        public int bitmap_bit_order; /* LSBFirst, MSBFirst */
        public int bitmap_pad; /* 8, 16, 32 either XY or ZPixmap */
        public int depth; /* depth of image */
        public int bytes_per_line; /* accelerator to next scanline */
        public int bits_per_pixel; /* bits per pixel (ZPixmap) */
        public ulong red_mask; /* bits in z arrangement */
        public ulong green_mask;
        public ulong blue_mask;
        private fixed byte funcs[128];
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct XVisualInfo
    {
        internal IntPtr visual;
        internal IntPtr visualid;
        internal int screen;
        internal uint depth;
        internal int klass;
        internal IntPtr red_mask;
        internal IntPtr green_mask;
        internal IntPtr blue_mask;
        internal int colormap_size;
        internal int bits_per_rgb;
    }
    // Event Masks
    internal const long ExposureMask = (1L << 15);
    internal const long StructureNotifyMask = (1L << 17);
    internal const long SubstructureNotifyMask = (1L << 19);
    internal const long KeyPressMask = (1L << 0);
    internal const long KeyReleaseMask = (1L << 1);
    internal const long ButtonPressMask = (1L << 2);
    internal const long ButtonReleaseMask = (1L << 3);
    internal const long PointerMotionMask = (1L << 6);
    internal const long FocusChangeMask = (1L << 20);
    internal const long PropertyChangeMask = (1L << 22);
    internal const long SelectionClearMask = (1L << 23); // Important for clipboard ownership
    internal const long SelectionRequestMask = (1L << 24); // Important for clipboard ownership
    internal const long SelectionNotifyMask = (1L << 25); // Important for clipboard ownership
    internal const long EnterWindowMask = (1L << 4);
    internal const long LeaveWindowMask = (1L << 5);

    internal const int SelectionClear = 29;

    [StructLayout(LayoutKind.Sequential)]
    internal struct XSelectionClearEvent
    {
        public int type;
        public IntPtr serial;
        public bool send_event;
        public IntPtr display;
        public IntPtr selection; // Atom
        public long time;
    }
    [StructLayout(LayoutKind.Sequential)]
    internal struct XSelectionEvent
    {
        public int type;
        public IntPtr serial;
        public bool send_event;
        public IntPtr display;
        public IntPtr requestor;
        public IntPtr selection;
        public IntPtr target;
        public IntPtr property;
        public long time;
    }
}
