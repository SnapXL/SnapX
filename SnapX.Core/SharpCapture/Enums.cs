
// SPDX-License-Identifier: GPL-3.0-or-later


using System.ComponentModel;

namespace SnapX.Core.SharpCapture;

public enum ScreenRecordOutput
{
    [Description("FFmpeg")]
    FFmpeg,
    [Description("Animated GIF")]
    GIF
}

public enum ScreenRecordGIFEncoding // Localized
{
    FFmpeg,
    NET,
    OctreeQuantizer
}

public enum RegionResult
{
    Close,
    Region,
    LastRegion,
    Fullscreen,
    Monitor,
    ActiveMonitor,
    AnnotateRunAfterCaptureTasks,
    AnnotateContinueTask,
    AnnotateCancelTask
}

public enum NodeType
{
    None,
    Rectangle,
    Line,
    Point,
    Freehand
}

internal enum NodePosition
{
    TopLeft,
    Top,
    TopRight,
    Right,
    BottomRight,
    Bottom,
    BottomLeft,
    Left,
    Extra
}

internal enum NodeShape
{
    Square,
    Circle,
    Diamond,
    CustomNode
}

public enum FFmpegVideoCodec
{
    [Description("H.264 / x264")]
    libx264,
    [Description("H.265 / x265")]
    libx265,
    [Description("VP8")]
    libvpx,
    [Description("VP9")]
    libvpx_vp9,
    [Description("MPEG-4 / Xvid")]
    libxvid,
    [Description("H.264 / NVENC")]
    h264_nvenc,
    [Description("HEVC / NVENC")]
    hevc_nvenc,
    [Description("H.264 / AMF")]
    h264_amf,
    [Description("HEVC / AMF")]
    hevc_amf,
    [Description("H.264 / Quick Sync")]
    h264_qsv,
    [Description("HEVC / Quick Sync")]
    hevc_qsv,
    [Description("GIF")]
    gif,
    [Description("WebP")]
    libwebp,
    [Description("APNG")]
    apng
}

public enum FFmpegAudioCodec
{
    [Description("AAC")]
    libvoaacenc,
    [Description("Opus")]
    libopus,
    [Description("Vorbis")]
    libvorbis,
    [Description("MP3")]
    libmp3lame
}

public enum FFmpegPreset
{
    [Description("Ultra fast")]
    ultrafast,
    [Description("Super fast")]
    superfast,
    [Description("Very fast")]
    veryfast,
    [Description("Faster")]
    faster,
    [Description("Fast")]
    fast,
    [Description("Medium")]
    medium,
    [Description("Slow")]
    slow,
    [Description("Slower")]
    slower,
    [Description("Very slow")]
    veryslow,
    [Description("Placebo")]
    placebo
}

public enum FFmpegTune
{
    film,
    animation,
    grain,
    stillimage,
    psnr,
    ssim,
    fastdecode,
    zerolatency
}

public enum FFmpegNVENCPreset
{
    [Description("Fastest (Lowest quality)")]
    p1,
    [Description("Faster (Lower quality)")]
    p2,
    [Description("Fast (Low quality)")]
    p3,
    [Description("Medium (Medium quality)")]
    p4,
    [Description("Slow (Good quality)")]
    p5,
    [Description("Slower (Better quality)")]
    p6,
    [Description("Slowest (Best quality)")]
    p7
}

public enum FFmpegNVENCTune
{
    [Description("High quality")]
    hq,
    [Description("Low latency")]
    ll,
    [Description("Ultra low latency")]
    ull,
    [Description("Lossless")]
    lossless
}

public enum FFmpegAMFUsage
{
    [Description("Generic transcoding")]
    transcoding,
    [Description("Ultra low latency transcoding")]
    ultralowlatency,
    [Description("Low latency transcoding")]
    lowlatency,
    [Description("Webcam")]
    webcam,
    [Description("High quality transcoding")]
    high_quality,
    [Description("Low latency yet high quality transcoding")]
    lowlatency_high_quality
}

public enum FFmpegAMFQuality
{
    [Description("Prefer speed")]
    speed,
    [Description("Balanced")]
    balanced,
    [Description("Prefer quality")]
    quality
}

public enum FFmpegQSVPreset
{
    [Description("Very fast")]
    veryfast,
    [Description("Faster")]
    faster,
    [Description("Fast")]
    fast,
    [Description("Medium")]
    medium,
    [Description("Slow")]
    slow,
    [Description("Slower")]
    slower,
    [Description("Very slow")]
    veryslow
}

public enum FFmpegPaletteGenStatsMode
{
    full,
    diff,
    single
}

public enum FFmpegPaletteUseDither
{
    none,
    bayer,
    heckbert,
    floyd_steinberg,
    sierra2,
    sierra2_4a,
    sierra3,
    burkes,
    atkinson
}

public enum RegionCaptureMode
{
    Default,
    Annotation,
    ScreenColorPicker,
    Ruler,
    OneClick,
    Editor,
    TaskEditor
}

public enum RegionCaptureAction // Localized
{
    None,
    CancelCapture,
    RemoveShapeCancelCapture,
    RemoveShape,
    SwapToolType,
    CaptureFullscreen,
    CaptureActiveMonitor,
    CaptureLastRegion
}

public enum ShapeCategory
{
    Region,
    Drawing,
    Effect,
    Tool
}

public enum ShapeType // Localized
{
    RegionRectangle,
    RegionEllipse,
    RegionFreehand,
    ToolSelect,
    DrawingRectangle,
    DrawingEllipse,
    DrawingFreehand,
    DrawingFreehandArrow,
    DrawingLine,
    DrawingArrow,
    DrawingTextOutline,
    DrawingTextBackground,
    DrawingSpeechBalloon,
    DrawingStep,
    DrawingMagnify,
    DrawingImage,
    DrawingImageScreen,
    DrawingSticker,
    DrawingCursor,
    DrawingSmartEraser,
    EffectBlur,
    EffectPixelate,
    EffectHighlight,
    ToolCrop,
    ToolCutOut
}

public enum ImageEditorStartMode // Localized
{
    AutoSize,
    Normal,
    Maximized,
    PreviousState,
    Fullscreen
}

public enum ImageInsertMethod
{
    None,
    Center,
    CanvasExpandDown,
    CanvasExpandRight
}

public enum BorderStyle // Localized
{
    Solid,
    Dash,
    Dot,
    DashDot,
    DashDotDot
}

public enum ScreenRecordState
{
    Waiting,
    BeforeStart,
    AfterStart,
    AfterRecordingStart,
    RecordingEnd,
    Encoding
}

public enum ScreenRecordingStatus
{
    Waiting,
    Working,
    Recording,
    Paused,
    Stopped,
    Aborted
}

public enum ScrollingCaptureStatus
{
    Failed,
    PartiallySuccessful,
    Successful
}
