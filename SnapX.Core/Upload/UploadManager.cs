
// SPDX-License-Identifier: GPL-3.0-or-later


using System.Runtime.InteropServices;
using System.Web;
using SixLabors.ImageSharp;
using SnapX.Core.Interfaces;
using SnapX.Core.Job;
using SnapX.Core.Utils;
using SnapX.Core.Utils.Extensions;
using SnapX.Core.Utils.Native;
using Xdg.Directories;

namespace SnapX.Core.Upload;

public class UploadManager
{
    private static IFilePicker _filePicker;

    public UploadManager(IFilePicker filePicker)
    {
        _filePicker = filePicker;
    }
    public static void UploadFile(string? filePath, TaskSettings? taskSettings = null)
    {
        taskSettings ??= TaskSettings.GetDefaultTaskSettings();

        if (!string.IsNullOrEmpty(filePath))
        {
            if (System.IO.File.Exists(filePath))
            {
                WorkerTask task = WorkerTask.CreateFileUploaderTask(filePath, taskSettings);
                TaskManager.Start(task);
            }
            else if (Directory.Exists(filePath))
            {
                string?[] files = Directory.GetFiles(filePath, "*.*", SearchOption.AllDirectories);
                UploadFile(files, taskSettings);
            }
        }
    }

    public static void UploadFile(string?[] files, TaskSettings? taskSettings = null)
    {
        taskSettings ??= TaskSettings.GetDefaultTaskSettings();

        if (files == null || files.Length == 0)
            return;

        if (files.Length > 10 && !IsUploadConfirmed(files.Length))
            return;

        foreach (var file in files)
        {
            UploadFile(file, taskSettings);
        }
    }

    private static bool IsUploadConfirmed(int length)
    {
        if (SnapX.Settings.ShowMultiUploadWarning)
        {
            // using (MyMessageBox msgbox = new MyMessageBox(string.Format(Resources.UploadManager_IsUploadConfirmed_Are_you_sure_you_want_to_upload__0__files_, length),
            //     "SnapX - " + Resources.UploadManager_IsUploadConfirmed_Upload_files,
            //     MessageBoxButtons.YesNo, Resources.UploadManager_IsUploadConfirmed_Don_t_show_this_message_again_))
            // {
            //     msgbox.ShowDialog();
            //     SnapX.Settings.ShowMultiUploadWarning = !msgbox.IsChecked;
            //     return msgbox.DialogResult == DialogResult.Yes;
            // }
            throw new NotImplementedException("SnapX.Settings.ShowMultiUploadWarning is not implemented");
        }

        return true;
    }

    public static void UploadFile(TaskSettings? taskSettings = null)
    {
        taskSettings ??= TaskSettings.GetDefaultTaskSettings();
        var title = Lang.UploadManagerUploadFile;
        var initialDir = IsValidDirectory(SnapX.Settings.FileUploadDefaultDirectory)
            ? SnapX.Settings.FileUploadDefaultDirectory
            : UserDirectory.DesktopDir;

        DebugHelper.WriteLine("Need file to upload. Asking UI for file.");
        var selectedFiles = _filePicker.PickFilesAsync(
            title,
            initialDir,
            allowMultiple: true).GetAwaiter().GetResult();

        if (selectedFiles.Length == 0)
        {
            DebugHelper.WriteLine("User cancelled file picker.");
            return;
        }

        DebugHelper.WriteLine($"User selected {selectedFiles.Length} file(s) to upload.");
        UploadFile(selectedFiles);
    }

    public static bool IsValidDirectory(string? dir)
    {
        return !string.IsNullOrEmpty(dir) && Directory.Exists(dir);
    }

    public static void UploadFolder(TaskSettings? taskSettings = null)
    {
        // using (FolderSelectDialog folderDialog = new FolderSelectDialog())
        // {
        //     folderDialog.Title = "SnapX - " + Resources.UploadManager_UploadFolder_Folder_upload;
        //
        //     if (!string.IsNullOrEmpty(SnapX.Settings.FileUploadDefaultDirectory) && Directory.Exists(SnapX.Settings.FileUploadDefaultDirectory))
        //     {
        //         folderDialog.InitialDirectory = SnapX.Settings.FileUploadDefaultDirectory;
        //     }
        //     else
        //     {
        //         folderDialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
        //     }
        //
        //     if (folderDialog.ShowDialog() && !string.IsNullOrEmpty(folderDialog.FileName))
        //     {
        //         SnapX.Settings.FileUploadDefaultDirectory = folderDialog.FileName;
        //         UploadFile(folderDialog.FileName, taskSettings);
        //     }
        // }
    }

    public static void ProcessImageUpload(Image image, TaskSettings? taskSettings)
    {
        if (image != null)
        {
            if (!taskSettings.AdvancedSettings.ProcessImagesDuringClipboardUpload)
            {
                taskSettings.AfterCaptureJob = AfterCaptureTasks.UploadImageToHost;
            }

            RunImageTask(image, taskSettings);
        }
    }

    public static void ProcessTextUpload(string? text, TaskSettings? taskSettings)
    {
        if (string.IsNullOrEmpty(text))
            return;

        var url = text.Trim();

        if (URLHelpers.IsValidURL(url))
        {
            if (taskSettings.UploadSettings.ClipboardUploadURLContents)
            {
                DownloadAndUploadFile(url, taskSettings);
                return;
            }

            if (taskSettings.UploadSettings.ClipboardUploadShortenURL)
            {
                ShortenURL(url, taskSettings);
                return;
            }

            if (taskSettings.UploadSettings.ClipboardUploadShareURL)
            {
                ShareURL(url, taskSettings);
                return;
            }
        }

        if (taskSettings.UploadSettings.ClipboardUploadAutoIndexFolder && text.Length <= 260 && Directory.Exists(text))
        {
            IndexFolder(text, taskSettings);
        }
        else
        {
            UploadText(text, taskSettings, true);
        }
    }

    public static void ProcessFilesUpload(string?[] files, TaskSettings? taskSettings)
    {
        if (files?.Length > 0)
        {
            UploadFile(files, taskSettings);
        }
    }


    public static void ClipboardUpload(TaskSettings? taskSettings = null)
    {
        taskSettings ??= TaskSettings.GetDefaultTaskSettings();

        try
        {
            if (Clipboard.ContainsImage())
            {
                var image = Clipboard.GetImage();

                ProcessImageUpload(image, taskSettings);
            }
            else if (Clipboard.ContainsText())
            {
                var text = Clipboard.GetText();

                ProcessTextUpload(text, taskSettings);
            }
            else if (Clipboard.ContainsFileDropList())
            {
                string?[] files = Clipboard.GetFileDropList().Cast<string>().ToArray();

                ProcessFilesUpload(files, taskSettings);
            }
        }
        catch (ExternalException e)
        {
            DebugHelper.WriteException(e);
            // Basic retries. Should use Polly Nuget package
            ClipboardUpload(taskSettings);

        }
        catch (Exception e)
        {
            DebugHelper.WriteException(e);
        }
    }

    public static void UploadURL(TaskSettings? taskSettings = null, string? url = null)
    {
        taskSettings ??= TaskSettings.GetDefaultTaskSettings();

        string? inputText = null;

        string? text = Clipboard.GetText();

        if (URLHelpers.IsValidURL(text))
        {
            inputText = text;
        }


        if (!string.IsNullOrEmpty(url))
        {
            DownloadAndUploadFile(url, taskSettings);
        }
    }
    public static void RunImageTask(Image image, TaskSettings? taskSettings)
    {
        var metadata = new TaskMetadata(image);
        RunImageTask(metadata, taskSettings);
    }
    public static void RunImageTask(Image image, TaskSettings? taskSettings, bool skipQuickTaskMenu = false, bool skipAfterCaptureWindow = false)
    {
        var metadata = new TaskMetadata(image);
        RunImageTask(metadata, taskSettings, skipQuickTaskMenu, skipAfterCaptureWindow);
    }

    public static void RunImageTask(TaskMetadata metadata, TaskSettings? taskSettings, bool skipQuickTaskMenu = false, bool skipAfterCaptureWindow = false)
    {
        taskSettings ??= TaskSettings.GetDefaultTaskSettings();

        if (metadata is { Image: not null } && taskSettings != null)
        {
            if (!skipQuickTaskMenu && taskSettings.AfterCaptureJob.HasFlag(AfterCaptureTasks.ShowQuickTaskMenu))
            {
                RunImageTask(metadata, taskSettings, true);
                return;
            }

            string customFileName = null;


            var task = WorkerTask.CreateImageUploaderTask(metadata, taskSettings, customFileName);
            TaskManager.Start(task);
        }
    }

    public static void UploadImage(Image? image, TaskSettings? taskSettings = null)
    {
        taskSettings ??= TaskSettings.GetDefaultTaskSettings();
        if (image == null) return;
        if (taskSettings is { IsSafeTaskSettings: true })
        {
            taskSettings.UseDefaultAfterCaptureJob = false;
            taskSettings.AfterCaptureJob = AfterCaptureTasks.UploadImageToHost;
        }

        RunImageTask(image, taskSettings);
    }

    public static void UploadImage(Image? image, ImageDestination imageDestination, FileDestination imageFileDestination, TaskSettings? taskSettings = null)
    {
        if (image == null) return;
        taskSettings ??= TaskSettings.GetDefaultTaskSettings();

        if (taskSettings is { IsSafeTaskSettings: true })
        {
            taskSettings.UseDefaultAfterCaptureJob = false;
            taskSettings.AfterCaptureJob = AfterCaptureTasks.UploadImageToHost;
            taskSettings.UseDefaultDestinations = false;
            taskSettings.ImageDestination = imageDestination;
            taskSettings.ImageFileDestination = imageFileDestination;
        }

        RunImageTask(image, taskSettings);
    }
    public static void UploadText(string? text, TaskSettings? taskSettings = null, bool allowCustomText = false)
    {
        taskSettings ??= TaskSettings.GetDefaultTaskSettings();

        if (string.IsNullOrEmpty(text)) return;

        if (allowCustomText)
        {
            var input = taskSettings?.AdvancedSettings.TextCustom;

            if (!string.IsNullOrEmpty(input))
            {
                if (taskSettings is { AdvancedSettings.TextCustomEncodeInput: true })
                {
                    text = HttpUtility.HtmlEncode(text);
                }

                text = input.Replace("%input", text);
            }
        }

        var task = WorkerTask.CreateTextUploaderTask(text, taskSettings);
        TaskManager.Start(task);
    }

    public static void UploadImageStream(Stream? stream, string? fileName, TaskSettings? taskSettings = null)
    {
        taskSettings ??= TaskSettings.GetDefaultTaskSettings();

        if (stream == null || stream.Length == 0 || string.IsNullOrEmpty(fileName))
            return;

        var task = WorkerTask.CreateDataUploaderTask(EDataType.Image, stream, fileName, taskSettings);
        TaskManager.Start(task);
    }


    public static void ShortenURL(string? url, TaskSettings? taskSettings = null)
    {
        if (string.IsNullOrEmpty(url))
            return;

        taskSettings ??= TaskSettings.GetDefaultTaskSettings();
        var task = WorkerTask.CreateURLShortenerTask(url, taskSettings);
        TaskManager.Start(task);
    }


    public static void ShortenURL(string? url, UrlShortenerType urlShortener)
    {
        if (string.IsNullOrEmpty(url))
            return;

        var taskSettings = TaskSettings.GetDefaultTaskSettings();
        taskSettings.URLShortenerDestination = urlShortener;

        var task = WorkerTask.CreateURLShortenerTask(url, taskSettings);
        TaskManager.Start(task);
    }


    public static void ShareURL(string? url, TaskSettings? taskSettings = null)
    {
        if (string.IsNullOrEmpty(url))
            return;

        taskSettings ??= TaskSettings.GetDefaultTaskSettings();

        var task = WorkerTask.CreateShareURLTask(url, taskSettings);
        TaskManager.Start(task);
    }


    public static void ShareURL(string? url, URLSharingServices urlSharingService)
    {
        if (string.IsNullOrEmpty(url))
            return;

        var taskSettings = TaskSettings.GetDefaultTaskSettings();
        taskSettings.URLSharingServiceDestination = urlSharingService;

        var task = WorkerTask.CreateShareURLTask(url, taskSettings);
        TaskManager.Start(task);
    }

    public static void DownloadFile(string? url, TaskSettings? taskSettings = null)
        => DownloadFile(url, false, taskSettings);

    public static void DownloadAndUploadFile(string? url, TaskSettings? taskSettings = null)
        => DownloadFile(url, true, taskSettings);

    private static void DownloadFile(string? url, bool upload, TaskSettings? taskSettings = null)
    {
        DebugHelper.WriteLine($"Downloading file {url}");
        DebugHelper.WriteLine($"Upload: {upload}");
        if (string.IsNullOrEmpty(url)) return;

        taskSettings ??= TaskSettings.GetDefaultTaskSettings();

        var task = WorkerTask.CreateDownloadTask(url, upload, taskSettings);

        if (task != null)
        {
            TaskManager.Start(task);
        }
    }

    public static void IndexFolder(string? folderPath, TaskSettings? taskSettings = null)
    {
        if (string.IsNullOrEmpty(folderPath) || !Directory.Exists(folderPath)) return;

        taskSettings ??= TaskSettings.GetDefaultTaskSettings();
        taskSettings.ToolsSettings.IndexerSettings.BinaryUnits = SnapX.Settings.BinaryUnits;

        string? source = null;

        Task.Run(() =>
        {
            source = Indexer.Indexer.Index(folderPath, taskSettings.ToolsSettings.IndexerSettings);
        }).ContinueInCurrentContext(() =>
        {
            if (string.IsNullOrEmpty(source)) return;
            var task = WorkerTask.CreateTextUploaderTask(source, taskSettings);
            task.Info.FileName = Path.ChangeExtension(task.Info.FileName, taskSettings.ToolsSettings.IndexerSettings.Output.ToString().ToLower());
            TaskManager.Start(task);
        });
    }
}

