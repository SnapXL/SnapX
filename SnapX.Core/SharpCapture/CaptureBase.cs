
// SPDX-License-Identifier: GPL-3.0-or-later


using SixLabors.ImageSharp;
using SnapX.Core.Interfaces;
using SnapX.Core.Job;
using SnapX.Core.SharpCapture.Interfaces;
using SnapX.Core.Utils.Extensions;
using SnapX.Core.Utils.Native;

namespace SnapX.Core.SharpCapture;
public abstract class CaptureBase(
    IMainWindowService MainWindowService,
    INotificationService NotificationService,
    IUploadManager UploadManager,
    IDelayService DelayService,
    ILoggerService LoggerService,
    ICaptureService CaptureService)
{
    public bool AllowAutoHideForm { get; set; } = true;
    public bool AllowAnnotation { get; set; } = true;

    internal readonly ICaptureService _captureService = CaptureService;

    public async Task CaptureAsync(TaskSettings? taskSettings = null, bool autoHideForm = false)
    {
        using (LoggerService.BeginScope("Capture"))
        {
            if (taskSettings == null)
            {
                taskSettings = TaskSettings.GetDefaultTaskSettings();
                LoggerService.Debug("Loaded default task settings");
            }

            if (taskSettings.CaptureSettings.ScreenshotDelay > 0)
            {
                var delay = (int)(taskSettings.CaptureSettings.ScreenshotDelay * 1000);
                LoggerService.Information("Delaying capture for {Delay} ms as configured", delay);
                await DelayService.DelayAsync(delay);
            }

            await CaptureInternalAsync(taskSettings, autoHideForm);
        }
    }

    private async Task CaptureInternalAsync(TaskSettings taskSettings, bool autoHideForm)
    {
        using (LoggerService.BeginScope("CaptureInternal"))
        {
            if (autoHideForm && AllowAutoHideForm)
            {
                LoggerService.Debug("Hiding main window for capture");
                await MainWindowService.HideAsync();
                await DelayService.DelayAsync(250);
            }

            TaskMetadata? metadata = null;

            try
            {
                AllowAnnotation = true;
                LoggerService.Debug("Executing capture logic in {CaptureType}", GetType().Name);
                metadata = await ExecuteAsync(taskSettings);
            }
            catch (Exception ex)
            {
                LoggerService.Error(ex, "An error occurred during capture");
            }
            finally
            {
                if (autoHideForm && AllowAutoHideForm)
                {
                    LoggerService.Debug("Re-activating main window after capture");
                    await MainWindowService.ForceActivateAsync();
                }

                await AfterCaptureAsync(metadata, taskSettings);
            }
        }
    }

    protected virtual async Task AfterCaptureAsync(TaskMetadata? metadata, TaskSettings taskSettings)
    {
        using (LoggerService.BeginScope("AfterCapture"))
        {
            if (metadata is { Image: not null })
            {
                LoggerService.Debug("Running AfterCapture tasks");

                await NotificationService
                    .PlayNotificationSoundAsync(NotificationSound.Capture, taskSettings);

                if (taskSettings.AfterCaptureJob.HasFlag(AfterCaptureTasks.AnnotateImage) && !AllowAnnotation)
                {
                    taskSettings.AfterCaptureJob =
                        taskSettings.AfterCaptureJob.Remove(AfterCaptureTasks.AnnotateImage);

                    LoggerService.Information("Annotation disabled. Removed AnnotateImage from AfterCapture tasks");
                }

                if (taskSettings.ImageSettings.ImageEffectOnlyRegionCapture &&
                    GetType() != typeof(CaptureRegion) && GetType() != typeof(CaptureLastRegion))
                {
                    taskSettings.AfterCaptureJob =
                        taskSettings.AfterCaptureJob.Remove(AfterCaptureTasks.AddImageEffects);

                    LoggerService.Information("Skipped image effects because ImageEffectOnlyRegionCapture is true");
                }

                await UploadManager.RunImageTaskAsync(metadata, taskSettings);
                LoggerService.Information("Upload service completed image processing");
            }
            else
            {
                LoggerService.Warning("No image captured. Skipping AfterCapture tasks");
            }
        }
    }

    protected abstract Task<TaskMetadata> ExecuteAsync(TaskSettings taskSettings);

    protected TaskMetadata CreateMetadata()
    {
        return CreateMetadata(Rectangle.Empty, null);
    }

    protected TaskMetadata CreateMetadata(Rectangle insideRect, string? ignoreProcess = "explorer")
    {
        var metadata = new TaskMetadata();

        var windowInfo = Methods.GetForegroundWindow();
        if ((ignoreProcess == null || !windowInfo.ProcessName.Equals(ignoreProcess, StringComparison.OrdinalIgnoreCase)) &&
            (insideRect.IsEmpty || windowInfo.Rectangle.Contains(insideRect)))
        {
            metadata.UpdateInfo(windowInfo);
            LoggerService.Debug("Foreground window info added to capture metadata: {ProcessName}", windowInfo.ProcessName);
        }
        else
        {
            LoggerService.Debug("Window info skipped because it didn't match criteria. IgnoreProcess={IgnoreProcess}", ignoreProcess ?? string.Empty);
        }

        return metadata;
    }
}
