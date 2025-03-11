using SixLabors.ImageSharp;
using SnapX.Core.Media;

namespace SnapX.Core.ScreenCapture.SharpCapture;

public class BaseCapture
{
    public virtual async Task<Image?> CaptureScreen(Screen? screen) =>
        throw new NotImplementedException("SharpCapture CaptureScreen is not implemented.");
    public virtual async Task<Image?> CaptureScreen(Point? pos) =>
        throw new NotImplementedException("SharpCapture CaptureScreen is not implemented.");
    public virtual async Task<Image?> CaptureWindow(Point pos) =>
        throw new NotImplementedException("SharpCapture CaptureWindow is not implemented.");
    public virtual async Task<Image?> CaptureWindow(WindowInfo? windowInfo) =>
        throw new NotImplementedException("SharpCapture CaptureWindow is not implemented.");
    public virtual async Task<Image?> CaptureRectangle(Rectangle rect) =>
        throw new NotImplementedException("SharpCapture CaptureRectangle is not implemented.");
    public virtual async Task<Image?> CaptureFullscreen() =>
        throw new NotImplementedException("SharpCapture CaptureFullscreen is not implemented.");
    public virtual async Task<Rectangle> GetWorkingArea() =>
        throw new NotImplementedException("SharpCapture GetWorkingArea is not implemented.");
    public virtual async Task<Screen> GetPrimaryScreen() =>
        throw new NotImplementedException("SharpCapture GetPrimaryScreen is not implemented.");
    public virtual async Task<Screen> GetScreen(Point pos) =>
        throw new NotImplementedException("SharpCapture GetScreen is not implemented.");
}
