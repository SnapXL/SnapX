
// SPDX-License-Identifier: GPL-3.0-or-later


using SixLabors.ImageSharp;
using SnapX.Core.Job;
using SnapX.Core.Upload;
using SnapX.Core.Utils.Extensions;
using SnapX.Core.Utils.Native;

namespace SnapX.Core.Capture;
public abstract class CaptureBase
{
    public bool AllowAutoHideForm { get; set; } = true;
    public bool AllowAnnotation { get; set; } = true;

    public void Capture(bool autoHideForm)
    {
        Capture(null, autoHideForm);
    }

    public void Capture(TaskSettings taskSettings = null, bool autoHideForm = false)
    {
        if (taskSettings == null) taskSettings = TaskSettings.GetDefaultTaskSettings();

        // TODO: Reimplement taskSettings.GeneralSettings.ToastWindowAutoHide
        // if (taskSettings.GeneralSettings.ToastWindowAutoHide)
        // {
        //     NotificationForm.CloseActiveForm();
        // }

        if (taskSettings.CaptureSettings.ScreenshotDelay > 0)
        {
            int delay = (int)(taskSettings.CaptureSettings.ScreenshotDelay * 1000);

            Task.Delay(delay).ContinueInCurrentContext(() =>
            {
                CaptureInternal(taskSettings, autoHideForm);
            });
        }
        else
        {
            CaptureInternal(taskSettings, autoHideForm);
        }
    }

    protected abstract TaskMetadata Execute(TaskSettings taskSettings);

    private void CaptureInternal(TaskSettings taskSettings, bool autoHideForm)
    {
        if (autoHideForm && AllowAutoHideForm)
        {
            // SnapX.MainWindow.Hide();
            // Thread.Sleep(250);
        }

        TaskMetadata metadata = null;

        try
        {
            AllowAnnotation = true;
            metadata = Execute(taskSettings);
        }
        catch (Exception ex)
        {
            DebugHelper.WriteException(ex);
        }
        finally
        {
            if (autoHideForm && AllowAutoHideForm)
            {
                // SnapX.MainWindow.ForceActivate();
            }

            AfterCapture(metadata, taskSettings);
        }
    }

    private void AfterCapture(TaskMetadata metadata, TaskSettings taskSettings)
    {
        if (metadata != null && metadata.Image != null)
        {
            // TaskHelpers.PlayNotificationSoundAsync(NotificationSound.Capture, taskSettings);

            if (taskSettings.AfterCaptureJob.HasFlag(AfterCaptureTasks.AnnotateImage) && !AllowAnnotation)
            {
                taskSettings.AfterCaptureJob = taskSettings.AfterCaptureJob.Remove(AfterCaptureTasks.AnnotateImage);
            }

            if (taskSettings.ImageSettings.ImageEffectOnlyRegionCapture &&
                GetType() != typeof(CaptureRegion) && GetType() != typeof(CaptureLastRegion))
            {
                taskSettings.AfterCaptureJob = taskSettings.AfterCaptureJob.Remove(AfterCaptureTasks.AddImageEffects);
            }

            UploadManager.RunImageTask(metadata, taskSettings);
        }
    }

    protected TaskMetadata CreateMetadata()
    {
        return CreateMetadata(Rectangle.Empty, null);
    }

    protected TaskMetadata CreateMetadata(Rectangle insideRect)
    {
        return CreateMetadata(insideRect, "explorer");
    }

    protected TaskMetadata CreateMetadata(Rectangle insideRect, string ignoreProcess)
    {
        var metadata = new TaskMetadata();

        var windowInfo = Methods.GetForegroundWindow();
        if ((ignoreProcess == null || !windowInfo.ProcessName.Equals(ignoreProcess, StringComparison.OrdinalIgnoreCase)) &&
            (insideRect.IsEmpty || windowInfo.Rectangle.Contains(insideRect)))
        {
            metadata.UpdateInfo(windowInfo);
        }

        return metadata;
    }
}

