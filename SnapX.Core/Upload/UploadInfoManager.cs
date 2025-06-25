
// SPDX-License-Identifier: GPL-3.0-or-later


using SnapX.Core.Job;
using SnapX.Core.Utils;
using SnapX.Core.Utils.Native;

namespace SnapX.Core.Upload;

public class UploadInfoManager
{
    public UploadInfoStatus[] SelectedItems { get; private set; }

    public UploadInfoStatus SelectedItem
    {
        get
        {
            if (IsItemSelected)
            {
                return SelectedItems[0];
            }

            return null;
        }
    }

    public bool IsItemSelected
    {
        get
        {
            return SelectedItems != null && SelectedItems.Length > 0;
        }
    }

    private UploadInfoParser parser;

    public UploadInfoManager()
    {
        parser = new UploadInfoParser();
    }

    public void UpdateSelectedItems(IEnumerable<WorkerTask> tasks)
    {
        if (tasks != null && tasks.Count() > 0)
        {
            SelectedItems = tasks.Where(x => x != null && x.Info != null).Select(x => new UploadInfoStatus(x)).ToArray();
        }
        else
        {
            SelectedItems = null;
        }
    }

    private void CopyTexts(IEnumerable<string> texts)
    {
        if (texts != null && texts.Count() > 0)
        {
            string? urls = string.Join("\r\n", texts.ToArray());

            if (!string.IsNullOrEmpty(urls))
            {
                Clipboard.CopyText(urls);
            }
        }
    }

    #region Open

    public void OpenURL()
    {
        if (IsItemSelected && SelectedItem.IsURLExist) URLHelpers.OpenURL(SelectedItem.Info.Result.URL);
    }

    public void OpenShortenedURL()
    {
        if (IsItemSelected && SelectedItem.IsShortenedURLExist) URLHelpers.OpenURL(SelectedItem.Info.Result.ShortenedURL);
    }

    public void OpenThumbnailURL()
    {
        if (IsItemSelected && SelectedItem.IsThumbnailURLExist) URLHelpers.OpenURL(SelectedItem.Info.Result.ThumbnailURL);
    }

    public void OpenDeletionURL()
    {
        if (IsItemSelected && SelectedItem.IsDeletionURLExist) URLHelpers.OpenURL(SelectedItem.Info.Result.DeletionURL);
    }

    public void OpenFile()
    {
        if (IsItemSelected && SelectedItem.IsFileExist) FileHelpers.OpenFile(SelectedItem.Info.FilePath);
    }

    public void OpenThumbnailFile()
    {
        if (IsItemSelected && SelectedItem.IsThumbnailFileExist) FileHelpers.OpenFile(SelectedItem.Info.ThumbnailFilePath);
    }

    public void OpenFolder()
    {
        if (IsItemSelected && SelectedItem.IsFileExist) FileHelpers.OpenFolderWithFile(SelectedItem.Info.FilePath);
    }

    public void TryOpen()
    {
        if (!IsItemSelected) return;

        SelectedItem.Update();

        if (SelectedItem.IsShortenedURLExist)
        {
            URLHelpers.OpenURL(SelectedItem.Info.Result.ShortenedURL);
            return;
        }

        if (SelectedItem.IsURLExist)
        {
            URLHelpers.OpenURL(SelectedItem.Info.Result.URL);
            return;
        }

        if (SelectedItem.IsFilePathValid)
        {
            FileHelpers.OpenFile(SelectedItem.Info.FilePath);
        }
    }


    #endregion Open

    #region Copy

    public void CopyURL()
    {
        if (IsItemSelected) CopyTexts(SelectedItems.Where(x => x.IsURLExist).Select(x => x.Info.Result.URL));
    }

    public void CopyShortenedURL()
    {
        if (IsItemSelected) CopyTexts(SelectedItems.Where(x => x.IsShortenedURLExist).Select(x => x.Info.Result.ShortenedURL));
    }

    public void CopyThumbnailURL()
    {
        if (IsItemSelected) CopyTexts(SelectedItems.Where(x => x.IsThumbnailURLExist).Select(x => x.Info.Result.ThumbnailURL));
    }

    public void CopyDeletionURL()
    {
        if (IsItemSelected) CopyTexts(SelectedItems.Where(x => x.IsDeletionURLExist).Select(x => x.Info.Result.DeletionURL));
    }

    public void CopyFile()
    {
        if (IsItemSelected && SelectedItem.IsFileExist) Clipboard.CopyFile(SelectedItem.Info.FilePath);
    }

    public void CopyImage()
    {
        if (IsItemSelected && SelectedItem.IsImageFile) Clipboard.CopyImageFromFile(SelectedItem.Info.FilePath);
    }

    public void CopyText()
    {
        if (IsItemSelected && SelectedItem.IsTextFile) Clipboard.CopyTextFromFile(SelectedItem.Info.FilePath);
    }

    public void CopyThumbnailFile()
    {
        if (IsItemSelected && SelectedItem.IsThumbnailFileExist) Clipboard.CopyFile(SelectedItem.Info.ThumbnailFilePath);
    }

    public void CopyThumbnailImage()
    {
        if (IsItemSelected && SelectedItem.IsThumbnailFileExist) Clipboard.CopyImageFromFile(SelectedItem.Info.ThumbnailFilePath);
    }

    public void CopyHTMLLink()
    {
        if (IsItemSelected) CopyTexts(SelectedItems.Where(x => x.IsURLExist).Select(x => parser.Parse(x.Info, UploadInfoParser.HTMLLink)));
    }

    public void CopyHTMLImage()
    {
        if (IsItemSelected) CopyTexts(SelectedItems.Where(x => x.IsImageURL).Select(x => parser.Parse(x.Info, UploadInfoParser.HTMLImage)));
    }

    public void CopyHTMLLinkedImage()
    {
        if (IsItemSelected) CopyTexts(SelectedItems.Where(x => x.IsImageURL && x.IsThumbnailURLExist).Select(x => parser.Parse(x.Info, UploadInfoParser.HTMLLinkedImage)));
    }

    public void CopyForumLink()
    {
        if (IsItemSelected) CopyTexts(SelectedItems.Where(x => x.IsURLExist).Select(x => parser.Parse(x.Info, UploadInfoParser.ForumLink)));
    }

    public void CopyForumImage()
    {
        if (IsItemSelected) CopyTexts(SelectedItems.Where(x => x.IsImageURL).Select(x => parser.Parse(x.Info, UploadInfoParser.ForumImage)));
    }

    public void CopyForumLinkedImage()
    {
        if (IsItemSelected) CopyTexts(SelectedItems.Where(x => x.IsImageURL && x.IsThumbnailURLExist).Select(x => parser.Parse(x.Info, UploadInfoParser.ForumLinkedImage)));
    }

    public void CopyMarkdownLink()
    {
        if (IsItemSelected) CopyTexts(SelectedItems.Where(x => x.IsURLExist).Select(x => parser.Parse(x.Info, UploadInfoParser.MarkdownLink)));
    }

    public void CopyMarkdownImage()
    {
        if (IsItemSelected) CopyTexts(SelectedItems.Where(x => x.IsImageURL).Select(x => parser.Parse(x.Info, UploadInfoParser.MarkdownImage)));
    }

    public void CopyMarkdownLinkedImage()
    {
        if (IsItemSelected) CopyTexts(SelectedItems.Where(x => x.IsImageURL && x.IsThumbnailURLExist).Select(x => parser.Parse(x.Info, UploadInfoParser.MarkdownLinkedImage)));
    }

    public void CopyFilePath()
    {
        if (IsItemSelected) CopyTexts(SelectedItems.Where(x => x.IsFilePathValid).Select(x => x.Info.FilePath));
    }

    public void CopyFileName()
    {
        if (IsItemSelected) CopyTexts(SelectedItems.Where(x => x.IsFilePathValid).Select(x => Path.GetFileNameWithoutExtension(x.Info.FilePath)));
    }

    public void CopyFileNameWithExtension()
    {
        if (IsItemSelected) CopyTexts(SelectedItems.Where(x => x.IsFilePathValid).Select(x => Path.GetFileName(x.Info.FilePath)));
    }

    public void CopyFolder()
    {
        if (IsItemSelected) CopyTexts(SelectedItems.Where(x => x.IsFilePathValid).Select(x => Path.GetDirectoryName(x.Info.FilePath)));
    }

    public void CopyCustomFormat(string? format)
    {
        if (!string.IsNullOrEmpty(format) && IsItemSelected) CopyTexts(SelectedItems.Where(x => x.IsURLExist).Select(x => parser.Parse(x.Info, format)));
    }

    public void TryCopy()
    {
        if (IsItemSelected)
        {
            if (SelectedItem.IsShortenedURLExist)
            {
                CopyTexts(SelectedItems.Where(x => x.IsShortenedURLExist).Select(x => x.Info.Result.ShortenedURL));
            }
            else if (SelectedItem.IsURLExist)
            {
                CopyTexts(SelectedItems.Where(x => x.IsURLExist).Select(x => x.Info.Result.URL));
            }
            else if (SelectedItem.IsFilePathValid)
            {
                CopyTexts(SelectedItems.Where(x => x.IsFilePathValid).Select(x => x.Info.FilePath));
            }
        }
    }

    #endregion Copy

    #region Other

    public void StopUpload()
    {
        if (IsItemSelected)
        {
            foreach (WorkerTask task in SelectedItems.Select(x => x.Task))
            {
                task?.Stop();
            }
        }
    }

    public void Upload()
    {
        if (IsItemSelected && SelectedItem.IsFileExist) UploadManager.UploadFile(SelectedItem.Info.FilePath);
    }

    public void Download()
    {
        if (IsItemSelected && SelectedItem.IsFileURL) UploadManager.DownloadFile(SelectedItem.Info.Result.URL);
    }


    public void DeleteFiles()
    {
        if (IsItemSelected)
        {
            foreach (string filePath in SelectedItems.Select(x => x.Info.FilePath))
            {
                FileHelpers.DeleteFile(filePath, true);
            }
        }
    }

    public void ShortenURL(UrlShortenerType urlShortener)
    {
        if (IsItemSelected && SelectedItem.IsURLExist) UploadManager.ShortenURL(SelectedItem.Info.Result.ToString(), urlShortener);
    }

    public void ShareURL(URLSharingServices urlSharingService)
    {
        if (IsItemSelected && SelectedItem.IsURLExist) UploadManager.ShareURL(SelectedItem.Info.Result.ToString(), urlSharingService);
    }

    public void SearchImageUsingGoogleLens()
    {
        if (IsItemSelected && SelectedItem.IsURLExist) TaskHelpers.SearchImageUsingGoogleLens(SelectedItem.Info.Result.URL);
    }

    public void SearchImageUsingBing()
    {
        if (IsItemSelected && SelectedItem.IsURLExist) TaskHelpers.SearchImageUsingBing(SelectedItem.Info.Result.URL);
    }

    public async Task OCRImage()
    {
        if (IsItemSelected && SelectedItem.IsImageFile) await TaskHelpers.OCRImage(SelectedItem.Info.FilePath);
    }



    #endregion Other
}

