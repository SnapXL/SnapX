// SPDX-License-Identifier: GPL-3.0-or-later


using SnapX.Core.Interfaces;
using SnapX.Core.Job;
using SnapX.Core.SharpCapture.Interfaces;

namespace SnapX.Core.SharpCapture;
public class CaptureWindow(IMainWindowService MainWindowService, INotificationService NotificationService, IUploadManager UploadManager, IDelayService DelayService, ILoggerService LoggerService, ICaptureService CaptureService, IntPtr? WindowHandle = 0) : CaptureBase(MainWindowService, NotificationService, UploadManager, DelayService, LoggerService, CaptureService)
{
    public IntPtr WindowHandle { get; protected set; } = WindowHandle.GetValueOrDefault(IntPtr.Zero);

    // protected override TaskMetadata Execute(TaskSettings? taskSettings)
    // {
    //     WindowInfo windowInfo = new(WindowHandle);
    //
    //     if (windowInfo.IsMinimized)
    //     {
    //         windowInfo.Restore();
    //         Thread.Sleep(250);
    //     }
    //
    //     if (!windowInfo.IsActive)
    //     {
    //         windowInfo.Activate();
    //         Thread.Sleep(100);
    //     }
    //
    //     var metadata = new TaskMetadata();
    //     metadata.UpdateInfo(windowInfo);
    //
    //     if (taskSettings.CaptureSettings.CaptureTransparent && !taskSettings.CaptureSettings.CaptureClientArea)
    //     {
    //         metadata.Image = TaskHelpers.GetScreenshot(taskSettings).CaptureWindowTransparent(WindowHandle);
    //     }
    //     else
    //     {
    //         metadata.Image = TaskHelpers.GetScreenshot(taskSettings).CaptureWindow(WindowHandle);
    //     }
    //
    //     return metadata;
    // }
    protected override async Task<TaskMetadata> ExecuteAsync(TaskSettings taskSettings)
    {
        throw new NotImplementedException();
    }
}

