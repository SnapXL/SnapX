using SixLabors.ImageSharp;
using SnapX.Core.Interfaces;
using SnapX.Core.Media;
using SnapX.Core.SharpCapture;
using SnapX.Core.SharpCapture.Linux;
using SnapX.Core.SharpCapture.macOS;
using SnapX.Core.Utils.Extensions;
#if TARGET_WINDOWS
using SnapX.Core.SharpCapture.Windows;
#endif

namespace SnapX.Core.Utils.Native;

public class Methods(ILoggerService _logger)
{
    private ILoggerService Logger => _logger;
    private static bool IsMacOS => OperatingSystem.IsMacOS();
    private static bool IsLinux => OperatingSystem.IsLinux();
    private static bool IsWindows => OperatingSystem.IsWindows();
    private static bool IsFreeBSD => OperatingSystem.IsFreeBSD();


    internal static INativeAPI NativeAPI
    {
        get
        {
#if WINDOWS
            return new WindowsAPI();
#else
            if (IsMacOS) return new MacOSAPI();
            if (IsLinux || IsFreeBSD) return new LinuxAPI(DebugHelper.Logger as ILoggerService);
            throw new PlatformNotSupportedException("This platform is not supported for native API calls.");
#endif
        }
    }
    private static BaseSharpCapture SharpCapture
    {
        get
        {
#if WINDOWS
            return new WindowsCapture();
#else
            if (IsMacOS) return new macOSCapture();
            if (IsLinux || IsFreeBSD) return new LinuxCapture();
            throw new PlatformNotSupportedException("This platform is not supported for native API calls.");
#endif
        }
    }
    public static List<WindowInfo> GetWindowList() => NativeAPI.GetWindowList();
    public static Image GetJumboFileIcon(string filePath, bool jumboSize = true) =>
        NativeAPI.GetJumboFileIcon(filePath, jumboSize);
    public static void ShowWindow(WindowInfo window) => NativeAPI.ShowWindow(window);
    public static void RestoreWindow(WindowInfo window) => ShowWindow(window);
    public static void CopyText(string text) => NativeAPI.CopyText(text);
    public static async Task<Image?> CaptureScreen(Rectangle bounds) => await SharpCapture.CaptureScreen(bounds);
    public static async Task<Image?> CaptureScreen(Point pos) => await SharpCapture.CaptureScreen(pos);

    public static async Task<Image?> CaptureFullscreen() => await SharpCapture.CaptureFullscreen();
    public static async Task<Image?> CaptureRectangle(Rectangle rect) => await SharpCapture.CaptureRectangle(rect);
    public static async Task<Image?> CaptureWindow(Point pos) => await SharpCapture.CaptureWindow(pos);
    public static async Task<Rectangle> GetWorkingArea() => await SharpCapture.GetWorkingArea();
    public static async Task<Rectangle> GetPrimaryScreen() => await SharpCapture.GetPrimaryScreen();
    public static async Task<Rectangle> GetActiveScreen() => await SharpCapture.GetScreen(GetCursorPosition());

    public static Rectangle GetScreenBounds(Point pos) => SharpCapture.GetScreen(pos).GetAwaiter().GetResult();
    public static Screen? GetScreen(Point pos) => NativeAPI.GetScreen(pos);

    public static void CopyImage(Image image, string? fileName) => NativeAPI.CopyImage(image, fileName);


    public static Point GetCursorPosition()
    {
        var point = Point.Empty;
        try
        {
            point = NativeAPI.GetCursorPosition();
        }
        catch (Exception ex)
        {
            ex.ShowError();
        }
        DebugHelper.WriteLine($"GetCursorPosition returned {point}");
        return point;
    }

    public static Rectangle GetWindowRectangle(IntPtr windowHandle = 0) =>
        NativeAPI.GetWindowRectangle(windowHandle);

    public static WindowInfo GetForegroundWindow()
    {
        // TODO: Reimplement GetForegroundWindow
        return new WindowInfo();
    }

    // Linux (Wayland): Use DBus to interact with the Wayland compositor
    private static Rectangle GetWindowRectangleWayland(IntPtr windowHandle)
    {
        // In practice, Wayland doesn't expose direct window information as X11 does.
        // You would need to use a DBus interface with the compositor (e.g., Gnome, KDE) to fetch window information.
        // This is more complex and would require integration with specific Wayland compositors.
        // For this example, we leave it unimplemented or you could integrate with dbus library.
        throw new NotImplementedException("Wayland window retrieval is not implemented.");
    }

    private static Rectangle GetWindowRectangleMacOS(IntPtr windowHandle)
    {
        throw new NotImplementedException("MacOS window retrieval is not implemented.");
    }

}

