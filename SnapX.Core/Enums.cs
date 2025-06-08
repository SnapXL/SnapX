
// SPDX-License-Identifier: GPL-3.0-or-later


using System.ComponentModel;
using SnapX.Core.Utils.Extensions;

namespace SnapX.Core;

public enum BuildType
{
    Debug,
    Release,
    Flatpak,
    Snap,
    AppImage,
    Runfile,
    RPM,
    DEB,
    PKG,
    Arch, // btw
    APK, // Not to be confused with Android. For Alpine Linux
    Unknown
}

public enum UpdateChannel // Localized
{
    Release,
    PreRelease,
    Dev
}

public enum SupportedLanguage
{
    Automatic, // Localized
    [Description("Nederlands (Dutch)")]
    Dutch,
    [Description("English")]
    English,
    [Description("Français (French)")]
    French,
    [Description("Deutsch (German)")]
    German,
    [Description("עִברִית (Hebrew)")]
    Hebrew,
    [Description("Magyar (Hungarian)")]
    Hungarian,
    [Description("Bahasa Indonesia (Indonesian)")]
    Indonesian,
    [Description("Italiano (Italian)")]
    Italian,
    [Description("日本語 (Japanese)")]
    Japanese,
    [Description("한국어 (Korean)")]
    Korean,
    [Description("Español mexicano (Mexican Spanish)")]
    MexicanSpanish,
    [Description("فارسی (Persian)")]
    Persian,
    [Description("Polski (Polish)")]
    Polish,
    [Description("Português (Portuguese)")]
    Portuguese,
    [Description("Português-Brasil (Portuguese-Brazil)")]
    PortugueseBrazil,
    [Description("Română (Romanian)")]
    Romanian,
    [Description("Русский (Russian)")]
    Russian,
    [Description("简体中文 (Simplified Chinese)")]
    SimplifiedChinese,
    [Description("Español (Spanish)")]
    Spanish,
    [Description("繁體中文 (Traditional Chinese)")]
    TraditionalChinese,
    [Description("Türkçe (Turkish)")]
    Turkish,
    [Description("Українська (Ukrainian)")]
    Ukrainian,
    [Description("Tiếng Việt (Vietnamese)")]
    Vietnamese
}

public enum TaskJob
{
    Job,
    DataUpload,
    FileUpload,
    TextUpload,
    ShortenURL,
    ShareURL,
    Download,
    DownloadUpload
}

public enum TaskStatus
{
    InQueue,
    Preparing,
    Working,
    Stopping,
    Stopped,
    Failed,
    Completed,
    History
}

[Flags]
public enum AfterCaptureTasks // Localized
{
    None = 0,
    ShowQuickTaskMenu = 1,
    ShowAfterCaptureWindow = 1 << 1,
    BeautifyImage = 1 << 2,
    AddImageEffects = 1 << 3,
    AnnotateImage = 1 << 4,
    CopyImageToClipboard = 1 << 5,
    PinToScreen = 1 << 6,
    SendImageToPrinter = 1 << 7,
    SaveImageToFile = 1 << 8,
    SaveImageToFileWithDialog = 1 << 9,
    SaveThumbnailImageToFile = 1 << 10,
    PerformActions = 1 << 11,
    CopyFileToClipboard = 1 << 12,
    CopyFilePathToClipboard = 1 << 13,
    ShowInExplorer = 1 << 14,
    ScanQRCode = 1 << 15,
    DoOCR = 1 << 16,
    ShowBeforeUploadWindow = 1 << 17,
    UploadImageToHost = 1 << 18,
    DeleteFile = 1 << 19
}

[Flags]
public enum AfterUploadTasks // Localized
{
    None = 0,
    ShowAfterUploadWindow = 1,
    UseURLShortener = 1 << 1,
    ShareURL = 1 << 2,
    CopyURLToClipboard = 1 << 3,
    OpenURL = 1 << 4,
    ShowQRCode = 1 << 5
}

public enum CaptureType
{
    Fullscreen,
    Monitor,
    ActiveMonitor,
    Window,
    ActiveWindow,
    Region,
    CustomRegion,
    LastRegion
}

public enum ScreenRecordStartMethod
{
    Region,
    ActiveWindow,
    CustomRegion,
    LastRegion
}

public enum HotkeyType // Localized
{
    None,
    // Upload
    FileUpload,
    [Category(EnumExtensions.HotkeyType_Category_Upload)]
    FolderUpload,
    [Category(EnumExtensions.HotkeyType_Category_Upload)]
    ClipboardUpload,
    [Category(EnumExtensions.HotkeyType_Category_Upload)]
    ClipboardUploadWithContentViewer,
    [Category(EnumExtensions.HotkeyType_Category_Upload)]
    UploadText,
    [Category(EnumExtensions.HotkeyType_Category_Upload)]
    UploadURL,
    [Category(EnumExtensions.HotkeyType_Category_Upload)]
    DragDropUpload,
    [Category(EnumExtensions.HotkeyType_Category_Upload)]
    ShortenURL,
    [Category(EnumExtensions.HotkeyType_Category_Upload)]
    TweetMessage,
    [Category(EnumExtensions.HotkeyType_Category_Upload)]
    StopUploads,
    // Screen capture
    [Category(EnumExtensions.HotkeyType_Category_ScreenCapture)]
    PrintScreen,
    [Category(EnumExtensions.HotkeyType_Category_ScreenCapture)]
    ActiveWindow,
    [Category(EnumExtensions.HotkeyType_Category_ScreenCapture)]
    CustomWindow,
    [Category(EnumExtensions.HotkeyType_Category_ScreenCapture)]
    ActiveMonitor,
    [Category(EnumExtensions.HotkeyType_Category_ScreenCapture)]
    RectangleRegion,
    [Category(EnumExtensions.HotkeyType_Category_ScreenCapture)]
    RectangleLight,
    [Category(EnumExtensions.HotkeyType_Category_ScreenCapture)]
    RectangleTransparent,
    [Category(EnumExtensions.HotkeyType_Category_ScreenCapture)]
    CustomRegion,
    [Category(EnumExtensions.HotkeyType_Category_ScreenCapture)]
    LastRegion,
    [Category(EnumExtensions.HotkeyType_Category_ScreenCapture)]
    ScrollingCapture,
    [Category(EnumExtensions.HotkeyType_Category_ScreenCapture)]
    AutoCapture,
    [Category(EnumExtensions.HotkeyType_Category_ScreenCapture)]
    StartAutoCapture,
    // Screen record
    [Category(EnumExtensions.HotkeyType_Category_ScreenRecord)]
    ScreenRecorder,
    [Category(EnumExtensions.HotkeyType_Category_ScreenRecord)]
    ScreenRecorderActiveWindow,
    [Category(EnumExtensions.HotkeyType_Category_ScreenRecord)]
    ScreenRecorderCustomRegion,
    [Category(EnumExtensions.HotkeyType_Category_ScreenRecord)]
    StartScreenRecorder,
    [Category(EnumExtensions.HotkeyType_Category_ScreenRecord)]
    ScreenRecorderGIF,
    [Category(EnumExtensions.HotkeyType_Category_ScreenRecord)]
    ScreenRecorderGIFActiveWindow,
    [Category(EnumExtensions.HotkeyType_Category_ScreenRecord)]
    ScreenRecorderGIFCustomRegion,
    [Category(EnumExtensions.HotkeyType_Category_ScreenRecord)]
    StartScreenRecorderGIF,
    [Category(EnumExtensions.HotkeyType_Category_ScreenRecord)]
    StopScreenRecording,
    [Category(EnumExtensions.HotkeyType_Category_ScreenRecord)]
    PauseScreenRecording,
    [Category(EnumExtensions.HotkeyType_Category_ScreenRecord)]
    AbortScreenRecording,
    // Tools
    [Category(EnumExtensions.HotkeyType_Category_Tools)]
    ColorPicker,
    [Category(EnumExtensions.HotkeyType_Category_Tools)]
    ScreenColorPicker,
    [Category(EnumExtensions.HotkeyType_Category_Tools)]
    Ruler,
    [Category(EnumExtensions.HotkeyType_Category_Tools)]
    PinToScreen,
    [Category(EnumExtensions.HotkeyType_Category_Tools)]
    PinToScreenFromScreen,
    [Category(EnumExtensions.HotkeyType_Category_Tools)]
    PinToScreenFromClipboard,
    [Category(EnumExtensions.HotkeyType_Category_Tools)]
    PinToScreenFromFile,
    [Category(EnumExtensions.HotkeyType_Category_Tools)]
    PinToScreenCloseAll,
    [Category(EnumExtensions.HotkeyType_Category_Tools)]
    ImageEditor,
    [Category(EnumExtensions.HotkeyType_Category_Tools)]
    ImageBeautifier,
    [Category(EnumExtensions.HotkeyType_Category_Tools)]
    ImageEffects,
    [Category(EnumExtensions.HotkeyType_Category_Tools)]
    ImageViewer,
    [Category(EnumExtensions.HotkeyType_Category_Tools)]
    ImageCombiner,
    [Category(EnumExtensions.HotkeyType_Category_Tools)]
    ImageSplitter,
    [Category(EnumExtensions.HotkeyType_Category_Tools)]
    ImageThumbnailer,
    [Category(EnumExtensions.HotkeyType_Category_Tools)]
    VideoConverter,
    [Category(EnumExtensions.HotkeyType_Category_Tools)]
    VideoThumbnailer,
    [Category(EnumExtensions.HotkeyType_Category_Tools)]
    OCR,
    [Category(EnumExtensions.HotkeyType_Category_Tools)]
    QRCode,
    [Category(EnumExtensions.HotkeyType_Category_Tools)]
    QRCodeDecodeFromScreen,
    [Category(EnumExtensions.HotkeyType_Category_Tools)]
    HashCheck,
    [Category(EnumExtensions.HotkeyType_Category_Tools)]
    IndexFolder,
    [Category(EnumExtensions.HotkeyType_Category_Tools)]
    ClipboardViewer,
    [Category(EnumExtensions.HotkeyType_Category_Tools)]
    BorderlessWindow,
    [Category(EnumExtensions.HotkeyType_Category_Tools)]
    ActiveWindowBorderless,
    [Category(EnumExtensions.HotkeyType_Category_Tools)]
    ActiveWindowTopMost,
    [Category(EnumExtensions.HotkeyType_Category_Tools)]
    InspectWindow,
    [Category(EnumExtensions.HotkeyType_Category_Tools)]
    MonitorTest,
    [Category(EnumExtensions.HotkeyType_Category_Tools)]
    DNSChanger,
    // Other
    [Category(EnumExtensions.HotkeyType_Category_Other)]
    DisableHotkeys,
    [Category(EnumExtensions.HotkeyType_Category_Other)]
    OpenMainWindow,
    [Category(EnumExtensions.HotkeyType_Category_Other)]
    OpenScreenshotsFolder,
    [Category(EnumExtensions.HotkeyType_Category_Other)]
    OpenHistory,
    [Category(EnumExtensions.HotkeyType_Category_Other)]
    OpenImageHistory,
    [Category(EnumExtensions.HotkeyType_Category_Other)]
    ToggleActionsToolbar,
    [Category(EnumExtensions.HotkeyType_Category_Other)]
    ToggleTrayMenu,
    [Category(EnumExtensions.HotkeyType_Category_Other)]
    ExitShareX
}


public enum ToastClickAction // Localized
{
    CloseNotification,
    AnnotateImage,
    CopyImageToClipboard,
    CopyFile,
    CopyFilePath,
    CopyUrl,
    OpenFile,
    OpenFolder,
    OpenUrl,
    Upload,
    PinToScreen
}

public enum ThumbnailViewClickAction // Localized
{
    Default,
    Select,
    OpenImageViewer,
    OpenFile,
    OpenFolder,
    OpenURL,
    EditImage
}

public enum FileExistAction // Localized
{
    Ask,
    Overwrite,
    UniqueName,
    Cancel
}

public enum ImagePreviewVisibility // Localized
{
    Show, Hide, Automatic
}

public enum ImagePreviewLocation // Localized
{
    Side, Bottom
}

public enum ThumbnailTitleLocation // Localized
{
    Top, Bottom
}

public enum RegionCaptureType
{
    Default, Light, Transparent
}

public enum StartupState
{
    Disabled,
    DisabledByUser,
    Enabled,
    DisabledByPolicy,
    EnabledByPolicy
}

public enum BalloonTipClickAction
{
    None,
    OpenURL,
    OpenDebugLog
}

public enum TaskViewMode // Localized
{
    ListView,
    ThumbnailView
}

public enum NativeMessagingAction
{
    None,
    UploadImage,
    UploadVideo,
    UploadAudio,
    UploadText,
    ShortenURL
}

public enum NotificationSound
{
    Capture,
    TaskCompleted,
    ActionCompleted,
    Error
}
public enum EDataType // Localized
{
    Default,
    File,
    Image,
    Text,
    URL
}

public enum PNGBitDepth // Localized
{
    Default,
    Automatic,
    Bit32,
    Bit24
}

public enum GIFQuality // Localized
{
    Default,
    Bit8,
    Bit4,
    Grayscale
}

public enum EImageFormat
{
    [Description("png")]
    PNG,
    [Description("jpg")]
    JPEG,
    [Description("gif")]
    GIF,
    [Description("bmp")]
    BMP,
    [Description("tif")]
    TIFF
}

public enum HashType
{
    [Description("CRC-32")]
    CRC32,
    [Description("MD5")]
    MD5,
    [Description("SHA-1")]
    SHA1,
    [Description("SHA-256")]
    SHA256,
    [Description("SHA-384")]
    SHA384,
    [Description("SHA-512")]
    SHA512
}

public enum BorderType
{
    Outside,
    Inside
}

public enum DownloaderFormStatus
{
    Waiting,
    DownloadStarted,
    DownloadCompleted,
    InstallStarted
}

public enum InstallType
{
    Default,
    Silent,
    VerySilent,
    Event
}

public enum ReleaseChannelType
{
    [Description("Stable version")]
    Stable,
    [Description("Beta version")]
    Beta,
    [Description("Dev version")]
    Dev
}

public enum UpdateStatus
{
    None,
    UpdateCheckFailed,
    UpdateAvailable,
    UpToDate
}

public enum PrintType
{
    Image,
    Text
}

public enum DrawStyle
{
    Hue,
    Saturation,
    Brightness,
    Red,
    Green,
    Blue
}

public enum ColorType
{
    None, RGBA, HSB, CMYK, Hex, Decimal
}

public enum ColorFormat
{
    RGB, RGBA, ARGB
}

public enum ProxyMethod // Localized
{
    None,
    Manual,
    Automatic
}

public enum SlashType
{
    Prefix,
    Suffix
}

public enum ScreenTearingTestMode
{
    VerticalLines,
    HorizontalLines
}

public enum HotkeyStatus
{
    Registered,
    Failed,
    NotConfigured
}

public enum ImageCombinerAlignment
{
    LeftOrTop,
    Center,
    RightOrBottom
}

public enum ImageInterpolationMode
{
    HighQualityBicubic,
    Bicubic,
    HighQualityBilinear,
    Bilinear,
    NearestNeighbor
}

public enum ArrowHeadDirection // Localized
{
    End,
    Start,
    Both
}

public enum FFmpegArchitecture
{
    win64,
    win32,
    macos64
}

public enum StepType // Localized
{
    Numbers,
    LettersUppercase,
    LettersLowercase,
    RomanNumeralsUppercase,
    RomanNumeralsLowercase
}

public enum CutOutEffectType // Localized
{
    None,
    ZigZag,
    TornEdge,
    Wave
}

