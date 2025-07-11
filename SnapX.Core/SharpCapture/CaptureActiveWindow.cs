
// SPDX-License-Identifier: GPL-3.0-or-later


using SnapX.Core.Interfaces;
using SnapX.Core.Job;
using SnapX.Core.SharpCapture.Interfaces;

namespace SnapX.Core.SharpCapture;
public class CaptureActiveWindow(
    IMainWindowService MainWindowService,
    INotificationService NotificationService,
    IUploadManager UploadManager,
    IDelayService DelayService,
    ILoggerService LoggerService,
    ICaptureService CaptureService)
    : CaptureBase(MainWindowService, NotificationService, UploadManager, DelayService, LoggerService, CaptureService)
{

    protected override async Task<TaskMetadata> ExecuteAsync(TaskSettings taskSettings)
    {
        var metadata = CreateMetadata();

        if (taskSettings.CaptureSettings is { CaptureTransparent: true, CaptureClientArea: false })
        {
            LoggerService.Debug("Capture transparent mode is default now. Non transparent mode is not implemented");
        }

        metadata.Image = await _captureService.CaptureActiveWindowAsync();

        return metadata;
    }
}
