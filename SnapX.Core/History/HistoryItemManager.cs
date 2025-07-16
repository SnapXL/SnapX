// SPDX-License-Identifier: GPL-3.0-or-later


using SnapX.Core.Utils;

namespace SnapX.Core.History;
public partial class HistoryItemManager
{
    public delegate HistoryItem[] GetHistoryItemsEventHandler();

    public event GetHistoryItemsEventHandler GetHistoryItems;

    public HistoryItem HistoryItem { get; private set; }

    public bool IsURLExist { get; private set; }
    public bool IsShortenedURLExist { get; private set; }
    public bool IsThumbnailURLExist { get; private set; }
    public bool IsDeletionURLExist { get; private set; }
    public bool IsImageURL { get; private set; }
    public bool IsTextURL { get; private set; }
    public bool IsFilePathValid { get; private set; }
    public bool IsFileExist { get; private set; }
    public bool IsImageFile { get; private set; }
    public bool IsTextFile { get; private set; }

    private Action<string> uploadFile, editImage, pinToScreen;

    public HistoryItemManager(Action<string> uploadFile, Action<string> editImage, Action<string> pinToScreen, bool hideShowMoreInfoButton = false)
    {
        this.uploadFile = uploadFile;
        this.editImage = editImage;
        this.pinToScreen = pinToScreen;
    }

    public HistoryItem UpdateSelectedHistoryItem()
    {
        HistoryItem[] historyItems = OnGetHistoryItems();

        if (historyItems != null && historyItems.Length > 0)
        {
            HistoryItem = historyItems[0];
        }
        else
        {
            HistoryItem = null;
        }
        if (HistoryItem == null) return null;


        IsURLExist = !string.IsNullOrEmpty(HistoryItem.URL);
        IsShortenedURLExist = !string.IsNullOrEmpty(HistoryItem.ShortenedURL);
        IsThumbnailURLExist = !string.IsNullOrEmpty(HistoryItem.ThumbnailURL);
        IsDeletionURLExist = !string.IsNullOrEmpty(HistoryItem.DeletionURL);
        IsImageURL = IsURLExist && FileHelpers.IsImageFile(HistoryItem.URL);
        IsTextURL = IsURLExist && FileHelpers.IsTextFile(HistoryItem.URL);
        IsFilePathValid = !string.IsNullOrEmpty(HistoryItem.FilePath) && Path.HasExtension(HistoryItem.FilePath);
        IsFileExist = IsFilePathValid && File.Exists(HistoryItem.FilePath);
        IsImageFile = IsFileExist && FileHelpers.IsImageFile(HistoryItem.FilePath);
        IsTextFile = IsFileExist && FileHelpers.IsTextFile(HistoryItem.FilePath);


        return HistoryItem;
    }

    public HistoryItem[] OnGetHistoryItems()
    {
        if (GetHistoryItems != null)
        {
            return GetHistoryItems();
        }

        return null;
    }


    public void OpenURL()
    {
        if (HistoryItem != null && IsURLExist) URLHelpers.OpenURL(HistoryItem.URL);
    }

    public void OpenShortenedURL()
    {
        if (HistoryItem != null && IsShortenedURLExist) URLHelpers.OpenURL(HistoryItem.ShortenedURL);
    }

    public void OpenThumbnailURL()
    {
        if (HistoryItem != null && IsThumbnailURLExist) URLHelpers.OpenURL(HistoryItem.ThumbnailURL);
    }

    public void OpenDeletionURL()
    {
        if (HistoryItem != null && IsDeletionURLExist) URLHelpers.OpenURL(HistoryItem.DeletionURL);
    }

    public void OpenFile()
    {
        if (HistoryItem != null && IsFileExist) FileHelpers.OpenFile(HistoryItem.FilePath);
    }

    public void OpenFolder()
    {
        if (HistoryItem != null && IsFileExist) FileHelpers.OpenFolderWithFile(HistoryItem.FilePath);
    }

    public void TryOpen()
    {
        if (HistoryItem != null)
        {
            if (IsShortenedURLExist)
            {
                URLHelpers.OpenURL(HistoryItem.ShortenedURL);
            }
            else if (IsURLExist)
            {
                URLHelpers.OpenURL(HistoryItem.URL);
            }
            else if (IsFileExist)
            {
                FileHelpers.OpenFile(HistoryItem.FilePath);
            }
        }
    }

    public void CopyURL()
    {
        HistoryItem[] historyItems = OnGetHistoryItems();
        if (historyItems != null)
        {
            string[] array = historyItems.Where(x => x != null && !string.IsNullOrEmpty(x.URL)).Select(x => x.URL).ToArray();

            if (array != null && array.Length > 0)
            {
                string urls = string.Join("\r\n", array);

                if (!string.IsNullOrEmpty(urls))
                {
                    throw new NotImplementedException("CopyURL is not implemented");
                }
            }
        }
    }

    public void CopyShortenedURL()
    {
        HistoryItem[] historyItems = OnGetHistoryItems();
        if (historyItems != null)
        {
            string[] array = historyItems.Where(x => x != null && !string.IsNullOrEmpty(x.ShortenedURL)).Select(x => x.ShortenedURL).ToArray();

            if (array != null && array.Length > 0)
            {
                string shortenedURLs = string.Join("\r\n", array);

                if (!string.IsNullOrEmpty(shortenedURLs))
                {
                    throw new NotImplementedException("CopyShortenedURL is not implemented");
                }
            }
        }
    }

    public void CopyThumbnailURL()
    {
        HistoryItem[] historyItems = OnGetHistoryItems();
        if (historyItems != null)
        {
            string[] array = historyItems.Where(x => x != null && !string.IsNullOrEmpty(x.ThumbnailURL)).Select(x => x.ThumbnailURL).ToArray();

            if (array != null && array.Length > 0)
            {
                string thumbnailURLs = string.Join("\r\n", array);

                if (!string.IsNullOrEmpty(thumbnailURLs))
                {
                    throw new NotImplementedException("CopyThumbnailURL is not implemented");
                }
            }
        }
    }

    public void CopyDeletionURL()
    {
        HistoryItem[] historyItems = OnGetHistoryItems();
        if (historyItems != null)
        {
            string[] array = historyItems.Where(x => x != null && !string.IsNullOrEmpty(x.DeletionURL)).Select(x => x.DeletionURL).ToArray();

            if (array != null && array.Length > 0)
            {
                string deletionURLs = string.Join("\r\n", array);

                if (!string.IsNullOrEmpty(deletionURLs))
                {
                    throw new NotImplementedException("CopyDeletionURL is not implemented");
                }
            }
        }
    }

    public void CopyFile()
    {
        HistoryItem[] historyItems = OnGetHistoryItems();
        if (historyItems != null)
        {
            string[] array = historyItems.Where(x => x != null && !string.IsNullOrEmpty(x.FilePath) && Path.HasExtension(x.FilePath) &&
                System.IO.File.Exists(x.FilePath)).Select(x => x.FilePath).ToArray();

            if (array != null && array.Length > 0)
            {
                throw new NotImplementedException("CopyFile is not implemented");
            }
        }
    }

    public void CopyImage()
    {
        if (HistoryItem != null && IsImageFile) throw new NotImplementedException("CopyImage is not implemented");

    }

    public void CopyText()
    {
        if (HistoryItem != null && IsTextFile) throw new NotImplementedException("");

    }

    public void CopyHTMLLink()
    {
        HistoryItem[] historyItems = OnGetHistoryItems();
        if (historyItems != null)
        {
            string[] array = historyItems.Where(x => x != null && !string.IsNullOrEmpty(x.URL)).
                Select(x => string.Format("<a href=\"{0}\">{0}</a>", x.URL)).ToArray();

            if (array != null && array.Length > 0)
            {
                string htmlLinks = string.Join("\r\n", array);

                if (!string.IsNullOrEmpty(htmlLinks))
                {
                    throw new NotImplementedException("CopyHTMLLink is not implemented");

                }
            }
        }
    }

    public void CopyHTMLImage()
    {
        HistoryItem[] historyItems = OnGetHistoryItems();
        if (historyItems != null)
        {
            string[] array = historyItems.Where(x => x != null && !string.IsNullOrEmpty(x.URL) && FileHelpers.IsImageFile(x.URL)).
                Select(x => string.Format("<img src=\"{0}\"/>", x.URL)).ToArray();

            if (array != null && array.Length > 0)
            {
                string htmlImages = string.Join("\r\n", array);

                if (!string.IsNullOrEmpty(htmlImages))
                {
                    throw new NotImplementedException("CopyHTMLImage is not implemented");
                }
            }
        }
    }

    public void CopyHTMLLinkedImage()
    {
        HistoryItem[] historyItems = OnGetHistoryItems();
        if (historyItems != null)
        {
            string[] array = historyItems.Where(x => x != null && !string.IsNullOrEmpty(x.URL) && FileHelpers.IsImageFile(x.URL) &&
                !string.IsNullOrEmpty(x.ThumbnailURL)).Select(x => string.Format("<a href=\"{0}\"><img src=\"{1}\"/></a>", x.URL, x.ThumbnailURL)).ToArray();

            if (array != null && array.Length > 0)
            {
                string htmlLinkedImages = string.Join("\r\n", array);

                if (!string.IsNullOrEmpty(htmlLinkedImages))
                {
                    throw new NotImplementedException("CopyHTMLLinkedImage is not implemented");
                }
            }
        }
    }

    public void CopyForumLink()
    {
        HistoryItem[] historyItems = OnGetHistoryItems();
        if (historyItems != null)
        {
            string[] array = historyItems.Where(x => x != null && !string.IsNullOrEmpty(x.URL)).Select(x => string.Format("[url]{0}[/url]", x.URL)).ToArray();

            if (array != null && array.Length > 0)
            {
                string forumLinks = string.Join("\r\n", array);

                if (!string.IsNullOrEmpty(forumLinks))
                {
                    throw new NotImplementedException("CopyForumLink is not implemented");
                }
            }
        }
    }

    public void CopyForumImage()
    {
        HistoryItem[] historyItems = OnGetHistoryItems();
        if (historyItems != null)
        {
            string[] array = historyItems.Where(x => x != null && !string.IsNullOrEmpty(x.URL) && FileHelpers.IsImageFile(x.URL)).
                Select(x => string.Format("[img]{0}[/img]", x.URL)).ToArray();

            if (array != null && array.Length > 0)
            {
                string forumImages = string.Join("\r\n", array);

                if (!string.IsNullOrEmpty(forumImages))
                {
                    throw new NotImplementedException("CopyForumImage is not implemented");
                }
            }
        }
    }

    public void CopyForumLinkedImage()
    {
        HistoryItem[] historyItems = OnGetHistoryItems();
        if (historyItems != null)
        {
            string[] array = historyItems.Where(x => x != null && !string.IsNullOrEmpty(x.URL) && FileHelpers.IsImageFile(x.URL) &&
                !string.IsNullOrEmpty(x.ThumbnailURL)).Select(x => string.Format("[url={0}][img]{1}[/img][/url]", x.URL, x.ThumbnailURL)).ToArray();

            if (array != null && array.Length > 0)
            {
                string forumLinkedImages = string.Join("\r\n", array);

                if (!string.IsNullOrEmpty(forumLinkedImages))
                {
                    throw new NotImplementedException("CopyForumLinkedImage is not implemented");
                }
            }
        }
    }

    public void CopyMarkdownLink()
    {
        HistoryItem[] historyItems = OnGetHistoryItems();
        if (historyItems != null)
        {
            string[] array = historyItems.Where(x => x != null && !string.IsNullOrEmpty(x.URL)).
                Select(x => string.Format("[{0}]({1})", x.FileName, x.URL)).ToArray();

            if (array != null && array.Length > 0)
            {
                string markdownLinks = string.Join("\r\n", array);

                if (!string.IsNullOrEmpty(markdownLinks))
                {
                    throw new NotImplementedException("CopyMarkdownLink is not implemented");
                }
            }
        }
    }

    public void CopyMarkdownImage()
    {
        HistoryItem[] historyItems = OnGetHistoryItems();
        if (historyItems != null)
        {
            string[] array = historyItems.Where(x => x != null && !string.IsNullOrEmpty(x.URL) && FileHelpers.IsImageFile(x.URL)).
                Select(x => string.Format("![{0}]({1})", x.FileName, x.URL)).ToArray();

            if (array != null && array.Length > 0)
            {
                string markdownImages = string.Join("\r\n", array);

                if (!string.IsNullOrEmpty(markdownImages))
                {
                    throw new NotImplementedException("CopyMarkdownImage is not implemented");
                }
            }
        }
    }

    public void CopyMarkdownLinkedImage()
    {
        HistoryItem[] historyItems = OnGetHistoryItems();
        if (historyItems != null)
        {
            string[] array = historyItems.Where(x => x != null && !string.IsNullOrEmpty(x.URL) && FileHelpers.IsImageFile(x.URL) &&
                !string.IsNullOrEmpty(x.ThumbnailURL)).Select(x => string.Format("[![{0}]({1})]({2})", x.FileName, x.ThumbnailURL, x.URL)).ToArray();

            if (array != null && array.Length > 0)
            {
                string markdownLinkedImages = string.Join("\r\n", array);

                if (!string.IsNullOrEmpty(markdownLinkedImages))
                {
                    throw new NotImplementedException("CopyMarkdownLinkedImage is not implemented");
                }
            }
        }
    }

    public void CopyFilePath()
    {
        HistoryItem[] historyItems = OnGetHistoryItems();
        if (historyItems != null)
        {
            string[] array = historyItems.Where(x => x != null && !string.IsNullOrEmpty(x.FilePath) && Path.HasExtension(x.FilePath) &&
                System.IO.File.Exists(x.FilePath)).Select(x => x.FilePath).ToArray();

            if (array != null && array.Length > 0)
            {
                string filePaths = string.Join("\r\n", array);

                if (!string.IsNullOrEmpty(filePaths))
                {
                    throw new NotImplementedException("CopyFilePath is not implemented");
                }
            }
        }
    }

    public void CopyFileName()
    {
        HistoryItem[] historyItems = OnGetHistoryItems();
        if (historyItems != null)
        {
            string[] array = historyItems.Where(x => x != null && !string.IsNullOrEmpty(x.FilePath) && Path.HasExtension(x.FilePath)).
                Select(x => Path.GetFileNameWithoutExtension(x.FilePath)).ToArray();

            if (array != null && array.Length > 0)
            {
                string fileNames = string.Join("\r\n", array);

                if (!string.IsNullOrEmpty(fileNames))
                {
                    throw new NotImplementedException("CopyFileName is not implemented");
                }
            }
        }
    }

    public void CopyFileNameWithExtension()
    {
        HistoryItem[] historyItems = OnGetHistoryItems();
        if (historyItems != null)
        {
            string[] array = historyItems.Where(x => x != null && !string.IsNullOrEmpty(x.FilePath) && Path.HasExtension(x.FilePath)).
                Select(x => Path.GetFileName(x.FilePath)).ToArray();

            if (array != null && array.Length > 0)
            {
                string fileNamesWithExtension = string.Join("\r\n", array);

                if (!string.IsNullOrEmpty(fileNamesWithExtension))
                {
                    throw new NotImplementedException("CopyFileNameWithExtension is not implemented");
                }
            }
        }
    }

    public void CopyFolder()
    {
        HistoryItem[] historyItems = OnGetHistoryItems();
        if (historyItems != null)
        {
            string[] array = historyItems.Where(x => x != null && !string.IsNullOrEmpty(x.FilePath) && Path.HasExtension(x.FilePath)).
                Select(x => Path.GetDirectoryName(x.FilePath)).ToArray();

            if (array != null && array.Length > 0)
            {
                string folderPaths = string.Join("\r\n", array);

                if (!string.IsNullOrEmpty(folderPaths))
                {
                    throw new NotImplementedException("CopyFolder is not implemented");
                }
            }
        }
    }

    public void ShowImagePreview()
    {
        if (HistoryItem != null && IsImageFile) throw new NotImplementedException("ShowImagePreview is not implemented");

    }

    public void UploadFile()
    {
        if (uploadFile != null && HistoryItem != null && IsFileExist) uploadFile(HistoryItem.FilePath);
    }

    public void EditImage()
    {
        if (editImage != null && HistoryItem != null && IsImageFile) editImage(HistoryItem.FilePath);
    }

    public void PinToScreen()
    {
        if (pinToScreen != null && HistoryItem != null && IsImageFile) pinToScreen(HistoryItem.FilePath);
    }

    public void ShowMoreInfo()
    {
        throw new NotImplementedException("ShowMoreInfo is not implemented");
    }
}

