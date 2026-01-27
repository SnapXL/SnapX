
// SPDX-License-Identifier: GPL-3.0-or-later


using SixLabors.ImageSharp;
using SnapX.Core.Job;

namespace SnapX.Core.Capture;

public class CaptureMonitor : CaptureBase
{
    public Rectangle MonitorRectangle { get; private set; }

    public CaptureMonitor(Rectangle monitorRectangle)
    {
        MonitorRectangle = monitorRectangle;
    }

    protected override TaskMetadata Execute(TaskSettings taskSettings)
    {
        DebugHelper.WriteLine("CaptureMonitor Start");
        var metadata = CreateMetadata(MonitorRectangle);
        metadata.Image = TaskHelpers.GetScreenshot().CaptureRectangle(MonitorRectangle);
        return metadata;
    }
}

