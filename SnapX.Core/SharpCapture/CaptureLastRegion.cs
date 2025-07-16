// SPDX-License-Identifier: GPL-3.0-or-later


using SnapX.Core.Interfaces;
using SnapX.Core.Job;
using SnapX.Core.SharpCapture.Interfaces;

namespace SnapX.Core.SharpCapture;
public class CaptureLastRegion(IMainWindowService MainWindowService, INotificationService NotificationService, IUploadManager UploadManager, IDelayService DelayService, ILoggerService LoggerService, ICaptureService CaptureService, RegionCaptureType RegionCaptureType) : CaptureRegion(MainWindowService, NotificationService, UploadManager, DelayService, LoggerService, CaptureService, RegionCaptureType)
{
    protected override async Task<TaskMetadata> ExecuteAsync(TaskSettings taskSettings)
    {
        throw new NotImplementedException();
    }
    // protected override TaskMetadata Execute(TaskSettings taskSettings)
    // {
    //     switch (lastRegionCaptureType)
    //     {
    //         default:
    //         case RegionCaptureType.Default: return ExecuteRegionCapture(taskSettings);
    //         case RegionCaptureType.Light: return ExecuteRegionCaptureLight(taskSettings);
    //         case RegionCaptureType.Transparent: return ExecuteRegionCaptureTransparent(taskSettings);
    //     }
    // }
}

