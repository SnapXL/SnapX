
// SPDX-License-Identifier: GPL-3.0-or-later


using SnapX.Core.Job;

namespace SnapX.Core.Capture;

public class CaptureActiveMonitor : CaptureBase
{
    protected override TaskMetadata Execute(TaskSettings taskSettings)
    {
        DebugHelper.WriteLine("CaptureActiveMonitor started");
        var promise = TaskHelpers.GetScreenshot(taskSettings).CaptureActiveMonitor();
        promise.Wait();
        var img = promise.Result;
        var metadata = CreateMetadata(img.Bounds);
        metadata.Image = img;
        return metadata;
    }
}

