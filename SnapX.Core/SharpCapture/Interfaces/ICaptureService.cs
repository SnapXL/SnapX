using SixLabors.ImageSharp;
using SnapX.Core.Job;

namespace SnapX.Core.SharpCapture.Interfaces;

public interface ICaptureService
{
    Task<Image?> CaptureActiveMonitorAsync(TaskSettings? taskSettings = null);
    Task<Image?> CaptureFullscreenAsync(TaskSettings? taskSettings = null);
    Task<Image?> CaptureRectangle(TaskSettings? taskSettings = null, Rectangle? rect = null);
    Task<Image?> CaptureActiveWindowAsync(TaskSettings? taskSettings = null);
}
