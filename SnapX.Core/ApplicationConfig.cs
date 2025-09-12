using System.ComponentModel;
using System.Text.Json.Serialization;
using SixLabors.ImageSharp;
using SnapX.Core.History;
using SnapX.Core.ImageEffects;
using SnapX.Core.Job;
using SnapX.Core.Utils;
using SnapX.Core.Utils.Converters;
using SnapX.Core.Utils.Miscellaneous;

namespace SnapX.Core;

public class WindowState
{
    [JsonConverter(typeof(JsonPointConverter))]
    public Point Location { get; set; }
    [JsonConverter(typeof(JsonSizeConverter))]
    public Size Size { get; set; }
    public bool IsMaximized { get; set; }
}

public class ApplicationConfig : SettingsBase<ApplicationConfig>
{
    public TaskSettings DefaultTaskSettings { get; set; } = new();
    public DateTime FirstTimeRunDate { get; set; } = DateTime.Now;
    public string FileUploadDefaultDirectory { get; set; } = "";
    public int NameParserAutoIncrementNumber { get; set; } = 0;
    public List<QuickTaskInfo> QuickTaskPresets { get; set; } = QuickTaskInfo.DefaultPresets;
    // Main window
    public bool FirstTimeMinimizeToTray { get; set; } = true;
    public List<int> TaskListViewColumnWidths { get; set; } = [];
    public int PreviewSplitterDistance { get; set; } = 335;
    public SupportedLanguage Language { get; set; } = SupportedLanguage.Automatic;
    public bool ShowTray { get; set; } = true;
    public bool SilentRun { get; set; } = false;
    public bool TrayIconProgressEnabled { get; set; } = true;
    public bool TaskbarProgressEnabled { get; set; } = true;
    public bool UseWhiteShareXIcon { get; set; } = false;
    public bool RememberMainFormSize { get; set; } = false;
    public bool RememberMainFormPosition { get; set; } = true;
    [JsonConverter(typeof(JsonPointConverter))]
    public Point MainFormPosition { get; set; } = Point.Empty;
    [JsonConverter(typeof(JsonSizeConverter))]
    public Size MainFormSize { get; set; } = Size.Empty;
    public HotkeyType TrayLeftClickAction { get; set; } = HotkeyType.RectangleRegion;
    public HotkeyType TrayLeftDoubleClickAction { get; set; } = HotkeyType.OpenMainWindow;
    public HotkeyType TrayMiddleClickAction { get; set; } = HotkeyType.ClipboardUploadWithContentViewer;
    public bool AutoCheckUpdate { get; set; } = true;
    public UpdateChannel UpdateChannel { get; set; } = UpdateChannel.Release;
    // TEMP: For backward compatibility
    public bool CheckPreReleaseUpdates { get; set; } = false;
    public bool UseCustomTheme { get; set; }
    public List<Theme> Themes { get; set; } = Theme.GetDefaultThemes();
    public int SelectedTheme { get; set; }
    public bool UseCustomScreenshotsPath { get; set; } = false;
    public string? CustomScreenshotsPath { get; set; } = "";
    public string? SaveImageSubFolderPattern { get; set; } = "%y-%mo";
    public string? SaveImageSubFolderPatternWindow { get; set; } = "";
    public bool ShowMenu { get; set; } = true;
    public TaskViewMode TaskViewMode { get; set; } = TaskViewMode.ThumbnailView;
    public bool ShowThumbnailTitle { get; set; } = true;
    [JsonConverter(typeof(JsonSizeConverter))]
    public Size ThumbnailSize { get; set; } = new(200, 150);
    public ThumbnailViewClickAction ThumbnailClickAction { get; set; } = ThumbnailViewClickAction.Default;
    public bool ShowColumns { get; set; } = true;
    public ImagePreviewVisibility ImagePreview { get; set; } = ImagePreviewVisibility.Automatic;
    public ImagePreviewLocation ImagePreviewLocation { get; set; } = ImagePreviewLocation.Side;
    public bool AutoCleanupBackupFiles { get; set; } = false;
    public bool AutoCleanupLogFiles { get; set; } = false;
    public int CleanupKeepFileCount { get; set; } = 10;
    public ProxyInfo ProxySettings { get; set; } = new();
    public int UploadLimit { get; set; } = 5;
    public int BufferSizePower { get; set; } = 5;
    public List<string> ClipboardContentFormats { get; set; } = [];
    public int MaxUploadFailRetry { get; set; } = 1;
    public bool UseSecondaryUploaders { get; set; } = false;
    public List<Upload.ImageDestination> SecondaryImageUploaders { get; set; } = [];
    public List<Upload.TextDestination> SecondaryTextUploaders { get; set; } = [];
    public List<Upload.FileDestination> SecondaryFileUploaders { get; set; } = [];
    public bool HistorySaveTasks { get; set; } = true;
    public bool HistoryCheckURL { get; set; } = false;
    public HistorySettings HistorySettings { get; set; } = new();
    public ImageHistorySettings ImageHistorySettings { get; set; } = new();
    public bool DontShowPrintSettingsDialog { get; set; }
    // public PrintSettings PrintSettings { get; set; }
    [JsonConverter(typeof(JsonRectangleConverter))]
    public Rectangle AutoCaptureRegion { get; set; } = Rectangle.Empty;
    public decimal AutoCaptureRepeatTime { get; set; } = 60;
    public bool AutoCaptureMinimizeToTray { get; set; } = true;
    public bool AutoCaptureWaitUpload { get; set; } = true;
    [JsonConverter(typeof(JsonRectangleConverter))]
    public Rectangle ScreenRecordRegion { get; set; } = Rectangle.Empty;
    public List<HotkeyType> ActionsToolbarList { get; set; } = [ HotkeyType.RectangleRegion, HotkeyType.PrintScreen, HotkeyType.ScreenRecorder,
        HotkeyType.None, HotkeyType.FileUpload, HotkeyType.ClipboardUploadWithContentViewer ];
    public bool ActionsToolbarRunAtStartup { get; set; } = false;
    [JsonConverter(typeof(JsonPointConverter))]
    public Point ActionsToolbarPosition { get; set; } = Point.Empty;
    public bool ActionsToolbarLockPosition { get; set; } = false;
    public bool ActionsToolbarStayTopMost { get; set; } = true;
    [Category("Application"), DefaultValue(true), Description("Uses your GPU to render the UI, slightly increases memory usage")]
    public bool HardwareAccelerated { get; set; } = true;
    public List<Color> RecentColors { get; set; } = [];
    [Category("Application"), DefaultValue(false), Description("Calculate and show file sizes in binary units (KiB, MiB etc.)")]
    public bool BinaryUnits { get; set; }
    //
    [Category("Application"), DefaultValue(false), Description("Show most recent task first in main window.")]
    public bool ShowMostRecentTaskFirst { get; set; }
    //
    [Category("Application"), DefaultValue(false), Description("Show only customized tasks in main window workflows.")]
    public bool WorkflowsOnlyShowEdited { get; set; }
    //
    [Category("Application"), DefaultValue(false), Description("Automatically expand capture menu when you open the tray menu.")]
    public bool TrayAutoExpandCaptureMenu { get; set; }
    [Category("Application"), DefaultValue(false), Description("Prevent the application from logging to a file")]
    public bool DisableLogging { get; set; }

    [Category("Application"), DefaultValue(false),
     Description("Application crash analytics and usage analytics that are anonymized.")]
    public bool DisableTelemetry { get; set; } = false;
    //
    [Category("Application"), DefaultValue(true), Description("Show tips and hotkeys in main window when task list is empty.")]
    public bool ShowMainWindowTip { get; set; }
    //
    [Category("Application"), DefaultValue(""),
     Description("Browser path for your favorite browser for SnapX Web Extension.")]
    public string BrowserPath = "";
    //
    //
    [Category("Application"), DefaultValue(false),
     Description("Save settings after task completed but only if there is no other active tasks.")]
    public bool SaveSettingsAfterTaskCompleted { get; set; } = false;
    //
    [Category("Application"), DefaultValue(false),
     Description("In main window when task is completed automatically select it.")]
    public bool AutoSelectLastCompletedTask { get; set; } = false;
    //
    [Category("Application"), DefaultValue(false), Description("Ultra secret mode.")]
    public bool DevMode
    {
        get
        {
#if DEBUG
            return true;
#else
            return false;
#endif
        }
    }
    //
    [Category("Hotkey"), DefaultValue(false), Description("Disables hotkeys.")]
    public bool DisableHotkeys { get; set; }
    //
    [Category("Hotkey"), DefaultValue(false), Description("If active window is fullscreen then hotkeys won't be executed.")]
    public bool DisableHotkeysOnFullscreen { get; set; }
    //
    private int hotkeyRepeatLimit;
    //
    [Category("Hotkey"), DefaultValue(500), Description("If you hold hotkeys then it will only trigger every this milliseconds.")]
    public int HotkeyRepeatLimit
    {
        get
        {
            return hotkeyRepeatLimit;
        }
        set
        {
            hotkeyRepeatLimit = Math.Max(value, 200);
        }
    }
    [Category("Clipboard"), DefaultValue(true), Description("Show clipboard content viewer when using clipboard upload in main window.")]
    public bool ShowClipboardContentViewer { get; set; }
    //
    [Category("Image"), DefaultValue(false), Description("Strip color space information chunks from PNG image.")]
    public bool PNGStripColorSpaceInformation { get; set; }
    //
    [Category("Image"), DefaultValue(true), Description("If JPEG exif contains orientation data then rotate image accordingly.")]
    public bool RotateImageByExifOrientationData { get; set; }
    //
    [Category("Upload"), DefaultValue(false), Description("Can be used to disable uploading application wide.")]
    public bool DisableUpload { get; set; }
    //
    [Category("Upload"), DefaultValue(false), Description("Accept invalid SSL certificates when uploading.")]
    public bool AcceptInvalidSSLCertificates { get; set; }
    //
    [Category("Upload"), DefaultValue(true), Description("Ignore emojis while URL encoding upload results.")]
    public bool URLEncodeIgnoreEmoji { get; set; }
    //
    [Category("Upload"), DefaultValue(true), Description("Show first time upload warning.")]
    public bool ShowUploadWarning { get; set; }
    //
    [Category("Upload"), DefaultValue(true), Description("Show more than 10 files upload warning.")]
    public bool ShowMultiUploadWarning { get; set; }
    //
    [Category("Upload"), DefaultValue(100), Description("Large file size defined in MB. SnapX will warn before uploading large files. 0 disables this feature.")]
    public int ShowLargeFileSizeWarning { get; set; }
    //
    [Category("Paths"),
     Description(
         "Custom uploaders configuration path. If you have already configured this setting in another device and you are attempting to use the same location, then backup the file before configuring this setting and restore after exiting SnapX.")]
    public string? CustomUploadersConfigPath { get; set; } = "";
    //
    [Category("Paths"), Description("Custom hotkeys configuration path. If you have already configured this setting in another device and you are attempting to use the same location, then backup the file before configuring this setting and restore after exiting SnapX.")]
    public string? CustomHotkeysConfigPath { get; set; } = "";
    [Category("Paths"), Description("Custom screenshot path (secondary location). If custom screenshot path is temporarily unavailable (e.g. network share), SnapX will use this location (recommended to be a local path).")]
    public string? CustomScreenshotsPath2 { get; set; } = "";
    //
    [Category("Drag and drop window"), DefaultValue(150), Description("Size of drop window.")]
    public int DropSize { get; set; }

    [Category("Drag and drop window"), DefaultValue(5), Description("Position offset of drop window.")]
    public int DropOffset { get; set; }
    [Category("Drag and drop window"), DefaultValue(100), Description("Opacity of drop window.")]
    public int DropOpacity { get; set; }

    [Category("Drag and drop window"), DefaultValue(255), Description("When you drag file to drop window then opacity will change to this.")]
    public int DropHoverOpacity { get; set; }
    [Category("Drag and drop window"), DefaultValue(ContentAlignment.BottomRight), Description("Where drop window will open.")]
    public ContentAlignment DropAlignment { get; set; }

    public string? SQLitePath { get; set; }
}

