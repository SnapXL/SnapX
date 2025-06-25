using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using SnapX.Core.SharpCapture.Linux.DBus;
using SnapX.Core.Utils.Native;
using Tmds.DBus;
using Tmds.DBus.Protocol;

namespace SnapX.Core.SharpCapture.Linux;

public class LinuxCapture : BaseCapture
{
    public override async Task<Image?> CaptureFullscreen()
    {
        // if (LinuxAPI.IsWayland()) return await TakeScreenshotWithPortal();

        if (!IsCompositorKwin) return await TakeScreenshotWithPortal();
        // Todo: replace try catch with method that checks for valid kwin permissions.
        try
        {
            return await TakeScreenshotWithKwin();
        }
        catch (Exception e)
        {
            // Fallback to portal method.
        }

        return await TakeScreenshotWithPortal();
    }

    private static async Task<Image> TakeScreenshotWithPortal()
    {
        var connection = new Connection(Address.Session!);
        await connection.ConnectAsync().ConfigureAwait(false);
        var desktop = new DesktopService(connection, "org.freedesktop.portal.Desktop");
        // var access = new DesktopService(connection, "org.freedesktop.access");
        var screenshot = desktop.CreateScreenshot("/org/freedesktop/portal/desktop");
        var options = new Dictionary<string, VariantValue>()
        {
            // { "interactive", true }
        };
        var timeoutTask = Task.Delay(10000);
        var portalResponse = connection.Call(() => screenshot.ScreenshotAsync("", options));

        var completedTask = await Task.WhenAny(portalResponse, timeoutTask);

        if (completedTask == timeoutTask)
        {
            throw new TimeoutException("Call to org.freedesktop.portal.Desktop Screenshot timed out. Please try again.");
        }
        var Response = await portalResponse;
        var uri = new Uri(Response.Results["uri"].GetString());
        var fileURL = Uri.UnescapeDataString(uri.LocalPath);
        var img = await Image.LoadAsync(fileURL);
        _ = Task.Run(() => File.Delete(fileURL));

        return img;
    }

    // A significantly faster solution for screen capturing on KDE Wayland over FreeDesktop Portals.
    //
    // Instead of creating/contributing a new wayland protocol or using an existing wayland protocol for screen capturing,
    // KWin provides a special dbus interface `org.kde.KWin.ScreenShot2` for taking screenshots without prompting the user. This is meant for their in-house screenshot app `Spectacle`.
    // However, this interface *can* be used by other apps, as long as you follow a few rules:
    //   1. There must be a .desktop file in a privileged location e.g., /usr/share/applications/
    //   2. The .desktop entry `Exec` *must* point to a bin located in a privileged location e.g., `Exec=/usr/bin/snapx`
    //   3. The .desktop file *must* contain the following entry: `X-KDE-DBUS-Restricted-Interfaces=org.kde.KWin.ScreenShot2`
    //
    // If all these rules are followed, KWin will allow SnapX to take privileged, unprompted screenshots on wayland.
    // Interface Documentation: https://github.com/KDE/kwin/blob/master/src/plugins/screenshot/org.kde.KWin.ScreenShot2.xml
    private static async Task<Image> TakeScreenshotWithKwin()
    {
        var connection = new Connection(Address.Session!);
        await connection.ConnectAsync().ConfigureAwait(false);
        var screenShotService = new ScreenShot2Service(connection, "org.kde.KWin.ScreenShot2");
        var screenshot = screenShotService.CreateScreenShot2("/org/kde/KWin/ScreenShot2");
        var options = new Dictionary<string, VariantValue>()
        {
            // { "include-cursor", false },
            // { "native-resolution", false },
        };

        var tempFile = Path.GetTempFileName();
        var fileHandle = File.OpenHandle(tempFile, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite);

        var timeoutTask = Task.Delay(10000);
        var kwinResponse = screenshot.CaptureWorkspaceAsync(options, fileHandle);

        var completedTask = await Task.WhenAny(kwinResponse, timeoutTask);
        if (completedTask == timeoutTask)
        {
            throw new TimeoutException("Call to org.kde.KWin.ScreenShot2 Screenshot timed out. Please try again.");
        }

        var result = await kwinResponse;
        var expectedSize = result.Stride * (long)result.Height;

        while (new FileInfo(tempFile).Length < expectedSize)
        {
            await Task.Delay(100);
            // Todo Timeout
        }

        var image = await QImage.LoadAsync(tempFile, result);
        _ = Task.Run(() => File.Delete(tempFile));

        return image;
    }

    private static Image CropFullscreenScreenshotToBounds(Rectangle bounds, Image img)
    {
        var cropRectangle = new Rectangle(
            Math.Max(0, bounds.X),
            Math.Max(0, bounds.Y),
            Math.Min(img.Width - bounds.X, bounds.Width),
            Math.Min(img.Height - bounds.Y, bounds.Height)
        );

        img.Mutate(x => x.Crop(cropRectangle));

        return img;
    }
    public override async Task<Image?> CaptureScreen(Rectangle bounds)
    {
        // TODO: Implement pure X11 screenshotting instead of using portal
        // if (LinuxAPI.IsWayland())
        // {


        var fullscreenImage = await CaptureFullscreen().ConfigureAwait(false);
        var croppedImage = CropFullscreenScreenshotToBounds(bounds, fullscreenImage);
        Console.WriteLine($"Original: {fullscreenImage.Width}x{fullscreenImage.Height} After: {croppedImage.Width}x{croppedImage.Height}");
        return croppedImage;
        // }

        // return LinuxAPI.TakeScreenshotWithX11(screen);
    }
    public override async Task<Image?> CaptureScreen(Point? pos)
    {
        if (pos == null || !pos.HasValue) throw new ArgumentNullException(nameof(pos));
        return await CaptureScreen(await GetScreen(pos.Value));
    }

    public override async Task<Rectangle> GetScreen(Point pos) => Methods.NativeAPI.GetScreen(pos).Bounds;

    public override async Task<Rectangle> GetWorkingArea() => ((LinuxAPI)Methods.NativeAPI).GetScreenBounds();
    public override async Task<Image?> CaptureRectangle(Rectangle rect)
    {
        return CropFullscreenScreenshotToBounds(rect, await CaptureFullscreen().ConfigureAwait(false));
    }

    private static bool IsCompositorKwin => Environment.GetEnvironmentVariable("XDG_SESSION_TYPE") == "wayland" && Environment.GetEnvironmentVariable("XDG_CURRENT_DESKTOP") == "KDE";
}
