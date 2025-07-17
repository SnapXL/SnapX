// SPDX-License-Identifier: GPL-3.0-or-later


using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
using SixLabors.ImageSharp;
using SnapX.Core.Hotkey;
using SnapX.Core.Upload;
using SnapX.Core.Upload.BaseServices;
using SnapX.Core.Upload.BaseUploaders;
using SnapX.Core.Upload.Utils;
using SnapX.Core.Utils;
using SnapX.Core.Utils.Extensions;
using SnapX.Core.Utils.Miscellaneous;
using SnapX.Core.Utils.Native;
using Math = System.Math;

namespace SnapX.Core.Job;

// TODO: Refactor WorkerTask to reduce cognitive complexity
// Congitive complexity hits as high as 350% god damn.

public class WorkerTask : IDisposable
{
    public delegate void TaskEventHandler(WorkerTask task);
    public delegate void TaskImageEventHandler(WorkerTask task, Image image);
    public delegate void UploaderServiceEventHandler(IUploaderService uploaderService);

    public event TaskEventHandler StatusChanged, UploadStarted, UploadProgressChanged, UploadCompleted, TaskCompleted;
    public event TaskImageEventHandler ImageReady;
    public event UploaderServiceEventHandler UploadersConfigWindowRequested;

    public TaskInfo Info { get; private set; }
    public TaskStatus Status { get; private set; }
    public bool IsBusy => Status == TaskStatus.InQueue || IsWorking;
    public bool IsWorking => Status == TaskStatus.Preparing || Status == TaskStatus.Working || Status == TaskStatus.Stopping;
    public bool StopRequested { get; private set; }
    public bool RequestSettingUpdate { get; private set; }
    public bool EarlyURLCopied { get; private set; }
    public Stream? Data { get; private set; }
    public Image? Image { get; private set; }
    public bool KeepImage { get; set; }
    public string? Text { get; private set; }

    private ThreadWorker threadWorker;
    private GenericUploader? uploader;
    private TaskReferenceHelper taskReferenceHelper;

    #region Constructors

    private WorkerTask(TaskSettings? taskSettings)
    {
        Status = TaskStatus.InQueue;
        Info = new TaskInfo(taskSettings);
    }

    public static WorkerTask CreateDataUploaderTask(EDataType dataType, Stream stream, string? fileName, TaskSettings? taskSettings)
    {
        WorkerTask task = new WorkerTask(taskSettings);
        task.Info.Job = TaskJob.DataUpload;
        task.Info.DataType = dataType;
        task.Info.FileName = fileName;
        task.Data = stream;
        return task;
    }

    public static WorkerTask CreateFileUploaderTask(string? filePath, TaskSettings? taskSettings)
    {
        WorkerTask task = new WorkerTask(taskSettings);
        task.Info.FilePath = filePath;
        task.Info.DataType = TaskHelpers.FindDataType(task.Info.FilePath, taskSettings);

        if (task.Info.TaskSettings.UploadSettings.FileUploadUseNamePattern)
        {
            string ext = FileHelpers.GetFileNameExtension(task.Info.FilePath);
            task.Info.FileName = TaskHelpers.GetFileName(task.Info.TaskSettings, ext);
        }

        if (task.Info.TaskSettings.AdvancedSettings.ProcessImagesDuringFileUpload && task.Info.DataType == EDataType.Image)
        {
            task.Info.Job = TaskJob.Job;
            task.Image = Image.Load(task.Info.FilePath);
        }
        else
        {
            task.Info.Job = TaskJob.FileUpload;

            if (!task.LoadFileStream())
            {
                DebugHelper.WriteException("Failed to load uploader file. The file may be in use or corrupt garbage.");
                return null;
            }
        }

        return task;
    }

    public static WorkerTask CreateImageUploaderTask(TaskMetadata metadata, TaskSettings? taskSettings, string customFileName = null)
    {
        WorkerTask task = new WorkerTask(taskSettings);
        task.Info.Job = TaskJob.Job;
        task.Info.DataType = EDataType.Image;

        if (!string.IsNullOrEmpty(customFileName))
        {
            task.Info.FileName = FileHelpers.AppendExtension(customFileName, "bmp");
        }
        else
        {
            task.Info.FileName = TaskHelpers.GetFileName(taskSettings, "bmp", metadata);
        }

        task.Info.Metadata = metadata;
        task.Image = metadata.Image;
        return task;
    }

    public static WorkerTask CreateTextUploaderTask(string? text, TaskSettings? taskSettings)
    {
        WorkerTask task = new WorkerTask(taskSettings);
        task.Info.Job = TaskJob.TextUpload;
        task.Info.DataType = EDataType.Text;
        task.Info.FileName = TaskHelpers.GetFileName(taskSettings, taskSettings.AdvancedSettings.TextFileExtension);
        task.Text = text;
        return task;
    }

    public static WorkerTask CreateURLShortenerTask(string? url, TaskSettings? taskSettings)
    {
        WorkerTask task = new WorkerTask(taskSettings);
        task.Info.Job = TaskJob.ShortenURL;
        task.Info.DataType = EDataType.URL;
        task.Info.FileName = string.Format("Shorten URL ({0})", taskSettings.URLShortenerDestination.GetLocalizedDescription());
        task.Info.Result.URL = url;
        return task;
    }

    public static WorkerTask CreateShareURLTask(string? url, TaskSettings? taskSettings)
    {
        WorkerTask task = new WorkerTask(taskSettings);
        task.Info.Job = TaskJob.ShareURL;
        task.Info.DataType = EDataType.URL;
        task.Info.FileName = string.Format("Share URL ({0})", taskSettings.URLSharingServiceDestination.GetLocalizedDescription());
        task.Info.Result.URL = url;
        return task;
    }

    public static WorkerTask CreateFileJobTask(string? filePath, TaskMetadata metadata, TaskSettings? taskSettings, string customFileName = null)
    {
        var task = new WorkerTask(taskSettings);
        task.Info.FilePath = filePath;
        task.Info.DataType = TaskHelpers.FindDataType(task.Info.FilePath, taskSettings);

        if (!string.IsNullOrEmpty(customFileName))
        {
            string ext = FileHelpers.GetFileNameExtension(task.Info.FilePath);
            task.Info.FileName = FileHelpers.AppendExtension(customFileName, ext);
        }
        else if (task.Info.TaskSettings.UploadSettings.FileUploadUseNamePattern)
        {
            string ext = FileHelpers.GetFileNameExtension(task.Info.FilePath);
            task.Info.FileName = TaskHelpers.GetFileName(task.Info.TaskSettings, ext);
        }

        task.Info.Metadata = metadata;
        task.Info.Job = TaskJob.Job;

        if (task.Info.IsUploadJob && !task.LoadFileStream())
        {
            return null;
        }

        return task;
    }

    public static WorkerTask CreateDownloadTask(string? url, bool upload, TaskSettings? taskSettings)
    {
        WorkerTask task = new WorkerTask(taskSettings);
        task.Info.Job = upload ? TaskJob.DownloadUpload : TaskJob.Download;

        string? fileName = URLHelpers.URLDecode(url, 10);
        fileName = URLHelpers.GetFileName(fileName);
        fileName = FileHelpers.SanitizeFileName(fileName);

        if (task.Info.TaskSettings.UploadSettings.FileUploadUseNamePattern)
        {
            string ext = FileHelpers.GetFileNameExtension(fileName);
            fileName = TaskHelpers.GetFileName(task.Info.TaskSettings, ext);
        }

        if (string.IsNullOrEmpty(fileName))
        {
            return null;
        }

        task.Info.FileName = fileName;
        task.Info.DataType = TaskHelpers.FindDataType(task.Info.FileName, taskSettings);
        task.Info.Result.URL = url;
        return task;
    }

    #endregion Constructors

    public void Start()
    {
        if (Status == TaskStatus.InQueue && !StopRequested)
        {
            Info.TaskStartTime = DateTime.Now;

            threadWorker = new ThreadWorker();
            Prepare();
            threadWorker.DoWork += ThreadDoWork;
            threadWorker.Completed += ThreadCompleted;
            threadWorker.Start();
        }
    }

    private void Prepare()
    {
        Status = TaskStatus.Preparing;

        switch (Info.Job)
        {
            case TaskJob.Job:
            case TaskJob.TextUpload:
                Info.Status = "Preparing";
                break;
            default:
                Info.Status = "Starting";
                break;
        }

        OnStatusChanged();
    }

    public void Stop()
    {
        StopRequested = true;

        switch (Status)
        {
            case TaskStatus.InQueue:
                OnTaskCompleted();
                break;
            case TaskStatus.Preparing:
            case TaskStatus.Working:
                if (uploader != null) uploader.StopUpload();
                Status = TaskStatus.Stopping;
                Info.Status = "Stopping";
                OnStatusChanged();
                break;
        }
    }

    private void ThreadDoWork()
    {
        CreateTaskReferenceHelper();

        try
        {
            StopRequested = !DoThreadJob();

            OnImageReady();

            if (!StopRequested)
            {
                if (Info.IsUploadJob && TaskHelpers.IsUploadAllowed())
                {
                    DoUploadJob();
                }
                else
                {
                    Info.Result.IsURLExpected = false;
                }
            }
        }
        finally
        {
            KeepImage = Image != null && Info.TaskSettings.GeneralSettings.ShowToastNotificationAfterTaskCompleted;

            Dispose();

            if (EarlyURLCopied && (StopRequested || Info.Result == null || string.IsNullOrEmpty(Info.Result.URL)) && Clipboard.ContainsText())
            {
                Clipboard.Clear();
            }

            if ((Info.Job == TaskJob.Job || (Info.Job == TaskJob.FileUpload && (Info.TaskSettings?.AdvancedSettings.UseAfterCaptureTasksDuringFileUpload ?? true)))
                && Info.TaskSettings.AfterCaptureJob.HasFlag(AfterCaptureTasks.DeleteFile) && !string.IsNullOrEmpty(Info.FilePath) && File.Exists(Info.FilePath))
            {
                File.Delete(Info.FilePath);
            }
        }

        if (!StopRequested && Info.Result != null && Info.Result.IsURLExpected && !Info.Result.IsError)
        {
            if (string.IsNullOrEmpty(Info.Result.URL))
            {
                AddErrorMessage("Result.URL is empty");
            }
            else
            {
                DoAfterUploadJobs();
            }
        }
    }

    private void CreateTaskReferenceHelper()
    {
        taskReferenceHelper = new TaskReferenceHelper()
        {
            DataType = Info.DataType,
            OverrideFTP = Info.TaskSettings.OverrideFTP,
            FTPIndex = Info.TaskSettings.FTPIndex,
            OverrideCustomUploader = Info.TaskSettings.OverrideCustomUploader,
            CustomUploaderIndex = Info.TaskSettings.CustomUploaderIndex,
            TextFormat = Info.TaskSettings.AdvancedSettings.TextFormat
        };
    }

    private void DoUploadJob()
    {
        if (SnapX.Settings.ShowUploadWarning)
        {
            bool disableUpload = false;

            SnapX.Settings.ShowUploadWarning = false;

            if (disableUpload)
            {
                SnapX.DefaultTaskSettings.AfterCaptureJob = SnapX.DefaultTaskSettings.AfterCaptureJob.Remove(AfterCaptureTasks.UploadImageToHost);

                foreach (HotkeySettings hotkeySettings in SnapX.HotkeysConfig.Hotkeys)
                {
                    if (hotkeySettings.TaskSettings != null)
                    {
                        hotkeySettings.TaskSettings.AfterCaptureJob = hotkeySettings.TaskSettings.AfterCaptureJob.Remove(AfterCaptureTasks.UploadImageToHost);
                    }
                }

                Info.TaskSettings.AfterCaptureJob = Info.TaskSettings.AfterCaptureJob.Remove(AfterCaptureTasks.UploadImageToHost);
                Info.Result.IsURLExpected = false;
                RequestSettingUpdate = true;

                return;
            }
        }

        if (SnapX.Settings.ShowLargeFileSizeWarning > 0)
        {
            long dataSize = SnapX.Settings.BinaryUnits ? SnapX.Settings.ShowLargeFileSizeWarning * 1024 * 1024 : SnapX.Settings.ShowLargeFileSizeWarning * 1000 * 1000;
            if (Data != null && Data.Length > dataSize)
            {
                throw new NotImplementedException("LargeFileSizeWarning");
            }
        }

        if (!StopRequested)
        {
            SettingManager.WaitUploadersConfig();

            Status = TaskStatus.Working;
            Info.Status = "Uploading";


            bool cancelUpload = false;

            if (Info.TaskSettings.AfterCaptureJob.HasFlag(AfterCaptureTasks.ShowBeforeUploadWindow))
            {
                throw new NotImplementedException("ShowBeforeUploadWindow");
            }

            if (!cancelUpload)
            {
                OnUploadStarted();

                bool isError = DoUpload(Data, Info.FileName);

                if (isError && SnapX.Settings.MaxUploadFailRetry > 0)
                {
                    for (int retry = 1; !StopRequested && isError && retry <= SnapX.Settings.MaxUploadFailRetry; retry++)
                    {
                        DebugHelper.WriteLine("Upload failed. Retrying upload.");
                        isError = DoUpload(Data, Info.FileName, retry);
                    }
                }

                if (!isError)
                {
                    OnUploadCompleted();
                }
            }
            else
            {
                Info.Result.IsURLExpected = false;
            }
        }
    }

    private bool DoUpload(Stream data, string? fileName, int retry = 0)
    {
        bool isError = false;

        if (retry > 0)
        {
            if (SnapX.Settings.UseSecondaryUploaders)
            {
                Info.TaskSettings.ImageDestination = SnapX.Settings.SecondaryImageUploaders[retry - 1];
                Info.TaskSettings.ImageFileDestination = SnapX.Settings.SecondaryFileUploaders[retry - 1];
                Info.TaskSettings.TextDestination = SnapX.Settings.SecondaryTextUploaders[retry - 1];
                Info.TaskSettings.TextFileDestination = SnapX.Settings.SecondaryFileUploaders[retry - 1];
                Info.TaskSettings.FileDestination = SnapX.Settings.SecondaryFileUploaders[retry - 1];
            }
            else
            {
                Thread.Sleep(1000);
            }
        }

        SSLBypassHelper sslBypassHelper = null;

        try
        {
            if (HelpersOptions.AcceptInvalidSSLCertificates)
            {
                sslBypassHelper = new SSLBypassHelper();
            }

            if (!CheckUploadFilters(data, fileName))
            {
                switch (Info.UploadDestination)
                {
                    case EDataType.Image:
                        Info.Result = UploadImage(data, fileName);
                        break;
                    case EDataType.Text:
                        Info.Result = UploadText(data, fileName);
                        break;
                    case EDataType.File:
                        Info.Result = UploadFile(data, fileName);
                        break;
                }
            }

            StopRequested |= taskReferenceHelper.StopRequested;
        }
        catch (Exception e)
        {
            if (!StopRequested)
            {
                DebugHelper.WriteException(e);
                isError = true;
                AddErrorMessage(e.ToString());
            }
        }
        finally
        {
            if (sslBypassHelper != null)
            {
                sslBypassHelper.Dispose();
            }

            if (Info.Result == null)
            {
                Info.Result = new UploadResult();
            }

            if (uploader != null)
            {
                AddErrorMessage(uploader.Errors);
            }

            isError |= Info.Result.IsError;
        }

        return isError;
    }

    private void AddErrorMessage(UploaderErrorManager errors)
    {
        if (Info.Result == null)
        {
            Info.Result = new UploadResult();
        }

        Info.Result.Errors.Add(errors);
    }

    private void AddErrorMessage(string? error)
    {
        Info.Result ??= new UploadResult();

        Info.Result.Errors.Add(error);
    }

    private bool DoThreadJob()
    {
        if (Info.IsUploadJob && Info.TaskSettings.AdvancedSettings.AutoClearClipboard)
        {
            Clipboard.Clear();
        }

        if (Info.Job == TaskJob.Download || Info.Job == TaskJob.DownloadUpload)
        {
            bool downloadResult = DownloadFromURL(Info.Job == TaskJob.DownloadUpload);

            if (!downloadResult)
            {
                return false;
            }
            else if (Info.Job == TaskJob.Download)
            {
                return true;
            }
        }

        if (Info.Job == TaskJob.Job)
        {
            if (!DoAfterCaptureJobs())
            {
                return false;
            }

            DoFileJobs();
        }
        else if (Info.Job == TaskJob.TextUpload && !string.IsNullOrEmpty(Text))
        {
            DoTextJobs();
        }
        else if (Info.Job == TaskJob.FileUpload && Info.TaskSettings.AdvancedSettings.UseAfterCaptureTasksDuringFileUpload)
        {
            DoFileJobs();
        }

        if (Info.TaskSettings.AfterCaptureJob.HasFlag(AfterCaptureTasks.DoOCR))
        {
            DoOCR();
        }

        if (Info.IsUploadJob && Data != null && Data.CanSeek)
        {
            Data.Position = 0;
        }

        return true;
    }

    private bool DoAfterCaptureJobs()
    {
        if (Image == null)
        {
            return true;
        }

        if (Info.TaskSettings.AfterCaptureJob.HasFlag(AfterCaptureTasks.BeautifyImage))
        {
            throw new NotImplementedException("AfterCaptureTasks.BeautifyImage is not implemented");
            // Image = TaskHelpers.BeautifyImage(Image, Info.TaskSettings);
            //
            // if (Image == null)
            // {
            //     return false;
            // }
        }

        if (Info.TaskSettings.AfterCaptureJob.HasFlag(AfterCaptureTasks.AddImageEffects))
        {
            throw new NotImplementedException("AfterCaptureTasks.AddImageEffects is not implemented");
            // Image = TaskHelpers.ApplyImageEffects(Image, Info.TaskSettings.ImageSettingsReference);
            //
            // if (Image == null)
            // {
            //     DebugHelper.WriteLine("Error: Applying image effects resulted empty image.");
            //     return false;
            // }
        }

        if (Info.TaskSettings.AfterCaptureJob.HasFlag(AfterCaptureTasks.AnnotateImage))
        {
            throw new NotImplementedException("AfterCaptureTasks.AnnotateImage is not implemented");
            // Image = TaskHelpers.AnnotateImage(Image, null, Info.TaskSettings, true);
            //
            // if (Image == null)
            // {
            //     return false;
            // }
        }

        if (Info.TaskSettings.AfterCaptureJob.HasFlag(AfterCaptureTasks.CopyImageToClipboard))
        {
            Clipboard.CopyImage(Image, Info.FileName);
            DebugHelper.WriteLine("Image copied to clipboard.");
        }

        if (Info.TaskSettings.AfterCaptureJob.HasFlag(AfterCaptureTasks.PinToScreen))
        {
            // Image imageCopy = Image.CloneSafe();
            // TaskHelpers.PinToScreen(imageCopy, Info.TaskSettings);
            throw new NotImplementedException("PinToScreen is not implemented.");
        }

        if (Info.TaskSettings.AfterCaptureJob.HasFlag(AfterCaptureTasks.SendImageToPrinter))
        {
            throw new NotImplementedException("SendImageToPrinter is not implemented and never will be. :)");
        }

        Info.Metadata.Image = Image;

        if (Info.TaskSettings.AfterCaptureJob.HasFlagAny(AfterCaptureTasks.SaveImageToFile, AfterCaptureTasks.SaveImageToFileWithDialog, AfterCaptureTasks.DoOCR,
            AfterCaptureTasks.UploadImageToHost))
        {
            var imageData = TaskHelpers.PrepareImage(Image, Info.TaskSettings);
            Data = imageData.ImageStream;
            Info.FileName = Path.ChangeExtension(Info.FileName, imageData.ImageFormat.GetDescription());

            if (Info.TaskSettings.AfterCaptureJob.HasFlag(AfterCaptureTasks.SaveImageToFile))
            {
                string? screenshotsFolder = TaskHelpers.GetScreenshotsFolder(Info.TaskSettings, Info.Metadata);
                string? filePath = TaskHelpers.HandleExistsFile(screenshotsFolder, Info.FileName, Info.TaskSettings);

                if (!string.IsNullOrEmpty(filePath))
                {
                    Info.FilePath = filePath;
                    imageData.Write(Info.FilePath);
                    DebugHelper.WriteLine("Image saved to file: " + Info.FilePath);
                }
            }

            if (Info.TaskSettings.AfterCaptureJob.HasFlag(AfterCaptureTasks.SaveImageToFileWithDialog))
            {
                throw new NotImplementedException("AfterCaptureTasks.SaveImageToFileWithDialog is not implemented. Needs to be reimplemented in SnapX.CommonUI");
            }

            if (Info.TaskSettings.AfterCaptureJob.HasFlag(AfterCaptureTasks.SaveThumbnailImageToFile))
            {
                string? thumbnailFileName;
                string? thumbnailFolder;

                if (!string.IsNullOrEmpty(Info.FilePath))
                {
                    thumbnailFileName = Path.GetFileName(Info.FilePath);
                    thumbnailFolder = Path.GetDirectoryName(Info.FilePath);
                }
                else
                {
                    thumbnailFileName = Info.FileName;
                    thumbnailFolder = TaskHelpers.GetScreenshotsFolder(Info.TaskSettings, Info.Metadata);
                }

                // Info.ThumbnailFilePath = TaskHelpers.CreateThumbnail(Image, thumbnailFolder, thumbnailFileName, Info.TaskSettings);
                //
                // if (!string.IsNullOrEmpty(Info.ThumbnailFilePath))
                // {
                //     DebugHelper.WriteLine("Thumbnail saved to file: " + Info.ThumbnailFilePath);
                // }
            }
        }

        return true;
    }

    private void DoFileJobs()
    {
        if (!string.IsNullOrEmpty(Info.FilePath) && System.IO.File.Exists(Info.FilePath))
        {
            if (Info.TaskSettings.AfterCaptureJob.HasFlag(AfterCaptureTasks.PerformActions) && Info.TaskSettings.ExternalPrograms != null)
            {
                var actions = Info.TaskSettings.ExternalPrograms.Where(x => x.IsActive);

                if (actions.Any())
                {
                    var isFileModified = false;
                    var fileName = Info.FileName;

                    foreach (ExternalProgram fileAction in actions)
                    {
                        var modifiedPath = fileAction.Run(Info.FilePath);

                        if (string.IsNullOrEmpty(modifiedPath)) continue;
                        isFileModified = true;
                        Info.FilePath = modifiedPath;

                        Data?.Dispose();

                        fileAction.DeletePendingInputFile();
                    }

                    if (isFileModified)
                    {
                        string extension = FileHelpers.GetFileNameExtension(Info.FilePath);
                        Info.FileName = FileHelpers.ChangeFileNameExtension(fileName, extension);

                        LoadFileStream();
                    }
                }
            }

            if (Info.TaskSettings.AfterCaptureJob.HasFlag(AfterCaptureTasks.CopyFileToClipboard))
            {
                Clipboard.CopyFile(Info.FilePath);
            }
            else if (Info.TaskSettings.AfterCaptureJob.HasFlag(AfterCaptureTasks.CopyFilePathToClipboard))
            {
                Clipboard.CopyText(Info.FilePath);
            }

            if (Info.TaskSettings.AfterCaptureJob.HasFlag(AfterCaptureTasks.ShowInExplorer))
            {
                FileHelpers.OpenFolderWithFile(Info.FilePath);
            }

            if (Info.TaskSettings.AfterCaptureJob.HasFlag(AfterCaptureTasks.ScanQRCode) && Info.DataType == EDataType.Image)
            {
                throw new NotImplementedException("QR Code Scanner not implemented");
            }
        }
    }

    private void DoTextJobs()
    {
        if (Info.TaskSettings.AdvancedSettings.TextTaskSaveAsFile)
        {
            string? screenshotsFolder = TaskHelpers.GetScreenshotsFolder(Info.TaskSettings);
            string? filePath = TaskHelpers.HandleExistsFile(screenshotsFolder, Info.FileName, Info.TaskSettings);

            if (!string.IsNullOrEmpty(filePath))
            {
                Info.FilePath = filePath;
                FileHelpers.CreateDirectoryFromFilePath(Info.FilePath);
                System.IO.File.WriteAllText(Info.FilePath, Text, Encoding.UTF8);
                DebugHelper.WriteLine("Text saved to file: " + Info.FilePath);
            }
        }

        byte[] byteArray = Encoding.UTF8.GetBytes(Text);
        Data = new MemoryStream(byteArray);
    }

    private void DoAfterUploadJobs()
    {
        try
        {
            if (Info.TaskSettings.UploadSettings.URLRegexReplace)
            {
                Info.Result.URL = Regex.Replace(Info.Result.URL, Info.TaskSettings.UploadSettings.URLRegexReplacePattern,
                    Info.TaskSettings.UploadSettings.URLRegexReplaceReplacement);
            }

            if (Info.TaskSettings.AdvancedSettings.ResultForceHTTPS)
            {
                Info.Result.ForceHTTPS();
            }

            if (Info.Job != TaskJob.ShareURL && (Info.TaskSettings.AfterUploadJob.HasFlag(AfterUploadTasks.UseURLShortener) || Info.Job == TaskJob.ShortenURL ||
                (Info.TaskSettings.AdvancedSettings.AutoShortenURLLength > 0 && Info.Result.URL.Length > Info.TaskSettings.AdvancedSettings.AutoShortenURLLength)))
            {
                UploadResult result = ShortenURL(Info.Result.URL);

                if (result != null)
                {
                    Info.Result.ShortenedURL = result.ShortenedURL;
                    Info.Result.Errors.Add(result.Errors);
                }
            }

            if (Info.Job != TaskJob.ShortenURL && (Info.TaskSettings.AfterUploadJob.HasFlag(AfterUploadTasks.ShareURL) || Info.Job == TaskJob.ShareURL))
            {
                UploadResult result = ShareURL(Info.Result.ToString());

                if (result != null)
                {
                    Info.Result.Errors.Add(result.Errors);
                }

                if (Info.Job == TaskJob.ShareURL)
                {
                    Info.Result.IsURLExpected = false;
                }
            }

            if (Info.TaskSettings.AfterUploadJob.HasFlag(AfterUploadTasks.CopyURLToClipboard))
            {
                var txt = !string.IsNullOrEmpty(Info.TaskSettings.AdvancedSettings.ClipboardContentFormat) ? new UploadInfoParser().Parse(Info, Info.TaskSettings.AdvancedSettings.ClipboardContentFormat) : Info.Result.ToString();

                if (!string.IsNullOrEmpty(txt))
                {
                    Clipboard.CopyText(txt);
                }
            }

            if (Info.TaskSettings.AfterUploadJob.HasFlag(AfterUploadTasks.OpenURL))
            {
                var result = !string.IsNullOrEmpty(Info.TaskSettings.AdvancedSettings.OpenURLFormat) ? new UploadInfoParser().Parse(Info, Info.TaskSettings.AdvancedSettings.OpenURLFormat) : Info.Result.ToString();

                URLHelpers.OpenURL(result);
            }

            if (Info.TaskSettings.AfterUploadJob.HasFlag(AfterUploadTasks.ShowQRCode))
            {
                throw new NotImplementedException("QR Code Scanner not implemented");
            }
        }
        catch (Exception e)
        {
            DebugHelper.WriteException(e);
            AddErrorMessage(e.ToString());
        }
    }

    public UploadResult UploadData(IGenericUploaderService service, Stream stream, string? fileName)
    {
        if (!service.CheckConfig(SnapX.UploadersConfig))
        {
            return GetInvalidConfigResult(service);
        }

        uploader = service.CreateUploader(SnapX.UploadersConfig, taskReferenceHelper);

        if (uploader != null)
        {
            uploader.Errors.DefaultTitle = service.ServiceName + " " + "error";
            uploader.BufferSize = (int)Math.Pow(2, SnapX.Settings.BufferSizePower) * 1024;
            uploader.ProgressChanged += uploader_ProgressChanged;

            if (Info.TaskSettings.AfterUploadJob.HasFlag(AfterUploadTasks.CopyURLToClipboard) && Info.TaskSettings.AdvancedSettings.EarlyCopyURL)
            {
                uploader.EarlyURLCopyRequested += url =>
                {
                    Clipboard.CopyText(url);
                    EarlyURLCopied = true;
                };
            }

            fileName = URLHelpers.RemoveBidiControlCharacters(fileName);

            if (Info.TaskSettings.UploadSettings.FileUploadReplaceProblematicCharacters)
            {
                fileName = URLHelpers.ReplaceReservedCharacters(fileName, "_");
            }

            Info.UploadDuration = Stopwatch.StartNew();

            UploadResult result = uploader.Upload(stream, fileName);

            Info.UploadDuration.Stop();

            return result;
        }

        return null;
    }

    private bool CheckUploadFilters(Stream stream, string? fileName)
    {
        if (Info.TaskSettings.UploadSettings.UploaderFilters != null && !string.IsNullOrEmpty(fileName) && stream != null)
        {
            UploaderFilter filter = Info.TaskSettings.UploadSettings.UploaderFilters.FirstOrDefault(x => x.IsValidFilter(fileName));

            if (filter != null)
            {
                IGenericUploaderService service = filter.GetUploaderService();

                if (service != null)
                {
                    Info.Result = UploadData(service, stream, fileName);

                    return true;
                }
            }
        }

        return false;
    }

    public UploadResult UploadImage(Stream stream, string? fileName)
    {
        ImageUploaderService service = UploaderFactory.ImageUploaderServices[Info.TaskSettings.ImageDestination];

        return UploadData(service, stream, fileName);
    }

    public UploadResult UploadText(Stream stream, string? fileName)
    {
        TextUploaderService service = UploaderFactory.TextUploaderServices[Info.TaskSettings.TextDestination];

        return UploadData(service, stream, fileName);
    }

    public UploadResult UploadFile(Stream stream, string? fileName)
    {
        FileUploaderService service = UploaderFactory.FileUploaderServices[Info.TaskSettings.GetFileDestinationByDataType(Info.DataType)];

        return UploadData(service, stream, fileName);
    }

    public UploadResult ShortenURL(string? url)
    {
        URLShortenerService service = UploaderFactory.URLShortenerServices[Info.TaskSettings.URLShortenerDestination];

        if (!service.CheckConfig(SnapX.UploadersConfig))
        {
            return GetInvalidConfigResult(service);
        }

        URLShortener urlShortener = service.CreateShortener(SnapX.UploadersConfig, taskReferenceHelper);

        if (urlShortener != null)
        {
            return urlShortener.ShortenURL(url);
        }

        return null;
    }

    public UploadResult ShareURL(string? url)
    {
        if (!string.IsNullOrEmpty(url))
        {
            URLSharingService service = UploaderFactory.URLSharingServices[Info.TaskSettings.URLSharingServiceDestination];

            if (!service.CheckConfig(SnapX.UploadersConfig))
            {
                return GetInvalidConfigResult(service);
            }

            URLSharer urlSharer = service.CreateSharer(SnapX.UploadersConfig, taskReferenceHelper);

            if (urlSharer != null)
            {
                return urlSharer.ShareURL(url);
            }
        }

        return null;
    }

    private UploadResult GetInvalidConfigResult(IUploaderService uploaderService)
    {
        UploadResult ur = new UploadResult();

        string? message = string.Format("{0} configuration is invalid or missing. Please check \"Destination settings\" window to configure it.",
            uploaderService.ServiceName);
        DebugHelper.WriteLine(message);
        ur.Errors.Add(message);

        OnUploadersConfigWindowRequested(uploaderService);

        return ur;
    }

    private bool DownloadFromURL(bool upload)
    {
        string url = Info.Result.URL.Trim();
        Info.Result.URL = "";

        if (!Info.TaskSettings.UploadSettings.FileUploadUseNamePattern)
        {
            try
            {
                string? fileName = WebHelpers.GetFileNameFromWebServerAsync(url).GetAwaiter().GetResult();

                if (!string.IsNullOrEmpty(fileName))
                {
                    Info.FileName = FileHelpers.SanitizeFileName(fileName);
                }
            }
            catch (Exception e)
            {
                DebugHelper.WriteException(e);
            }
        }

        string? screenshotsFolder = TaskHelpers.GetScreenshotsFolder(Info.TaskSettings);
        Info.FilePath = TaskHelpers.HandleExistsFile(screenshotsFolder, Info.FileName, Info.TaskSettings);

        if (!string.IsNullOrEmpty(Info.FilePath))
        {
            Info.Status = "Downloading";
            OnStatusChanged();

            try
            {
                WebHelpers.DownloadFileAsync(url, Info.FilePath).GetAwaiter().GetResult();

                if (upload)
                {
                    LoadFileStream();
                }

                return true;
            }
            catch (Exception e)
            {
                DebugHelper.WriteException(e);
            }
        }

        return false;
    }

    private void DoOCR()
    {
        if (Image != null && Info.DataType == EDataType.Image)
        {
            TaskHelpers.OCRImage(Image, Info.TaskSettings).GetAwaiter().GetResult();
        }
    }

    private bool LoadFileStream()
    {
        try
        {
            Data = new FileStream(Info.FilePath, FileMode.Open, FileAccess.Read, FileShare.Read);
        }
        catch (Exception e)
        {
            e.ShowError();
            return false;
        }

        return true;
    }

    private void ThreadCompleted()
    {
        OnTaskCompleted();
    }

    private void uploader_ProgressChanged(ProgressManager progress)
    {
        if (progress != null)
        {
            Info.Progress = progress;

            OnUploadProgressChanged();
        }
    }

    private void OnStatusChanged()
    {
        if (StatusChanged != null)
        {
            threadWorker.InvokeAsync(() => StatusChanged(this));
        }
    }

    private void OnImageReady()
    {
        if (ImageReady != null)
        {
            Image image = null;

            if (SnapX.Settings.TaskViewMode == TaskViewMode.ThumbnailView && Image != null)
            {
                DebugHelper.WriteException(new NotImplementedException("SnapX.Settings.TaskViewMode == TaskViewMode.ThumbnailView"));
            }

            threadWorker.InvokeAsync(() =>
            {
                using (image)
                {
                    ImageReady(this, image);
                }
            });
        }
    }

    private void OnUploadStarted()
    {
        if (UploadStarted != null)
        {
            threadWorker.InvokeAsync(() => UploadStarted(this));
        }
    }

    private void OnUploadCompleted()
    {
        if (UploadCompleted != null)
        {
            threadWorker.InvokeAsync(() => UploadCompleted(this));
        }
    }

    private void OnUploadProgressChanged()
    {
        if (UploadProgressChanged != null)
        {
            threadWorker.InvokeAsync(() => UploadProgressChanged(this));
        }
    }

    private void OnTaskCompleted()
    {
        Info.TaskEndTime = DateTime.Now;

        if (StopRequested)
        {
            Status = TaskStatus.Stopped;
            Info.Status = "Stopped";
            DebugHelper.WriteLine("StopRequested");
        }
        else if (Info.Result.IsError)
        {
            Status = TaskStatus.Failed;
            Info.Status = "Error";
        }
        else
        {
            Status = TaskStatus.Completed;
            Info.Status = "Done";
        }

        TaskCompleted?.Invoke(this);

        Dispose();
    }

    private void OnUploadersConfigWindowRequested(IUploaderService uploaderService)
    {
        if (UploadersConfigWindowRequested != null)
        {
            threadWorker.InvokeAsync(() => UploadersConfigWindowRequested(uploaderService));
        }
    }

    public void Dispose()
    {
        if (Data != null)
        {
            Data.Dispose();
            Data = null;
        }

        if (!KeepImage && Image != null)
        {
            Image.Dispose();
            Image = null;
        }
    }
}
