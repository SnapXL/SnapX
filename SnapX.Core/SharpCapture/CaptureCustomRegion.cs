// SPDX-License-Identifier: GPL-3.0-or-later


using SnapX.Core.Interfaces;
using SnapX.Core.Job;
using SnapX.Core.SharpCapture.Interfaces;

namespace SnapX.Core.SharpCapture;

public class CaptureCustomRegion(IMainWindowService MainWindowService, INotificationService NotificationService, IUploadManager UploadManager, IDelayService DelayService, ILoggerService LoggerService, ICaptureService CaptureService) : CaptureBase(MainWindowService, NotificationService, UploadManager, DelayService, LoggerService, CaptureService)
{
    protected override async Task<TaskMetadata> ExecuteAsync(TaskSettings taskSettings)
    {
        var rect = taskSettings.CaptureSettings.CaptureCustomRegion;
        var metadata = CreateMetadata(rect);
        metadata.Image = await _captureService.CaptureRectangle(taskSettings, rect);
        return metadata;
    }
}

