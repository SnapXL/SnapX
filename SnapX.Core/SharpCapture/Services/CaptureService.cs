using SixLabors.ImageSharp;
using SnapX.Core.Job;
using SnapX.Core.SharpCapture.Interfaces;
using SnapX.Core.Utils.Native;

namespace SnapX.Core.SharpCapture.Services;

public class CaptureService(BaseSharpCapture _baseCapture, INativeAPI _nativeAPI) : ICaptureService
{
    public async Task<Image?> CaptureActiveMonitorAsync(TaskSettings? taskSettings = null)
    {
        taskSettings ??= TaskSettings.GetDefaultTaskSettings();
        var bounds = await _baseCapture.GetScreen(_nativeAPI.GetCursorPosition());

        return await _baseCapture.CaptureScreen(bounds, taskSettings);
    }

    public async Task<Image?> CaptureFullscreenAsync(TaskSettings? taskSettings = null)
    {
        throw new NotImplementedException();
    }

    public async Task<Image?> CaptureRectangle(TaskSettings? taskSettings = null, Rectangle? rect = null)
    {
        throw new NotImplementedException();
    }

    public async Task<Image?> CaptureActiveWindowAsync(TaskSettings? taskSettings = null)
    {
        throw new NotImplementedException();
    }
}
