using SixLabors.ImageSharp;
using SnapX.Core.Job;

#pragma warning disable CS1998

namespace SnapX.Core.SharpCapture;

public abstract class BaseSharpCapture
{
    public virtual async Task<Image?> CaptureScreen(Rectangle bounds, TaskSettings? taskSettings = null) =>
        throw new NotImplementedException("SharpCapture CaptureScreen is not implemented.");
    public virtual async Task<Image?> CaptureScreen(Point? pos, TaskSettings? taskSettings = null) =>
        throw new NotImplementedException("SharpCapture CaptureScreen is not implemented.");
    public virtual async Task<Image?> CaptureWindow(Point pos, TaskSettings? taskSettings = null) =>
        throw new NotImplementedException("SharpCapture CaptureWindow is not implemented.");
    public virtual async Task<Image?> CaptureRectangle(Rectangle rect, TaskSettings? taskSettings = null) =>
        throw new NotImplementedException("SharpCapture CaptureRectangle is not implemented.");
    public virtual async Task<Image?> CaptureFullscreen(TaskSettings? taskSettings = null) =>
        throw new NotImplementedException("SharpCapture CaptureFullscreen is not implemented.");
    public virtual async Task<Rectangle> GetWorkingArea() =>
        throw new NotImplementedException("SharpCapture GetWorkingArea is not implemented.");
    public virtual async Task<Rectangle> GetPrimaryScreen() =>
        throw new NotImplementedException("SharpCapture GetPrimaryScreen is not implemented.");
    public virtual async Task<Rectangle> GetScreen(Point pos) =>
        throw new NotImplementedException("SharpCapture GetScreen is not implemented.");
}
