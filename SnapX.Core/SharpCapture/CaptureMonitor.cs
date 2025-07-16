// SPDX-License-Identifier: GPL-3.0-or-later


using SixLabors.ImageSharp;
using SnapX.Core.Interfaces;
using SnapX.Core.Job;
using SnapX.Core.SharpCapture.Interfaces;

namespace SnapX.Core.SharpCapture;
public class CaptureMonitor(IMainWindowService MainWindowService, INotificationService NotificationService, IUploadManager UploadManager, IDelayService DelayService, ILoggerService LoggerService, ICaptureService CaptureService, Rectangle MonitorRectangle) : CaptureBase(MainWindowService, NotificationService, UploadManager, DelayService, LoggerService, CaptureService)
{
    public Rectangle MonitorRectangle { get; private set; } = MonitorRectangle;


    // protected override TaskMetadata Execute(TaskSettings taskSettings)
    // {
    //     DebugHelper.WriteLine("CaptureMonitor Start");
    //     var metadata = CreateMetadata(MonitorRectangle);
    //     metadata.Image = TaskHelpers.GetScreenshot().CaptureRectangle(MonitorRectangle);
    //     return metadata;
    // }
    protected override async Task<TaskMetadata> ExecuteAsync(TaskSettings taskSettings)
    {
        throw new NotImplementedException();
    }
}

