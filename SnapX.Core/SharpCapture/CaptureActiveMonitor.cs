// SPDX-License-Identifier: GPL-3.0-or-later


using SnapX.Core.Interfaces;
using SnapX.Core.Job;
using SnapX.Core.SharpCapture.Interfaces;

namespace SnapX.Core.SharpCapture;

public class CaptureActiveMonitor(
    IMainWindowService MainWindowService,
    INotificationService NotificationService,
    IUploadManager UploadManager,
    IDelayService DelayService,
    ILoggerService _logger,
    ICaptureService CaptureService)
    : CaptureBase(MainWindowService, NotificationService, UploadManager, DelayService, _logger, CaptureService)
{
    protected override async Task<TaskMetadata> ExecuteAsync(TaskSettings taskSettings)
    {
        _logger.Debug("CaptureActiveMonitor started");

        var img = await _captureService
            .CaptureActiveMonitorAsync(taskSettings)
            .ConfigureAwait(false);
        if (img is null)
        {
            _logger.Debug("CaptureActiveMonitorAsync returned null. Returning empty TaskMetadata");
            return new TaskMetadata();
        }
        var metadata = CreateMetadata(img.Bounds);
        metadata.Image = img;
        return metadata;
    }
}
