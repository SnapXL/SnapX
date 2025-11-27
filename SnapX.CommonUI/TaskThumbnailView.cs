using System.ComponentModel;
using SixLabors.ImageSharp;
using SnapX.Core.Job;
using SnapX.Core.Utils;
using SnapX.Core.Utils.Native;

namespace SnapX.CommonUI;

public class TaskThumbnailView : INotifyPropertyChanged
{
    public TaskThumbnailView(WorkerTask task)
    {
        Task = task;
    }
    public WorkerTask Task { get; private set; }
    public event PropertyChangedEventHandler PropertyChanged;
    protected virtual void OnPropertyChanged(string propertyName) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    private bool titleVisible = true;


    public bool TitleVisible
    {
        get
        {
            return titleVisible;
        }
        set
        {
            if (titleVisible != value)
            {
                titleVisible = value;
            }
        }
    }

    public bool ThumbnailExists { get; private set; }

    private Size thumbnailSize;

    public Size ThumbnailSize
    {
        get
        {
            return thumbnailSize;
        }
        set
        {
            if (thumbnailSize != value)
            {
                thumbnailSize = value;
            }
        }
    }
    private Image? CreateThumbnail(string? filePath, Image? img = null)
    {
        if (img is not null)
            return ImageHelpers.ResizeImage(img, ThumbnailSize, false);

        filePath ??= Task.Info.FileName;
        if (string.IsNullOrWhiteSpace(filePath))
            return null;

        if (File.Exists(filePath))
        {
            if (!FileHelpers.IsVideoFile(filePath))
                return ImageHelpers.ResizeImage(Image.Load(filePath), ThumbnailSize, false);
            // var videoThumb = Methods.GetFileThumbnail(filePath, ThumbnailSize);
            // if (videoThumb is not null)
            // {
            //     if (videoThumb.Width > 64 && videoThumb.Height > 64)
            //         ImageHelpers.DrawImageCentered(videoThumb, Resources.Play);
            //     return videoThumb;
            // }
            return ImageHelpers.ResizeImage(Image.Load(filePath), ThumbnailSize, false);
        }

        using var icon = Methods.GetJumboFileIcon(filePath);
        return ImageHelpers.ResizeImage(icon, ThumbnailSize, false, true);
    }
}
