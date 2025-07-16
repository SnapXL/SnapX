// SPDX-License-Identifier: GPL-3.0-or-later


using SnapX.Core.Job;
using SnapX.Core.Utils;

namespace SnapX.Core.Upload;

public class UploadInfoStatus
{
    public WorkerTask Task { get; private set; }

    public TaskInfo Info => Task.Info;

    public bool IsURLExist { get; private set; }
    public bool IsShortenedURLExist { get; private set; }
    public bool IsThumbnailURLExist { get; private set; }
    public bool IsDeletionURLExist { get; private set; }
    public bool IsFileURL { get; private set; }
    public bool IsImageURL { get; private set; }
    public bool IsTextURL { get; private set; }
    public bool IsFilePathValid { get; private set; }
    public bool IsFileExist { get; private set; }
    public bool IsThumbnailFilePathValid { get; private set; }
    public bool IsThumbnailFileExist { get; private set; }
    public bool IsImageFile { get; private set; }
    public bool IsTextFile { get; private set; }

    public UploadInfoStatus(WorkerTask task)
    {
        Task = task;
        Update();
    }

    public void Update()
    {
        if (Info.Result != null)
        {
            IsURLExist = !string.IsNullOrEmpty(Info.Result.URL);
            IsShortenedURLExist = !string.IsNullOrEmpty(Info.Result.ShortenedURL);
            IsThumbnailURLExist = !string.IsNullOrEmpty(Info.Result.ThumbnailURL);
            IsDeletionURLExist = !string.IsNullOrEmpty(Info.Result.DeletionURL);
            IsFileURL = IsURLExist && URLHelpers.IsFileURL(Info.Result.URL);
            IsImageURL = IsFileURL && FileHelpers.IsImageFile(Info.Result.URL);
            IsTextURL = IsFileURL && FileHelpers.IsTextFile(Info.Result.URL);
        }

        IsFilePathValid = !string.IsNullOrEmpty(Info.FilePath) && Path.HasExtension(Info.FilePath);
        IsFileExist = IsFilePathValid && System.IO.File.Exists(Info.FilePath);
        IsThumbnailFilePathValid = !string.IsNullOrEmpty(Info.ThumbnailFilePath) && Path.HasExtension(Info.ThumbnailFilePath);
        IsThumbnailFileExist = IsThumbnailFilePathValid && System.IO.File.Exists(Info.ThumbnailFilePath);
        IsImageFile = IsFileExist && FileHelpers.IsImageFile(Info.FilePath);
        IsTextFile = IsFileExist && FileHelpers.IsTextFile(Info.FilePath);
    }
}

