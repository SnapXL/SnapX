
// SPDX-License-Identifier: GPL-3.0-or-later


using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using SnapX.Core.Utils.Extensions;

namespace SnapX.Core.Media;

public class VideoThumbnailOptions
{
    [Category("Thumbnails"), DefaultValue(ThumbnailLocationType.DefaultFolder), Description("Create thumbnails in default screenshot folder, same folder as the media file or in a custom folder.")]
    public ThumbnailLocationType OutputLocation { get; set; }

    [Category("Thumbnails"), DefaultValue(""), Description("Output folder where thumbnails will get saved.")]
    public string? CustomOutputDirectory { get; set; }

    [Category("Thumbnails"), DefaultValue(EImageFormat.PNG), Description("Thumbnail image format to save.")]
    public EImageFormat ImageFormat { get; set; }

    [Category("Thumbnails"), DefaultValue(9), Description("Total number of thumbnails to take.")]
    public int ThumbnailCount { get; set; }

    [Category("Thumbnails"), DefaultValue("_Thumbnail"), Description("Suffix to append to the thumbnail filename.")]
    public string FilenameSuffix { get; set; }

    [Category("Thumbnails"), DefaultValue(false), Description("Choose random frame each time a media file is processed.")]
    public bool RandomFrame { get; set; }

    [Category("Thumbnails"), DefaultValue(true), Description("Upload thumbnails.")]
    public bool UploadThumbnails { get; set; }

    [Category("Thumbnails"), DefaultValue(false), Description("After combine thumbnails keep single image files.")]
    public bool KeepScreenshots { get; set; }

    [Category("Thumbnails"), DefaultValue(false), Description("After all thumbnails taken open output directory automatically.")]
    public bool OpenDirectory { get; set; }

    [Category("Thumbnails"), DefaultValue(512), Description("Maximum thumbnail width size, 0 means don't resize.")]
    public int MaxThumbnailWidth { get; set; }

    [Category("Thumbnails / Combined"), DefaultValue(true), Description("Combine all thumbnails to one large thumbnail.")]
    public bool CombineScreenshots { get; set; }

    [Category("Thumbnails / Combined"), DefaultValue(10), Description("Space between border and content as pixel.")]
    public int Padding { get; set; }

    [Category("Thumbnails / Combined"), DefaultValue(10), Description("Space between thumbnails as pixel.")]
    public int Spacing { get; set; }

    [Category("Thumbnails / Combined"), DefaultValue(3), Description("Number of thumbnails per row.")]
    public int ColumnCount { get; set; }

    [Category("Thumbnails / Combined"), DefaultValue(true), Description("Add video information to the combined thumbnail.")]
    public bool AddVideoInfo { get; set; }

    [Category("Thumbnails / Combined"), DefaultValue(true), Description("Add timestamp of thumbnail at corner of image.")]
    public bool AddTimestamp { get; set; }

    [Category("Thumbnails / Combined"), DefaultValue(true), Description("Draw rectangle shadow behind thumbnails.")]
    public bool DrawShadow { get; set; }

    [Category("Thumbnails / Combined"), DefaultValue(true), Description("Draw border around thumbnails.")]
    public bool DrawBorder { get; set; }

    public string? DefaultOutputDirectory;
    public string LastVideoPath;

    [UnconditionalSuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code", Justification = "<Pending>")]
    public VideoThumbnailOptions()
    {
        this.ApplyDefaultPropertyValues();
    }
}
