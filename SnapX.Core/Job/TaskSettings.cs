
// SPDX-License-Identifier: GPL-3.0-or-later


using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SnapX.Core.ImageEffects;
using SnapX.Core.Indexer;
using SnapX.Core.Media;
using SnapX.Core.ScreenCapture;
using SnapX.Core.ScreenCapture.ScreenRecording;
using SnapX.Core.Upload;
using SnapX.Core.Utils;
using SnapX.Core.Utils.Converters;
using SnapX.Core.Utils.Extensions;
using SnapX.Core.Utils.Miscellaneous;
using SnapX.Core.Watch;

namespace SnapX.Core.Job;
public class TaskSettings
{
    [JsonIgnore]
    public TaskSettings TaskSettingsReference { get; private set; }

    [JsonIgnore]
    public bool IsSafeTaskSettings => TaskSettingsReference != null;

    public string Description { get; set; } = "";

    public HotkeyType Job { get; set; } = HotkeyType.None;

    public bool UseDefaultAfterCaptureJob { get; set; } = true;
    public AfterCaptureTasks AfterCaptureJob { get; set; } = AfterCaptureTasks.CopyImageToClipboard | AfterCaptureTasks.SaveImageToFile | AfterCaptureTasks.UploadImageToHost;

    public bool UseDefaultAfterUploadJob { get; set; } = true;
    public AfterUploadTasks AfterUploadJob { get; set; } = AfterUploadTasks.CopyURLToClipboard;

    public bool UseDefaultDestinations { get; set; } = true;
    public ImageDestination ImageDestination { get; set; } = ImageDestination.CustomImageUploader;
    public FileDestination ImageFileDestination { get; set; } = FileDestination.Dropbox;
    public TextDestination TextDestination { get; set; } = TextDestination.Pastebin;
    public FileDestination TextFileDestination { get; set; } = FileDestination.Dropbox;
    public FileDestination FileDestination { get; set; } = FileDestination.Dropbox;
    public UrlShortenerType URLShortenerDestination { get; set; } = UrlShortenerType.BITLY;
    public URLSharingServices URLSharingServiceDestination { get; set; } = URLSharingServices.Twitter;

    public bool OverrideFTP { get; set; } = false;
    public int FTPIndex { get; set; } = 0;

    public bool OverrideCustomUploader { get; set; } = false;
    public int CustomUploaderIndex { get; set; } = 0;

    public bool OverrideScreenshotsFolder { get; set; } = false;
    public string? ScreenshotsFolder { get; set; } = "";

    public bool UseDefaultGeneralSettings { get; set; } = true;
    public TaskSettingsGeneral GeneralSettings { get; set; } = new();

    public bool UseDefaultImageSettings { get; set; } = true;
    public TaskSettingsImage ImageSettings { get; set; } = new();

    [JsonIgnore]
    public TaskSettingsImage ImageSettingsReference
    {
        get
        {
            if (UseDefaultImageSettings)
            {
                return SnapX.DefaultTaskSettings.ImageSettings;
            }

            return TaskSettingsReference.ImageSettings;
        }
    }

    public bool UseDefaultCaptureSettings { get; set; } = true;
    public TaskSettingsCapture CaptureSettings { get; set; } = new();

    [JsonIgnore]
    public TaskSettingsCapture CaptureSettingsReference
    {
        get
        {
            if (UseDefaultCaptureSettings)
            {
                return SnapX.DefaultTaskSettings.CaptureSettings;
            }

            return TaskSettingsReference.CaptureSettings;
        }
    }

    public bool UseDefaultUploadSettings { get; set; } = true;
    public TaskSettingsUpload UploadSettings { get; set; } = new();

    public bool UseDefaultActions { get; set; } = true;
    public List<ExternalProgram> ExternalPrograms { get; set; } = [];

    public bool UseDefaultToolsSettings { get; set; } = true;
    public TaskSettingsTools ToolsSettings { get; set; } = new();

    [JsonIgnore]
    public TaskSettingsTools ToolsSettingsReference
    {
        get
        {
            if (UseDefaultToolsSettings)
            {
                return SnapX.DefaultTaskSettings.ToolsSettings;
            }

            return TaskSettingsReference.ToolsSettings;
        }
    }

    public bool UseDefaultAdvancedSettings = true;
    public TaskSettingsAdvanced AdvancedSettings = new TaskSettingsAdvanced();

    public bool WatchFolderEnabled = false;
    public List<WatchFolderSettings> WatchFolderList = [];

    public override string ToString()
    {
        return !string.IsNullOrEmpty(Description) ? Description : Job.GetLocalizedDescription();
    }

    public bool IsUsingDefaultSettings
    {
        get
        {
            return UseDefaultAfterCaptureJob && UseDefaultAfterUploadJob && UseDefaultDestinations && !OverrideFTP && !OverrideCustomUploader &&
                !OverrideScreenshotsFolder && UseDefaultGeneralSettings && UseDefaultImageSettings && UseDefaultCaptureSettings && UseDefaultUploadSettings &&
                UseDefaultActions && UseDefaultToolsSettings && UseDefaultAdvancedSettings && !WatchFolderEnabled;
        }
    }

    public static TaskSettings GetDefaultTaskSettings()
    {
        TaskSettings taskSettings = new TaskSettings();
        taskSettings.SetDefaultSettings();
        taskSettings.TaskSettingsReference = SnapX.DefaultTaskSettings;
        return taskSettings;
    }

    public static TaskSettings GetSafeTaskSettings(TaskSettings taskSettings)
    {
        TaskSettings safeTaskSettings;

        if (taskSettings.IsUsingDefaultSettings && SnapX.DefaultTaskSettings != null)
        {
            safeTaskSettings = SnapX.DefaultTaskSettings.Copy();
            safeTaskSettings.Description = taskSettings.Description;
            safeTaskSettings.Job = taskSettings.Job;
        }
        else
        {
            safeTaskSettings = taskSettings.Copy();
            safeTaskSettings.SetDefaultSettings();
        }

        safeTaskSettings.TaskSettingsReference = taskSettings;
        return safeTaskSettings;
    }

    public void SetDefaultSettings()
    {
        if (SnapX.DefaultTaskSettings != null)
        {
            TaskSettings defaultTaskSettings = SnapX.DefaultTaskSettings.Copy();

            if (UseDefaultAfterCaptureJob)
            {
                AfterCaptureJob = defaultTaskSettings.AfterCaptureJob;
            }

            if (UseDefaultAfterUploadJob)
            {
                AfterUploadJob = defaultTaskSettings.AfterUploadJob;
            }

            if (UseDefaultDestinations)
            {
                ImageDestination = defaultTaskSettings.ImageDestination;
                ImageFileDestination = defaultTaskSettings.ImageFileDestination;
                TextDestination = defaultTaskSettings.TextDestination;
                TextFileDestination = defaultTaskSettings.TextFileDestination;
                FileDestination = defaultTaskSettings.FileDestination;
                URLShortenerDestination = defaultTaskSettings.URLShortenerDestination;
                URLSharingServiceDestination = defaultTaskSettings.URLSharingServiceDestination;
            }

            if (UseDefaultGeneralSettings)
            {
                GeneralSettings = defaultTaskSettings.GeneralSettings;
            }

            if (UseDefaultImageSettings)
            {
                ImageSettings = defaultTaskSettings.ImageSettings;
            }

            if (UseDefaultCaptureSettings)
            {
                CaptureSettings = defaultTaskSettings.CaptureSettings;
            }

            if (UseDefaultUploadSettings)
            {
                UploadSettings = defaultTaskSettings.UploadSettings;
            }

            if (UseDefaultActions)
            {
                ExternalPrograms = defaultTaskSettings.ExternalPrograms;
            }

            if (UseDefaultToolsSettings)
            {
                ToolsSettings = defaultTaskSettings.ToolsSettings;
            }

            if (UseDefaultAdvancedSettings)
            {
                AdvancedSettings = defaultTaskSettings.AdvancedSettings;
            }
        }
    }

    public void Cleanup()
    {
        if (UseDefaultGeneralSettings)
        {
            GeneralSettings = null;
        }

        if (UseDefaultImageSettings)
        {
            ImageSettings = null;
        }

        if (UseDefaultCaptureSettings)
        {
            CaptureSettings = null;
        }

        if (UseDefaultUploadSettings)
        {
            UploadSettings = null;
        }

        if (UseDefaultActions)
        {
            ExternalPrograms = null;
        }

        if (UseDefaultToolsSettings)
        {
            ToolsSettings = null;
        }

        if (UseDefaultAdvancedSettings)
        {
            AdvancedSettings = null;
        }
    }

    public FileDestination GetFileDestinationByDataType(EDataType dataType)
    {
        switch (dataType)
        {
            case EDataType.Image:
                return ImageFileDestination;
            case EDataType.Text:
                return TextFileDestination;
            default:
            case EDataType.File:
                return FileDestination;
        }
    }
}

public class TaskSettingsGeneral
{
    #region General / Notifications

    public bool PlaySoundAfterCapture { get; set; } = true;
    public bool PlaySoundAfterUpload { get; set; } = true;
    public bool PlaySoundAfterAction { get; set; } = true;
    // Should be named ShowNotificationAfterTaskCompleted
    // but I don't want to break COMPAT
    public bool ShowToastNotificationAfterTaskCompleted { get; set; } = true;
    // Native operating system prompt will definitely support left click action
    public ToastClickAction ToastWindowLeftClickAction { get; set; } = ToastClickAction.OpenUrl;
    // Not so sure about this one, chief.
    // public ToastClickAction ToastWindowRightClickAction = ToastClickAction.CloseNotification;
    // Get out
    // public ToastClickAction ToastWindowMiddleClickAction = ToastClickAction.AnnotateImage;
    public bool DisableNotificationsOnFullscreen { get; set; } = false;
    public bool UseCustomCaptureSound { get; set; } = false;
    public string CustomCaptureSoundPath { get; set; } = "";
    public bool UseCustomTaskCompletedSound { get; set; } = false;
    public string CustomTaskCompletedSoundPath { get; set; } = "";
    public bool UseCustomActionCompletedSound { get; set; } = false;
    public string CustomActionCompletedSoundPath { get; set; } = "";
    public bool UseCustomErrorSound { get; set; } = false;
    public string CustomErrorSoundPath { get; set; } = "";

    #endregion
}
public class ServiceLink
{
    public string Name { get; set; }
    public string URL { get; set; }

    public ServiceLink(string name, string url)
    {
        Name = name;
        URL = url;
    }

    public string GenerateLink(string input)
    {
        if (!string.IsNullOrEmpty(input))
        {
            string encodedInput = URLHelpers.URLEncode(input);
            return string.Format(URL, encodedInput);
        }

        return null;
    }

    public void OpenLink(string input)
    {
        string link = GenerateLink(input);

        if (!string.IsNullOrEmpty(link))
        {
            URLHelpers.OpenURL(link);
        }
    }

    public override string ToString()
    {
        return Name;
    }
}
public class OCROptions
{
    public string Language { get; set; } = "en";
    public float ScaleFactor { get; set; } = 2f;
    public bool SingleLine { get; set; } = false;
    public bool Silent { get; set; } = false;
    public bool AutoCopy { get; set; } = false;
    public List<ServiceLink> ServiceLinks { get; set; } = DefaultServiceLinks;
    public bool CloseWindowAfterOpeningServiceLink { get; set; } = false;
    public int SelectedServiceLink { get; set; } = 0;

    public static List<ServiceLink> DefaultServiceLinks => new List<ServiceLink>()
    {
        new ServiceLink("Google Translate", "https://translate.google.com/?sl=auto&tl=en&text={0}&op=translate"),
        new ServiceLink("Google Search", "https://www.google.com/search?q={0}"),
        new ServiceLink("Google Images", "https://www.google.com/search?q={0}&tbm=isch"),
        new ServiceLink("Bing", "https://www.bing.com/search?q={0}"),
        new ServiceLink("DuckDuckGo", "https://duckduckgo.com/?q={0}"),
        new ServiceLink("DeepL", "https://www.deepl.com/translator#auto/en/{0}")
    };
}
public class TaskSettingsImage
{
    #region Image / General

    public EImageFormat ImageFormat { get; set; } = EImageFormat.PNG;
    public PNGBitDepth ImagePNGBitDepth { get; set; } = PNGBitDepth.Default;
    public int ImageJPEGQuality { get; set; } = 90;
    public GIFQuality ImageGIFQuality { get; set; } = GIFQuality.Default;
    public bool ImageAutoUseJPEG { get; set; } = true;
    public int ImageAutoUseJPEGSize { get; set; } = 2048;
    public bool ImageAutoJPEGQuality { get; set; } = false;
    public FileExistAction FileExistAction { get; set; } = FileExistAction.Ask;

    #endregion Image / General

    #region Image / Effects

    // public List<ImageEffectPreset> ImageEffectPresets { get; set; } = [ImageEffectPreset.GetDefaultPreset()];
    public int SelectedImageEffectPreset { get; set; } = 0;

    public bool ShowImageEffectsWindowAfterCapture { get; set; } = false;
    public bool ImageEffectOnlyRegionCapture { get; set; } = false;
    public bool UseRandomImageEffect { get; set; } = false;

    #endregion Image / Effects

    #region Image / Thumbnail

    public int ThumbnailWidth { get; set; } = 200;
    public int ThumbnailHeight { get; set; } = 0;
    public string ThumbnailName { get; set; } = "-thumbnail";
    public bool ThumbnailCheckSize { get; set; } = false;

    #endregion Image / Thumbnail
}

public class TaskSettingsCapture
{
    #region Capture / General

    public bool ShowCursor { get; set; } = true;
    public decimal ScreenshotDelay { get; set; } = 0;
    public bool CaptureTransparent { get; set; } = false;
    public bool CaptureShadow { get; set; } = true;
    public int CaptureShadowOffset { get; set; } = 100;
    public bool CaptureClientArea { get; set; } = false;
    public bool CaptureAutoHideTaskbar { get; set; } = false;
    [JsonConverter(typeof(JsonRectangleConverter))]
    public Rectangle CaptureCustomRegion { get; set; } = Rectangle.Empty;
    public string CaptureCustomWindow { get; set; } = "";

    #endregion Capture / General

    #region Capture / Region capture


    #endregion Capture / Region capture

    #region Capture / Screen recorder

    public FFmpegOptions FFmpegOptions { get; set; } = new();
    public int ScreenRecordFPS { get; set; } = 30;
    public int GIFFPS { get; set; } = 15;
    public RegionCaptureOptions SurfaceOptions { get; set; } = new();
    public bool ScreenRecordShowCursor { get; set; } = true;
    public bool ScreenRecordAutoStart { get; set; } = true;
    public float ScreenRecordStartDelay { get; set; } = 0f;
    public bool ScreenRecordFixedDuration { get; set; } = false;
    public float ScreenRecordDuration { get; set; } = 3f;
    public bool ScreenRecordTwoPassEncoding { get; set; } = false;
    public bool ScreenRecordAskConfirmationOnAbort { get; set; } = false;
    public bool ScreenRecordTransparentRegion { get; set; } = false;

    #endregion Capture / Screen recorder

    #region Capture / Scrolling capture

    // public ScrollingCaptureOptions ScrollingCaptureOptions { get; set; } = new ScrollingCaptureOptions();

    #endregion Capture / Scrolling capture

    #region Capture / OCR

    public OCROptions OCROptions { get; set; } = new OCROptions();

    #endregion Capture / OCR
}

public class TaskSettingsUpload
{
    #region Upload / File naming

    public bool UseCustomTimeZone { get; set; } = false;
    public TimeZoneInfo CustomTimeZone { get; set; } = TimeZoneInfo.Utc;
    public string? NameFormatPattern { get; set; } = "%ra{10}";
    public string? NameFormatPatternActiveWindow { get; set; } = "%pn_%ra{10}";
    public bool FileUploadUseNamePattern { get; set; } = false;
    public bool FileUploadReplaceProblematicCharacters { get; set; } = false;
    public bool URLRegexReplace { get; set; } = false;
    public string URLRegexReplacePattern { get; set; } = "^https?://(.+)$";
    public string URLRegexReplaceReplacement { get; set; } = "https://$1";

    #endregion Upload / File naming

    #region Upload / Clipboard upload

    public bool ClipboardUploadURLContents { get; set; } = false;
    public bool ClipboardUploadShortenURL { get; set; } = false;
    public bool ClipboardUploadShareURL { get; set; } = false;
    public bool ClipboardUploadAutoIndexFolder { get; set; } = false;

    #endregion Upload / Clipboard upload

    #region Upload / Uploader filters

    public List<UploaderFilter> UploaderFilters { get; set; } = [];

    #endregion Upload / Uploader filters
}

public class ImageBeautifierOptions
{
    public int Margin { get; set; }
    public int Padding { get; set; }
    public bool SmartPadding { get; set; }
    public int RoundedCorner { get; set; }
    public int ShadowRadius { get; set; }
    public int ShadowOpacity { get; set; }
    public int ShadowDistance { get; set; }
    public int ShadowAngle { get; set; }
    [JsonConverter(typeof(JsonColorConverter))]
    public Color ShadowColor { get; set; }
    public ImageBeautifierBackgroundType BackgroundType { get; set; }
    public BackgroundGradient BackgroundGradient { get; set; }
    [JsonConverter(typeof(JsonColorConverter))]
    public Color BackgroundColor { get; set; }
    public string BackgroundImageFilePath { get; set; }
    public ImageBeautifierOptions()
    {
        ResetOptions();
    }

    public void ResetOptions()
    {
        Margin = 80;
        Padding = 40;
        SmartPadding = true;
        RoundedCorner = 20;
        ShadowRadius = 30;
        ShadowOpacity = 80;
        ShadowDistance = 10;
        ShadowAngle = 180;
        ShadowColor = Color.Black;
        BackgroundType = ImageBeautifierBackgroundType.Gradient;
        // BackgroundGradient = new GradientInfo(LinearGradientMode.ForwardDiagonal, Color.FromArgb(255, 81, 47), Color.FromArgb(221, 36, 118));
        // BackgroundColor = Color.FromArgb(34, 34, 34);
        BackgroundImageFilePath = "";
    }
}
public class BackgroundGradient
{
    public string Type { get; set; }
    public List<GradientStop> Colors { get; set; }
    [JsonIgnore]
    public bool IsValid => Colors != null && Colors.Count > 0;

    [JsonIgnore]
    public bool IsVisible => IsValid && Colors.Any(x => x.Color.ToPixel<Rgba32>().A > 0);

    [JsonIgnore]
    public bool IsTransparent => IsValid && Colors.Any(x => x.Color.IsTransparent());
    public BackgroundGradient(string type)
    {
        Type = type;
        Colors = new();
    }
    public BackgroundGradient()
    {
        Type = "Vertical";
        Colors = new();
    }
    public BackgroundGradient(string type, params GradientStop[] colors) : this(type)
    {
        Colors = colors.ToList();
    }
    public BackgroundGradient(string type, params Color[] colors) : this(type)
    {
        for (int i = 0; i < colors.Length; i++)
        {
            Colors.Add(new GradientStop(colors[i], (int)Math.Round(100f / (colors.Length - 1) * i)));
        }
    }
    public void Clear()
    {
        Colors.Clear();
    }

    public void Sort()
    {
        Colors.Sort((x, y) => x.Location.CompareTo(y.Location));
    }

    public void Reverse()
    {
        Colors.Reverse();

        foreach (var color in Colors)
        {
            color.Location = 100 - color.Location;
        }
    }
}
public class GradientStop
{
    [JsonConverter(typeof(JsonColorConverter))]
    public Color Color { get; set; }
    public double Location { get; set; }
    public GradientStop()
    {
        Color = default; // or Color.Transparent if you prefer
        Location = 0;
    }

    public GradientStop(Color stopColor, double location)
    {
        Color = stopColor;
        Location = location;
    }
}
public class PinToScreenOptions
{
    public int InitialScale { get; set; } = 100;
    public int ScaleStep { get; set; } = 10;
    public bool HighQualityScale { get; set; } = true;
    public int InitialOpacity { get; set; } = 100;
    public int OpacityStep { get; set; } = 10;
    public ContentAlignment Placement { get; set; } = ContentAlignment.BottomRight;
    public int PlacementOffset { get; set; } = 10;
    public bool TopMost { get; set; } = true;
    public bool KeepCenterLocation { get; set; } = true;
    [JsonConverter(typeof(JsonColorConverter))]
    public Color BackgroundColor { get; set; } = Color.White;
    public bool Shadow { get; set; } = true;
    public bool Border { get; set; } = true;
    public int BorderSize { get; set; } = 2;
    [JsonConverter(typeof(JsonColorConverter))]
    public Color BorderColor { get; set; } = Color.CornflowerBlue;
    [JsonConverter(typeof(JsonSizeConverter))]
    public Size MinimizeSize { get; set; } = new Size(100, 100);
}
public class BorderlessWindowSettings
{
    public bool RememberWindowTitle { get; set; }
    public string WindowTitle { get; set; }
    public bool AutoCloseWindow { get; set; }
    public bool ExcludeTaskbarArea { get; set; }
}

public class TaskSettingsTools
{
    public string ScreenColorPickerFormat { get; set; } = "$hex";
    public string ScreenColorPickerFormatCtrl { get; set; } = "$r255, $g255, $b255";
    public string ScreenColorPickerInfoText { get; set; } = "RGB: $r255, $g255, $b255$nHex: $hex$nX: $x Y: $y";
    public PinToScreenOptions PinToScreenOptions { get; set; } = new();
    public IndexerSettings IndexerSettings { get; set; } = new();
    public ImageBeautifierOptions ImageBeautifierOptions { get; set; } = new();
    public ImageCombinerOptions ImageCombinerOptions { get; set; } = new();
    public VideoConverterOptions VideoConverterOptions { get; set; } = new();
    public VideoThumbnailOptions VideoThumbnailOptions { get; set; } = new();
    public BorderlessWindowSettings BorderlessWindowSettings { get; set; } = new();
}

public class TaskSettingsAdvanced
{
    [Category("General"), DefaultValue(false), Description("Allow after capture tasks for image files by loading them as bitmap when files are handled during file upload, clipboard file upload, drag && drop file upload, watch folder and other image file tasks.")]
    public bool ProcessImagesDuringFileUpload { get; set; }

    [Category("General"), DefaultValue(false), Description("Use after capture tasks for clipboard image uploads.")]
    public bool ProcessImagesDuringClipboardUpload { get; set; }

    [Category("General"), DefaultValue(false), Description("Use after capture tasks for browser extension image uploads.")]
    public bool ProcessImagesDuringExtensionUpload { get; set; }

    [Category("General"), DefaultValue(true), Description("Allows file related after capture tasks (\"Perform actions\", \"Copy file to clipboard\" etc.) to be used when doing file upload.")]
    public bool UseAfterCaptureTasksDuringFileUpload { get; set; }

    [Category("General"), DefaultValue(true), Description("Save text as file for tasks such as clipboard text upload, drag and drop text upload, index folder etc.")]
    public bool TextTaskSaveAsFile { get; set; }

    [Category("General"), DefaultValue(false), Description("If task contains upload job then this setting will clear clipboard when task start.")]
    public bool AutoClearClipboard { get; set; }

    [Category("Capture"), DefaultValue(false), Description("Disable annotation support in region capture.")]
    public bool RegionCaptureDisableAnnotation { get; set; }

    [Category("Upload"), Description("Files with these file extensions will be uploaded using image uploader.")]
    public List<string> ImageExtensions { get; set; }

    [Category("Upload"), Description("Files with these file extensions will be uploaded using text uploader.")]
    public List<string> TextExtensions { get; set; }

    [Category("Upload"), DefaultValue(false), Description("Copy URL before start upload. Only works for FTP, FTPS, SFTP, Amazon S3, Google Cloud Storage and Azure Storage.")]
    public bool EarlyCopyURL { get; set; }

    [Category("Upload text"), DefaultValue("txt"), Description("File extension when saving text to the local hard disk.")]
    public string TextFileExtension { get; set; }

    [Category("Upload text"), DefaultValue("text"), Description("Text format e.g. csharp, cpp, etc.")]
    public string TextFormat { get; set; }

    [Category("Upload text"), DefaultValue(""), Description("Custom text input. Use %input for text input. Example you can create web page with your text in it.")]
    public string TextCustom { get; set; }

    [Category("Upload text"), DefaultValue(true), Description("HTML encode custom text input.")]
    public bool TextCustomEncodeInput { get; set; }

    [Category("After upload"), DefaultValue(false), Description("If result URL starts with \"http://\" then replace it with \"https://\".")]
    public bool ResultForceHTTPS { get; set; }

    [Category("After upload"), DefaultValue("$result"),
    Description("Clipboard content format after uploading. Supported variables: $result, $url, $shorturl, $thumbnailurl, $deletionurl, $filepath, $filename, $filenamenoext, $folderpath, $foldername, $uploadtime and other variables such as %y-%mo-%d etc.")]
    public string? ClipboardContentFormat { get; set; }

    [Category("After upload"), DefaultValue("$result"), Description("Balloon tip content format after uploading. Supported variables: $result, $url, $shorturl, $thumbnailurl, $deletionurl, $filepath, $filename, $filenamenoext, $folderpath, $foldername, $uploadtime and other variables such as %y-%mo-%d etc.")]
    public string? BalloonTipContentFormat { get; set; }

    [Category("After upload"), DefaultValue("$result"), Description("After upload task \"Open URL\" format. Supported variables: $result, $url, $shorturl, $thumbnailurl, $deletionurl, $filepath, $filename, $filenamenoext, $folderpath, $foldername, $uploadtime and other variables such as %y-%mo-%d etc.")]
    public string? OpenURLFormat { get; set; }

    [Category("After upload"), DefaultValue(0), Description("Automatically shorten URL if the URL is longer than the specified number of characters. 0 means automatic URL shortening is not active.")]
    public int AutoShortenURLLength { get; set; }

    [Category("After upload"), DefaultValue(false), Description("After upload form will be automatically closed after 60 seconds.")]
    public bool AutoCloseAfterUploadForm { get; set; }

    [Category("Name pattern"), DefaultValue(100), Description("Maximum name pattern length for file name.")]
    public int NamePatternMaxLength { get; set; }

    [Category("Name pattern"), DefaultValue(50), Description("Maximum name pattern title (%t) length for file name.")]
    public int NamePatternMaxTitleLength { get; set; }

    [UnconditionalSuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code", Justification = "<Pending>")]
    public TaskSettingsAdvanced()
    {
        this.ApplyDefaultPropertyValues();
        ImageExtensions = FileHelpers.ImageFileExtensions.ToList();
        TextExtensions = FileHelpers.TextFileExtensions.ToList();
    }
}
