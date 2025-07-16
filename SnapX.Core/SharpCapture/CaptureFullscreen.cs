// SPDX-License-Identifier: GPL-3.0-or-later


using SnapX.Core.Interfaces;
using SnapX.Core.Job;
using SnapX.Core.SharpCapture.Interfaces;

namespace SnapX.Core.SharpCapture;

public class CaptureFullscreen(IMainWindowService MainWindowService, INotificationService NotificationService, IUploadManager UploadManager, IDelayService DelayService, ILoggerService LoggerService, ICaptureService CaptureService) : CaptureBase(MainWindowService, NotificationService, UploadManager, DelayService, LoggerService, CaptureService)
{
    // protected override TaskMetadata Execute(TaskSettings? taskSettings)
    // {
    //     DebugHelper.WriteLine("CaptureFullscreen");
    //     var img = TaskHelpers.GetScreenshot(taskSettings).CaptureFullscreen();
    //     var metadata = CreateMetadata(img.Bounds);
    //     metadata.Image = img;
    //
    //     return metadata;
    // }
    protected override async Task<TaskMetadata> ExecuteAsync(TaskSettings taskSettings)
    {
        throw new NotImplementedException();
    }
}

