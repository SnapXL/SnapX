
// SPDX-License-Identifier: GPL-3.0-or-later


using SnapX.Core.Interfaces;
using SnapX.Core.Job;
using SnapX.Core.SharpCapture.Interfaces;

namespace SnapX.Core.SharpCapture;
public class CaptureRegion(IMainWindowService MainWindowService, INotificationService NotificationService, IUploadManager UploadManager, IDelayService DelayService, ILoggerService LoggerService, ICaptureService CaptureService, RegionCaptureType RegionCaptureType) : CaptureBase(MainWindowService, NotificationService, UploadManager, DelayService, LoggerService, CaptureService)
{
    protected static RegionCaptureType lastRegionCaptureType = RegionCaptureType.Default;

    public RegionCaptureType RegionCaptureType { get; protected set; } = RegionCaptureType;

    // protected override TaskMetadata Execute(TaskSettings taskSettings)
    // {
    //     switch (RegionCaptureType)
    //     {
    //         default:
    //         case RegionCaptureType.Default:
    //             return ExecuteRegionCapture(taskSettings);
    //         case RegionCaptureType.Light:
    //             return ExecuteRegionCaptureLight(taskSettings);
    //         case RegionCaptureType.Transparent:
    //             return ExecuteRegionCaptureTransparent(taskSettings);
    //     }
    // }

    protected TaskMetadata ExecuteRegionCapture(TaskSettings taskSettings)
    {
        // Bitmap canvas;
        // var screenshot = TaskHelpers.GetScreenshot(taskSettings);
        // screenshot.CaptureCursor = false;
        //
        // canvas = screenshot.CaptureFullscreen();
        //
        // CursorData cursorData = null;
        //
        // if (taskSettings.CaptureSettings.ShowCursor)
        // {
        //     cursorData = new CursorData();
        // }
        //
        // using (RegionCaptureForm form = new RegionCaptureForm(mode, taskSettings.CaptureSettingsReference.SurfaceOptions, canvas))
        // {
        //     if (cursorData != null && cursorData.IsVisible)
        //     {
        //         form.AddCursor(cursorData.ToBitmap(), form.PointToClient(cursorData.DrawPosition));
        //     }
        //
        //     form.ShowDialog();
        //
        //     Bitmap result = form.GetResultImage();
        //
        //     if (result != null)
        //     {
        //         TaskMetadata metadata = new TaskMetadata(result);
        //
        //         if (form.IsImageModified)
        //         {
        //             AllowAnnotation = false;
        //         }
        //
        //         if (form.Result == RegionResult.Region)
        //         {
        //             WindowInfo windowInfo = form.GetWindowInfo();
        //             metadata.UpdateInfo(windowInfo);
        //         }
        //
        //         lastRegionCaptureType = RegionCaptureType.Default;
        //
        //         return metadata;
        //     }
        // }

        return null;
    }

    protected TaskMetadata ExecuteRegionCaptureLight(TaskSettings taskSettings)
    {
        // Bitmap canvas;
        // Screenshot screenshot = TaskHelpers.GetScreenshot(taskSettings);
        //
        // if (taskSettings.CaptureSettings.SurfaceOptions.ActiveMonitorMode)
        // {
        //     canvas = screenshot.CaptureActiveMonitor();
        // }
        // else
        // {
        //     canvas = screenshot.CaptureFullscreen();
        // }
        //
        // bool activeMonitorMode = taskSettings.CaptureSettings.SurfaceOptions.ActiveMonitorMode;
        //
        // using (RegionCaptureLightForm rectangleLight = new RegionCaptureLightForm(canvas, activeMonitorMode))
        // {
        //     if (rectangleLight.ShowDialog() == DialogResult.OK)
        //     {
        //         Bitmap result = rectangleLight.GetAreaImage();
        //
        //         if (result != null)
        //         {
        //             lastRegionCaptureType = RegionCaptureType.Light;
        //
        //             return new TaskMetadata(result);
        //         }
        //     }
        // }

        return null;
    }

    protected TaskMetadata ExecuteRegionCaptureTransparent(TaskSettings taskSettings)
    {
        // bool activeMonitorMode = taskSettings.CaptureSettings.SurfaceOptions.ActiveMonitorMode;
        //
        // using (RegionCaptureTransparentForm rectangleTransparent = new RegionCaptureTransparentForm(activeMonitorMode))
        // {
        //     if (rectangleTransparent.ShowDialog() == DialogResult.OK)
        //     {
        //         Screenshot screenshot = TaskHelpers.GetScreenshot(taskSettings);
        //         Bitmap result = rectangleTransparent.GetAreaImage(screenshot);
        //
        //         if (result != null)
        //         {
        //             lastRegionCaptureType = RegionCaptureType.Transparent;
        //
        //             return new TaskMetadata(result);
        //         }
        //     }
        // }

        return null;
    }

    protected override async Task<TaskMetadata> ExecuteAsync(TaskSettings taskSettings)
    {
        throw new NotImplementedException();
    }
}

