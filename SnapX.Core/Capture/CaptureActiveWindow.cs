
// SPDX-License-Identifier: GPL-3.0-or-later


using SnapX.Core.Job;

namespace SnapX.Core.Capture;

public class CaptureActiveWindow : CaptureBase
{
    protected override TaskMetadata Execute(TaskSettings taskSettings)
    {
        var metadata = CreateMetadata();

        if (taskSettings.CaptureSettings.CaptureTransparent && !taskSettings.CaptureSettings.CaptureClientArea)
        {
            metadata.Image = TaskHelpers.GetScreenshot(taskSettings).CaptureActiveWindowTransparent();
        }
        else
        {
            metadata.Image = TaskHelpers.GetScreenshot(taskSettings).CaptureActiveWindow();
        }

        return metadata;
    }
}
