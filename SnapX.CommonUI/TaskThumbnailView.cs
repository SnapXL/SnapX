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
    private Image CreateThumbnail(string? filePath = null, Image? img = null)
    {
        if (img != null) return ImageHelpers.ResizeImage(img, ThumbnailSize, false);
        if (string.IsNullOrWhiteSpace(filePath)) filePath = Task.Info.FileName;
        else if (File.Exists(filePath)) return ImageHelpers.ResizeImage(Image.Load(filePath), ThumbnailSize, true);
        if (string.IsNullOrEmpty(filePath)) return null; // TODO: Embed error image
        var icon = Methods.GetJumboFileIcon(filePath, false);
        return ImageHelpers.ResizeImage(icon, ThumbnailSize, false, true);
    }
}
