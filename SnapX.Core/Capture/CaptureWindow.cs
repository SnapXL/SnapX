
// SPDX-License-Identifier: GPL-3.0-or-later


using SnapX.Core.Job;
using SnapX.Core.Media;

namespace SnapX.Core.Capture;

public class CaptureWindow : CaptureBase
{
    public IntPtr WindowHandle { get; protected set; }

    public CaptureWindow()
    {
    }

    public CaptureWindow(IntPtr windowHandle)
    {
        WindowHandle = windowHandle;
    }

    protected override TaskMetadata Execute(TaskSettings taskSettings)
    {
        WindowInfo windowInfo = new(WindowHandle);

        if (windowInfo.IsMinimized)
        {
            windowInfo.Restore();
            Thread.Sleep(250);
        }

        if (!windowInfo.IsActive)
        {
            windowInfo.Activate();
            Thread.Sleep(100);
        }

        var metadata = new TaskMetadata();
        metadata.UpdateInfo(windowInfo);

        if (taskSettings.CaptureSettings.CaptureTransparent && !taskSettings.CaptureSettings.CaptureClientArea)
        {
            metadata.Image = TaskHelpers.GetScreenshot(taskSettings).CaptureWindowTransparent(WindowHandle);
        }
        else
        {
            metadata.Image = TaskHelpers.GetScreenshot(taskSettings).CaptureWindow(WindowHandle);
        }

        return metadata;
    }
}

