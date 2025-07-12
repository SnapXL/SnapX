
// SPDX-License-Identifier: GPL-3.0-or-later



using System.Diagnostics;
using SnapX.Core.History;
using SnapX.Core.Upload;
using SnapX.Core.Upload.Utils;
using SnapX.Core.Utils.Extensions;

namespace SnapX.Core.Job;
public record TaskInfo
{
    public TaskSettings? TaskSettings { get; set; }

    public string Status { get; set; }
    public TaskJob Job { get; set; }

    public bool IsUploadJob
    {
        get
        {
            switch (Job)
            {
                case TaskJob.Job:
                    return TaskSettings.AfterCaptureJob.HasFlag(AfterCaptureTasks.UploadImageToHost);
                case TaskJob.DataUpload:
                case TaskJob.FileUpload:
                case TaskJob.TextUpload:
                case TaskJob.ShortenURL:
                case TaskJob.ShareURL:
                case TaskJob.DownloadUpload:
                    return true;
            }

            return false;
        }
    }

    public ProgressManager Progress { get; set; }

    private string? filePath;

    public string? FilePath
    {
        get
        {
            return filePath;
        }
        set
        {
            filePath = value;

            if (string.IsNullOrEmpty(filePath))
            {
                FileName = "";
            }
            else
            {
                FileName = Path.GetFileName(filePath);
            }
        }
    }

    public string? FileName { get; set; }
    public string? ThumbnailFilePath { get; set; }
    public EDataType DataType { get; set; }
    public TaskMetadata Metadata { get; set; }

    public EDataType UploadDestination
    {
        get
        {
            if ((DataType == EDataType.Image && TaskSettings.ImageDestination == ImageDestination.FileUploader) ||
                (DataType == EDataType.Text && TaskSettings.TextDestination == TextDestination.FileUploader))
            {
                return EDataType.File;
            }

            return DataType;
        }
    }

    public string UploaderHost
    {
        get
        {
            if (IsUploadJob)
            {
                switch (UploadDestination)
                {
                    case EDataType.Image:
                        return TaskSettings.ImageDestination.GetLocalizedDescription();
                    case EDataType.Text:
                        return TaskSettings.TextDestination.GetLocalizedDescription();
                    case EDataType.File:
                        switch (DataType)
                        {
                            case EDataType.Image:
                                return TaskSettings.ImageFileDestination.GetLocalizedDescription();
                            case EDataType.Text:
                                return TaskSettings.TextFileDestination.GetLocalizedDescription();
                            default:
                            case EDataType.File:
                                return TaskSettings.FileDestination.GetLocalizedDescription();
                        }
                    case EDataType.URL:
                        if (Job == TaskJob.ShareURL)
                        {
                            return TaskSettings.URLSharingServiceDestination.GetLocalizedDescription();
                        }

                        return TaskSettings.URLShortenerDestination.GetLocalizedDescription();
                }
            }

            return "";
        }
    }

    public DateTime TaskStartTime { get; set; }
    public DateTime TaskEndTime { get; set; }

    public TimeSpan TaskDuration => TaskEndTime - TaskStartTime;

    public Stopwatch UploadDuration { get; set; }

    public UploadResult Result { get; set; }

    public TaskInfo(TaskSettings? taskSettings = null)
    {
        taskSettings ??= TaskSettings.GetDefaultTaskSettings();

        TaskSettings = taskSettings;
        Metadata = new TaskMetadata();
        Result = new UploadResult();
    }

    public List<HistoryItem.Tag> GetTags()
    {
        if (Metadata == null)
            return null;

        var tags = new List<HistoryItem.Tag>();

        if (!string.IsNullOrEmpty(Metadata.WindowTitle))
        {
            tags.Add(new HistoryItem.Tag { Name = "WindowTitle", Value = Metadata.WindowTitle });
        }

        if (!string.IsNullOrEmpty(Metadata.ProcessName))
        {
            tags.Add(new HistoryItem.Tag { Name = "ProcessName", Value = Metadata.ProcessName });
        }

        return tags.Count != 0 ? tags : [];
    }


    public override string? ToString()
    {
        string? text = Result.ToString();

        if (string.IsNullOrEmpty(text) && !string.IsNullOrEmpty(FilePath))
        {
            text = FilePath;
        }

        return text;
    }

    public HistoryItem GetHistoryItem()
    {
        return new HistoryItem
        {
            FileName = FileName,
            FilePath = FilePath,
            DateTime = TaskEndTime,
            Type = DataType.ToString(),
            Host = UploaderHost,
            URL = Result.URL,
            ThumbnailURL = Result.ThumbnailURL,
            DeletionURL = Result.DeletionURL,
            ShortenedURL = Result.ShortenedURL,
            Tags = GetTags()
        };
    }
}
