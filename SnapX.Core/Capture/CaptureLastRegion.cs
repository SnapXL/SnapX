
// SPDX-License-Identifier: GPL-3.0-or-later


using SnapX.Core.Job;

namespace SnapX.Core.Capture;

public class CaptureLastRegion : CaptureRegion
{
    protected override TaskMetadata Execute(TaskSettings taskSettings)
    {
        switch (lastRegionCaptureType)
        {
            default:
            case RegionCaptureType.Default: return ExecuteRegionCapture(taskSettings);
            case RegionCaptureType.Light: return ExecuteRegionCaptureLight(taskSettings);
            case RegionCaptureType.Transparent: return ExecuteRegionCaptureTransparent(taskSettings);
        }
    }
}

