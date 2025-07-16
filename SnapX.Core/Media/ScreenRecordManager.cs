// SPDX-License-Identifier: GPL-3.0-or-later


namespace SnapX.Core.Media;

public static class ScreenRecordManager
{
    public static bool IsRecording { get; private set; }

    // private static ScreenRecorder screenRecorder;
    // private static ScreenRecordForm recordForm;


    public static void PauseScreenRecording()
    {
        throw new NotImplementedException("As with many things, this function is.... NOT IMPLEMENTED. (ScreenRecordManager.PauseScreenRecording)");
    }

    public static void AbortRecording()
    {
        throw new NotImplementedException("As with many things, this function is.... NOT IMPLEMENTED. (ScreenRecordManager.AbortRecording)");

    }

    // private static void StartRecording(ScreenRecordOutput outputType, TaskSettings taskSettings, ScreenRecordStartMethod startMethod = ScreenRecordStartMethod.Region)
    // {
    //     if (outputType == ScreenRecordOutput.GIF)
    //     {
    //         taskSettings.CaptureSettings.FFmpegOptions.VideoCodec = FFmpegVideoCodec.gif;
    //     }
    //
    //     if (taskSettings.CaptureSettings.FFmpegOptions.IsAnimatedImage)
    //     {
    //         taskSettings.CaptureSettings.ScreenRecordTwoPassEncoding = true;
    //     }
    //
    //     int fps;
    //
    //     if (taskSettings.CaptureSettings.FFmpegOptions.VideoCodec == FFmpegVideoCodec.gif)
    //     {
    //         fps = taskSettings.CaptureSettings.GIFFPS;
    //     }
    //     else
    //     {
    //         fps = taskSettings.CaptureSettings.ScreenRecordFPS;
    //     }
    //
    //     DebugHelper.WriteLine("Starting screen recording. Video encoder: \"{0}\", Audio encoder: \"{1}\", FPS: {2}",
    //         taskSettings.CaptureSettings.FFmpegOptions.VideoCodec.GetDescription(), taskSettings.CaptureSettings.FFmpegOptions.AudioCodec.GetDescription(), fps);
    //
    //     if (!TaskHelpers.CheckFFmpeg(taskSettings))
    //     {
    //         return;
    //     }
    //
    //     if (!taskSettings.CaptureSettings.FFmpegOptions.IsSourceSelected)
    //     {
    //         MessageBox.Show(Resources.FFmpeg_FFmpeg_video_and_audio_source_both_can_t_be__None__,
    //             "SnapX - " + Resources.FFmpeg_FFmpeg_error, MessageBoxButtons.OK, MessageBoxIcon.Warning);
    //         return;
    //     }
    //
    //     if (taskSettings.GeneralSettings.ToastWindowAutoHide)
    //     {
    //         NotificationForm.CloseActiveForm();
    //     }
    //
    //     Rectangle captureRectangle = Rectangle.Empty;
    //     TaskMetadata metadata = new TaskMetadata();
    //
    //     switch (startMethod)
    //     {
    //         case ScreenRecordStartMethod.Region:
    //             if (taskSettings.CaptureSettings.ScreenRecordTransparentRegion)
    //             {
    //                 RegionCaptureTasks.GetRectangleRegionTransparent(out captureRectangle);
    //             }
    //             else
    //             {
    //                 RegionCaptureTasks.GetRectangleRegion(out captureRectangle, out WindowInfo windowInfo, taskSettings.CaptureSettings.SurfaceOptions);
    //
    //                 metadata.UpdateInfo(windowInfo);
    //             }
    //             break;
    //         case ScreenRecordStartMethod.ActiveWindow:
    //             if (taskSettings.CaptureSettings.CaptureClientArea)
    //             {
    //                 captureRectangle = CaptureHelpers.GetActiveWindowClientRectangle();
    //             }
    //             else
    //             {
    //                 captureRectangle = CaptureHelpers.GetActiveWindowRectangle();
    //             }
    //
    //             IntPtr handle = NativeMethods.GetForegroundWindow();
    //             WindowInfo activeWindowInfo = new WindowInfo(handle);
    //             metadata.UpdateInfo(activeWindowInfo);
    //             break;
    //         case ScreenRecordStartMethod.CustomRegion:
    //             captureRectangle = taskSettings.CaptureSettings.CaptureCustomRegion;
    //             break;
    //         case ScreenRecordStartMethod.LastRegion:
    //             captureRectangle = SnapX.Settings.ScreenRecordRegion;
    //             break;
    //     }
    //
    //     Rectangle screenRectangle = CaptureHelpers.GetScreenBounds();
    //     captureRectangle = Rectangle.Intersect(captureRectangle, screenRectangle);
    //
    //     if (taskSettings.CaptureSettings.FFmpegOptions.IsEvenSizeRequired)
    //     {
    //         captureRectangle = CaptureHelpers.EvenRectangleSize(captureRectangle);
    //     }
    //
    //     if (IsRecording || !captureRectangle.IsValid() || screenRecorder != null)
    //     {
    //         return;
    //     }
    //
    //     SnapX.Settings.ScreenRecordRegion = captureRectangle;
    //
    //     IsRecording = true;
    //
    //     string path = "";
    //     string concatPath = "";
    //     string tempPath = "";
    //     bool abortRequested = false;
    //
    //     float duration = taskSettings.CaptureSettings.ScreenRecordFixedDuration ? taskSettings.CaptureSettings.ScreenRecordDuration : 0;
    //
    //     recordForm = new ScreenRecordForm(captureRectangle)
    //     {
    //         ActivateWindow = startMethod == ScreenRecordStartMethod.Region,
    //         Duration = duration,
    //         AskConfirmationOnAbort = taskSettings.CaptureSettings.ScreenRecordAskConfirmationOnAbort
    //     };
    //
    //     recordForm.StopRequested += StopRecording;
    //     recordForm.Show();
    //
    //     Task.Run(() =>
    //     {
    //         try
    //         {
    //             string extension;
    //             if (taskSettings.CaptureSettings.ScreenRecordTwoPassEncoding)
    //             {
    //                 extension = "mp4";
    //             }
    //             else
    //             {
    //                 extension = taskSettings.CaptureSettings.FFmpegOptions.Extension;
    //             }
    //             string screenshotsFolder = TaskHelpers.GetScreenshotsFolder(taskSettings, metadata);
    //             string fileName = TaskHelpers.GetFileName(taskSettings, extension, metadata);
    //             path = TaskHelpers.HandleExistsFile(screenshotsFolder, fileName, taskSettings);
    //
    //             if (string.IsNullOrEmpty(path))
    //             {
    //                 abortRequested = true;
    //             }
    //             else
    //             {
    //                 concatPath = File.AppendTextToFileName(path, "-concat");
    //                 File.DeleteFile(concatPath);
    //                 tempPath = File.AppendTextToFileName(path, "-temp");
    //                 File.DeleteFile(tempPath);
    //             }
    //
    //             while (!abortRequested && (recordForm.Status == ScreenRecordingStatus.Waiting || recordForm.Status == ScreenRecordingStatus.Paused))
    //             {
    //                 recordForm.ChangeState(ScreenRecordState.BeforeStart);
    //
    //                 if (recordForm.Status == ScreenRecordingStatus.Paused || !taskSettings.CaptureSettings.ScreenRecordAutoStart)
    //                 {
    //                     recordForm.RecordResetEvent.WaitOne();
    //                 }
    //                 else
    //                 {
    //                     int delay = (int)(taskSettings.CaptureSettings.ScreenRecordStartDelay * 1000);
    //
    //                     if (delay > 0)
    //                     {
    //                         recordForm.InvokeSafe(() => recordForm.StartCountdown(delay));
    //
    //                         recordForm.RecordResetEvent.WaitOne(delay);
    //                     }
    //                 }
    //
    //                 if (recordForm.Status == ScreenRecordingStatus.Aborted)
    //                 {
    //                     abortRequested = true;
    //                 }
    //
    //                 if (recordForm.Status == ScreenRecordingStatus.Waiting || recordForm.Status == ScreenRecordingStatus.Paused)
    //                 {
    //                     if (recordForm.Status == ScreenRecordingStatus.Paused && System.IO.File.Exists(path))
    //                     {
    //                         File.RenameFile(path, concatPath);
    //                     }
    //
    //                     recordForm.ChangeState(ScreenRecordState.AfterStart);
    //
    //                     captureRectangle = recordForm.RecordingRegion;
    //
    //                     ScreenRecordingOptions options = new ScreenRecordingOptions()
    //                     {
    //                         IsRecording = true,
    //                         IsLossless = taskSettings.CaptureSettings.ScreenRecordTwoPassEncoding,
    //                         FFmpeg = taskSettings.CaptureSettings.FFmpegOptions,
    //                         FPS = fps,
    //                         Duration = duration,
    //                         OutputPath = path,
    //                         CaptureArea = captureRectangle,
    //                         DrawCursor = taskSettings.CaptureSettings.ScreenRecordShowCursor
    //                     };
    //
    //                     Screenshot screenshot = TaskHelpers.GetScreenshot(taskSettings);
    //                     screenshot.CaptureCursor = taskSettings.CaptureSettings.ScreenRecordShowCursor;
    //
    //                     screenRecorder?.Dispose();
    //                     screenRecorder = new ScreenRecorder(ScreenRecordOutput.FFmpeg, options, screenshot, captureRectangle);
    //                     screenRecorder.RecordingStarted += ScreenRecorder_RecordingStarted;
    //                     screenRecorder.EncodingProgressChanged += ScreenRecorder_EncodingProgressChanged;
    //                     screenRecorder.StartRecording();
    //                     recordForm.ChangeState(ScreenRecordState.RecordingEnd);
    //
    //                     if (recordForm.Status == ScreenRecordingStatus.Aborted)
    //                     {
    //                         abortRequested = true;
    //                     }
    //                 }
    //
    //                 TaskHelpers.PlayNotificationSoundAsync(NotificationSound.ActionCompleted, taskSettings);
    //
    //                 if (System.IO.File.Exists(concatPath))
    //                 {
    //                     using (FFmpegCLIManager ffmpeg = new FFmpegCLIManager(taskSettings.CaptureSettings.FFmpegOptions.FFmpegPath))
    //                     {
    //                         ffmpeg.ShowError = true;
    //                         ffmpeg.ConcatenateVideos(new string[] { concatPath, path }, tempPath, true);
    //                         FileHelpers.RenameFile(tempPath, path);
    //                     }
    //                 }
    //             }
    //         }
    //         catch (Exception e)
    //         {
    //             DebugHelper.WriteException(e);
    //         }
    //
    //         if (taskSettings.CaptureSettings.ScreenRecordTwoPassEncoding && !abortRequested && screenRecorder != null && System.IO.File.Exists(path))
    //         {
    //             recordForm.ChangeState(ScreenRecordState.Encoding);
    //
    //             path = ProcessTwoPassEncoding(path, metadata, taskSettings);
    //         }
    //
    //         if (recordForm != null)
    //         {
    //             recordForm.InvokeSafe(() =>
    //             {
    //                 recordForm.Close();
    //                 recordForm.Dispose();
    //                 recordForm = null;
    //             });
    //         }
    //
    //         if (screenRecorder != null)
    //         {
    //             screenRecorder.Dispose();
    //             screenRecorder = null;
    //         }
    //
    //         if (abortRequested)
    //         {
    //             FileHelpers.DeleteFile(path);
    //         }
    //
    //         FileHelpers.DeleteFile(concatPath);
    //         FileHelpers.DeleteFile(tempPath);
    //     }).ContinueInCurrentContext(() =>
    //     {
    //         if (!abortRequested && !string.IsNullOrEmpty(path) && System.IO.File.Exists(path) && TaskHelpers.ShowAfterCaptureForm(taskSettings, out string customFileName, null, path))
    //         {
    //             if (!string.IsNullOrEmpty(customFileName))
    //             {
    //                 string currentFileName = Path.GetFileNameWithoutExtension(path);
    //                 string ext = Path.GetExtension(path);
    //
    //                 if (!currentFileName.Equals(customFileName, StringComparison.OrdinalIgnoreCase))
    //                 {
    //                     path = FileHelpers.RenameFile(path, customFileName + ext);
    //                 }
    //             }
    //
    //             WorkerTask task = WorkerTask.CreateFileJobTask(path, metadata, taskSettings, customFileName);
    //             TaskManager.Start(task);
    //         }
    //
    //         abortRequested = false;
    //         IsRecording = false;
    //     });
    // }

    private static void ScreenRecorder_RecordingStarted()
    {
        // recordForm.ChangeState(ScreenRecordState.AfterRecordingStart);
    }

    private static void ScreenRecorder_EncodingProgressChanged(int progress)
    {
        // recordForm.ChangeStateProgress(progress);
    }
    //
    // private static string ProcessTwoPassEncoding(string input, TaskMetadata metadata, TaskSettings taskSettings, bool deleteInputFile = true)
    // {
    //     string screenshotsFolder = TaskHelpers.GetScreenshotsFolder(taskSettings, metadata);
    //     string fileName = TaskHelpers.GetFileName(taskSettings, taskSettings.CaptureSettings.FFmpegOptions.Extension, metadata);
    //     string output = Path.Combine(screenshotsFolder, fileName);
    //
    //     try
    //     {
    //         if (taskSettings.CaptureSettings.FFmpegOptions.VideoCodec == FFmpegVideoCodec.gif)
    //         {
    //             screenRecorder.FFmpegEncodeAsGIF(input, output);
    //         }
    //         else
    //         {
    //             screenRecorder.FFmpegEncodeVideo(input, output);
    //         }
    //     }
    //     finally
    //     {
    //         if (deleteInputFile && !input.Equals(output, StringComparison.OrdinalIgnoreCase) && System.IO.File.Exists(input))
    //         {
    //             System.IO.File.Delete(input);
    //         }
    //     }
    //
    //     return output;
    // }
}

