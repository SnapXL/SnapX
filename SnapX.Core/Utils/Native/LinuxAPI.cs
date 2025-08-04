using System.Runtime.InteropServices;
using System.Text;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;
using SnapX.Core.Interfaces;
using SnapX.Core.Media;

namespace SnapX.Core.Utils.Native;

public partial class LinuxAPI(ILoggerService Logger) : INativeAPI
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
        return !string.IsNullOrEmpty(sessionVersion)
            && sessionVersion.Contains("gnome", StringComparison.OrdinalIgnoreCase);
    }

    public Rectangle GetWindowRectangle(WindowInfo window)
    {
        return GetWindowRectangleX11(window.Handle);
    }

    public Rectangle GetWindowRectangle(IntPtr windowHandle)
    {
        return GetWindowRectangleX11(windowHandle);
    }

    public Screen? GetScreen(Point pos)
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
            XGetGeometry(
                display,
                rootWindow,
                out _,
                out var x,
                out var y,
                out var width,
                out var height,
                out _,
                out _
            );

            if (pos.X < x || pos.X > x + (int)width || pos.Y < y || pos.Y > y + (int)height)
                continue;
            Logger.Debug("Point {Pos} is within screen {I} bounds", pos, i);
            return new Screen()
            {
                Bounds = new Rectangle(x, y, (int)width, (int)height),
                Name = "NotImplementedName",
                Id = "NotImplementedID",
            };
        }

        XCloseDisplay(display);
        return null;
    }

    public void ShowWindow(WindowInfo windowInfo)
    {
        throw new NotImplementedException();
    }

    public void ShowWindow(IntPtr hwnd)
    {
        throw new NotImplementedException();
    }

    public Image GetJumboFileIcon(string filePath, bool jumboSize = true)
    {
        throw new NotImplementedException();
    }

    public void HideWindow(WindowInfo windowInfo)
    {
        throw new NotImplementedException();
    }

    public void HideWindow(IntPtr handle)
    {
        throw new NotImplementedException();
    }

    public List<WindowInfo> GetWindowList()
    {
        var windows = new List<WindowInfo>();
        if (IsWayland())
        {
            // if (IsPlasma())
            // {
            //     // Interact with Plasmashell over DBus
            //     return windows;
            // }
            //
            // if (IsGNOME())
            // {
            //     // Interact with GNOME Shell
            //     return windows;
            // }

            return windows;
        }

        var display = XOpenDisplay(null);
        if (display == IntPtr.Zero)
        {
            Logger.Debug("Unable to open X display");
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
        if (status == 0)
        {
            Logger.Debug("XQueryTree failed");
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
            windows.Add(
                new WindowInfo
                {
                    Handle = window,
                    Title = title,
                    IsVisible = isVisible,
                    Rectangle = rect,
                    IsMinimized = IsWindowMinimized(display, window),
                    IsActive = isActive,
                }
            );
        }

        XCloseDisplay(display);
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
    internal static partial IntPtr XGetWindowProperty(
        IntPtr display,
        IntPtr window,
        IntPtr property,
        long offset,
        long length,
        [MarshalAs(UnmanagedType.Bool)] bool delete,
        IntPtr type,
        out IntPtr prop_return,
        out uint nitems,
        out uint bytes_after,
        out int format
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

    private const long ALL_PLANES = -1;
    public const int ZPIXMAP = 2;

    internal Image TakeScreenshotWithX11(Screen screen)
    {
        unsafe
        {
            DebugHelper.WriteLine(
                $"Screenshotting screen {screen.Name} with {screen.Resolution} ({screen.Id})"
            );
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
            Logger.Debug("x: {AttributesX}", attributes.x);
            Logger.Debug("y: {AttributesY}", attributes.y);
            Logger.Debug("width: {AttributesWidth}", attributes.width);
            Logger.Debug("height: {AttributesHeight}", attributes.height);
            Logger.Debug(
                "border_width: {AttributesBorderWidth}",
                attributes.border_width
            );
            Logger.Debug("depth: {AttributesDepth}", attributes.depth);
            Logger.Debug("visual: {AttributesVisual}", attributes.visual);
            Logger.Debug("root: {AttributesRoot}", attributes.root);
            Logger.Debug("colormap: {AttributesColormap}", attributes.colormap);

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
            {
                throw new Exception("Unable to capture screen image using X11.");
            }

            var xImage = Marshal.PtrToStructure<XImage>(imagePtr);

            var pixelsPtr = (IntPtr)xImage.data;

            // Calculate the size of the pixel data in bytes
            // XImage's bytes_per_line is typically the width * bytes_per_pixel (depth / 8)
            // However, it's safer to use the XImage's width and height fields directly for allocation,
            // and then copy row by row if byte order or padding is an issue.
            // For simplicity, assuming a direct byte copy is feasible for common ZPIXMAP depths (e.g., 24 or 32 bits).
            var bytesPerPixel = xImage.depth / 8;
            var pixelDataSize = xImage.width * xImage.height * bytesPerPixel;
            Logger.Debug("bytesPerPixel: {BytesPerPixel}", bytesPerPixel);
            Logger.Debug("pixelDataSize: {PixelDataSize}", pixelDataSize);

            // Create a byte array to hold the pixel data
            var pixelData = new byte[pixelDataSize];

            Marshal.Copy(pixelsPtr, pixelData, 0, pixelDataSize);

            // Depending on the depth and byte order, you might need to reorder the pixel data.
            // XGetImage usually returns data in the display's native byte order and format.
            // For Rgba32, you often need 4 bytes per pixel (Red, Green, Blue, Alpha).
            // XGetImage with ZPIXMAP typically returns BGR or BGRA. If the target format is RGBA,
            // you'll need to swap byte order (e.g., if it's BGRA, swap B and R for each pixel).
            // This example assumes a direct conversion to Rgba32 is possible or handles it internally.
            // If 'Rgba32' expects RGBA and XGetImage gives BGRA, a conversion loop would be needed here.
            // For example:
            if (bytesPerPixel == 4) // Assuming 32-bit depth (BGRA or ARGB)
            {
                for (var i = 0; i < pixelDataSize; i += 4)
                {
                    // Swap B and R (if it's BGRA to RGBA)
                    (pixelData[i], pixelData[i + 2]) = (pixelData[i + 2], pixelData[i]);
                    // pixelData[i+3] is A (alpha), remains in place
                }
            }
            else if (bytesPerPixel == 3) // Assuming 24-bit depth (BGR)
            {
                // Convert BGR to RGB
                for (var i = 0; i < pixelDataSize; i += 3)
                {
                    (pixelData[i], pixelData[i + 2]) = (pixelData[i + 2], pixelData[i]);
                }

                // If converting to Rgba32, you'd also need to add an Alpha channel.
                // This would involve creating a new larger array and copying with alpha = 255.
                // For simplicity, if Image.LoadPixelData<Rgba32> can handle 3-byte input by adding alpha, it's fine.
                // Otherwise, a more complex conversion to a new RGBA byte array is required.
                var rgbaPixelData = new byte[(pixelDataSize / 3) * 4];
                var rgbaIndex = 0;
                for (var i = 0; i < pixelDataSize; i += 3)
                {
                    rgbaPixelData[rgbaIndex++] = pixelData[i]; // R
                    rgbaPixelData[rgbaIndex++] = pixelData[i + 1]; // G
                    rgbaPixelData[rgbaIndex++] = pixelData[i + 2]; // B
                    rgbaPixelData[rgbaIndex++] = 255; // A (fully opaque)
                }

                pixelData = rgbaPixelData; // Use the new RGBA data
            }

            // Create the Image<Rgba32> from the pixel data
            // Ensure the pixelData format matches what Rgba32 expects (typically RGBA).
            // The width and height of the screen object are used.
            var image = Image.LoadPixelData<Rgba32>(
                pixelData,
                screen.Bounds.Width,
                screen.Bounds.Height
            );

            // Free the XImage data when done
            XDestroyImage(imagePtr); // This frees the data pointed to by xImage.data as well

            XCloseDisplay(display);

            return image;
        }
    }

    private static string GetWindowTitle(IntPtr display, IntPtr window)
    {
        var windowTitlePtr = XFetchName(display, window);
        return windowTitlePtr != IntPtr.Zero
            ? Marshal.PtrToStringAnsi(windowTitlePtr) ?? "Untitled"
            : "Untitled";
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

    [LibraryImport(LibX11)]
    internal static partial IntPtr XFetchName(IntPtr display, IntPtr window);

    [LibraryImport(LibX11)]
    internal static partial int XDestroyImage(IntPtr ximage);

    [LibraryImport(LibX11)]
    internal static partial void XCloseDisplay(IntPtr display);

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

    public void CopyText(string text)
    {
        if (IsWayland())
        {
            // call dbus to copy text to clipboard
            DebugHelper.WriteLine("This code will crash SnapX on Wayland.");
            return;
        }
        var display = XOpenDisplay(null);
        if (display == IntPtr.Zero)
        {
            Logger.Debug("Unable to open X11 display");
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
            var response = new XEvent();
            response.type = SelectionNotify;
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

    public void CopyImage(Image image, string? filename)
    {
        using var ms = new MemoryStream();
        // Save the image in a format that ImageSharp understands for re-loading/processing
        // Using PngEncoder for internal consistency as the clipboard will provide PNG
        image.Save(ms, new PngEncoder());
        // This is important: reload the image from memory to ensure it's in a known state
        // and to get a Rgba32 image, as that's often what ImageSharp works best with internally.
        // If you always work with Rgba32 from TakeScreenshotWithX11, this might be redundant.
        var imageForClipboard = Image.Load<Rgba32>(ms.ToArray());


        if (IsWayland())
        {
            Logger.Debug("LinuxAPI.CopyImage - Wayland only code");
            // For Wayland, you'd need wl-clipboard or similar native Wayland protocols.
            // This X11 implementation does not apply to Wayland.
            // return;
        }

        try
        {
            // Get the singleton instance of the clipboard handler and set the image
            new X11ClipboardHandler(Logger).SetImage(imageForClipboard, filename);
            Logger.Debug("X11 image clipboard initiated");
        }
        catch (Exception ex)
        {
            Logger.Error("Failed to set X11 clipboard image: {ExMessage}", ex.Message);
        }
    }

    private static Rectangle GetWindowRectangleX11(IntPtr windowHandle)
    {
        IntPtr display = XOpenDisplay(null);
        if (display == IntPtr.Zero)
            throw new InvalidOperationException("Unable to open X11 display.");

        return XGetWindowAttributes(display, windowHandle, out var attributes) != 0 ? new Rectangle(attributes.x, attributes.y, attributes.width, attributes.height) : throw new InvalidOperationException("Unable to get window attributes.");
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

    public Point GetCursorPosition()
    {
        Logger.Debug("Get cursor position");
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
        Logger.Debug(
            "Cursor position: {RootX}, {RootY}, {WinX}, {WinY}, {Mask}",
            rootX,
            rootY,
            winX,
            winY,
            mask
        );
        return new Point(rootX, rootY);
    }

    [DllImport(LibX11)]
    private static extern int XGetWindowAttributes(
        IntPtr display,
        IntPtr window,
        out XWindowAttributes attributes
    );

    [StructLayout(LayoutKind.Sequential)]
    public struct XWindowAttributes
    {
        public int x,
            y;
        public int width,
            height;
        public int border_width,
            depth;
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
