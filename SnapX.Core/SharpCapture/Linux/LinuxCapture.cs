using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using SnapX.Core.Media;
using SnapX.Core.SharpCapture.Linux.DBus;
using SnapX.Core.Utils.Native;
using Tmds.DBus;
using Tmds.DBus.Protocol;

namespace SnapX.Core.SharpCapture.Linux;

public class LinuxCapture : BaseCapture
{
    public override async Task<Image?> CaptureFullscreen()
    {
        var isWayland = LinuxAPI.IsWayland();

        if (IsCompositorKwin)
        {
            try
            {
                return await TakeScreenshotWithKwin();
            }
            catch { }
        }

        if (isWayland)
        {
            try
            {
                return await TakeScreenshotWithPortal();
            }
            catch (Exception ex)
            {
                DebugHelper.WriteException(ex);
            }
        }

        try
        {
            return LinuxAPI.TakeFullscreenScreenshot();
        }
        catch (Exception ex)
        {
            DebugHelper.WriteException(ex);
        }

        return null;
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
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        using var connection = new Connection(Address.Session!);
        await connection.ConnectAsync().ConfigureAwait(false);

        var screenShotService = new ScreenShot2Service(connection, "org.kde.KWin.ScreenShot2");
        var screenshot = screenShotService.CreateScreenShot2("/org/kde/KWin/ScreenShot2");

        var options = new Dictionary<string, VariantValue>
        {
            { "include-cursor", false },
            { "native-resolution", true }
        };

        var tempFile = Path.GetTempFileName();
        try
        {
            using var fileHandle = File.OpenHandle(tempFile, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite, FileOptions.DeleteOnClose);

            var result = await screenshot.CaptureWorkspaceAsync(options, fileHandle).WaitAsync(cts.Token).ConfigureAwait(false);
            var expectedSize = (long)result.Stride * result.Height;

            using var fileCheckCts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            while (new FileInfo(tempFile).Length < expectedSize)
            {
                await Task.Delay(50, fileCheckCts.Token).ConfigureAwait(false);
            }

            return await QImage.LoadAsync(tempFile, result).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            throw new TimeoutException("The KWin screenshot operation or file write timed out.");
        }
        finally
        {
            if (File.Exists(tempFile))
            {
                try
                {
                    File.Delete(tempFile);
                }
                catch
                {
                    // Ignore
                }
            }
        }
    }

    private static Image CropFullscreenScreenshotToBounds(Rectangle bounds, Image img)
    {
        if (img == null)
        {
            DebugHelper.Logger?.Debug("Crop failed: Source image is null.");
            return null;
        }

        var x = Math.Clamp(bounds.X, 0, img.Width);
        var y = Math.Clamp(bounds.Y, 0, img.Height);

        var width = Math.Clamp(bounds.Width, 0, img.Width - x);
        var height = Math.Clamp(bounds.Height, 0, img.Height - y);

        if (width <= 0 || height <= 0)
        {
            DebugHelper.Logger?.Debug($"Crop aborted: Resulting bounds {width}x{height} are empty. Original image kept.");
            return img;
        }

        var cropRectangle = new Rectangle(x, y, width, height);

        DebugHelper.Logger?.Debug($"Cropping {img.Width}x{img.Height} image to {cropRectangle.Width}x{cropRectangle.Height} at offset {cropRectangle.X},{cropRectangle.Y}");

        try
        {
            img.Mutate(ctx => ctx.Crop(cropRectangle));
        }
        catch (Exception ex)
        {
            DebugHelper.Logger?.Debug($"ImageSharp Mutation Error: {ex.Message}");
        }

        return img;
    }
    public override async Task<Image?> CaptureScreen(Rectangle bounds)
    {
        var fullscreenImage = await CaptureFullscreen().ConfigureAwait(false);

        if (fullscreenImage == null)
        {
            DebugHelper.Logger?.Error("[LinuxCapture] Fullscreen capture returned null.");
            return null;
        }

        return CropFullscreenScreenshotToBounds(bounds, fullscreenImage);
    }

    public override async Task<Image?> CaptureScreen(Point? pos)
    {
        if (pos == null)
        {
            DebugHelper.Logger?.Error("[LinuxCapture] Position point was null.");
            throw new ArgumentNullException(nameof(pos));
        }

        var rect = await GetScreen(pos.Value).ConfigureAwait(false);

        if (rect != Rectangle.Empty) return await CaptureScreen(rect).ConfigureAwait(false);
        DebugHelper.Logger?.Error("[LinuxCapture] Could not find screen at coordinates: {Point}", pos.Value);
        return null;

    }
    public override async Task<Image?> CaptureScreen(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            DebugHelper.Logger?.Error("[LinuxCapture] Screen to capture was null or empty.");
            throw new ArgumentNullException(nameof(name));
        }

        var rect = await GetScreen(name).ConfigureAwait(false);
        return await CaptureScreen(rect).ConfigureAwait(false);
    }

    public override async Task<Image?> CaptureScreen(Screen screen)
    {
        var fullscreenImage = await CaptureFullscreen().ConfigureAwait(false);

        if (fullscreenImage == null)
        {
            DebugHelper.Logger?.Error("[LinuxCapture] Fullscreen capture returned null.");
            return null;
        }

        return CropFullscreenScreenshotToBounds(screen.Bounds, fullscreenImage);
    }

    public override async Task<Rectangle> GetScreen(Point pos) => Methods.NativeAPI.GetScreen(pos)?.Bounds ?? Rectangle.Empty;
    public override async Task<Rectangle> GetScreen(string name) => ((LinuxAPI)Methods.NativeAPI).GetScreen(name)?.Bounds ?? Rectangle.Empty;

    public override async Task<Rectangle> GetWorkingArea() => ((LinuxAPI)Methods.NativeAPI).GetScreenBounds();
    public override async Task<Image?> CaptureRectangle(Rectangle rect)
    {
        return CropFullscreenScreenshotToBounds(rect, await CaptureFullscreen().ConfigureAwait(false));
    }
    public override Task<Image?> CaptureWindow(WindowInfo window)
    {
        return Task.Run(() => ((LinuxAPI)Methods.NativeAPI).TakeScreenshotOfX11Window(window));
    }
    public override async Task<Image?> CaptureWindow(Point pos)
    {
        var windows = Methods.GetWindowList();

        var targetWindow = windows
            .Where(w => w is { Rectangle: { Width: > 0, Height: > 0 } })
            .Reverse()
            .FirstOrDefault(window => window.Rectangle.Contains(pos));

        if (targetWindow == null)
        {
            DebugHelper.Logger?.Debug($"No window found at {pos}");
            return null;
        }

        return await CaptureWindow(targetWindow);
    }

    private static bool IsCompositorKwin => Environment.GetEnvironmentVariable("XDG_SESSION_TYPE") == "wayland" && Environment.GetEnvironmentVariable("XDG_CURRENT_DESKTOP") == "KDE";
}
