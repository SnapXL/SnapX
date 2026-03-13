
// SPDX-License-Identifier: GPL-3.0-or-later


using System.Text.Json.Serialization;
using SixLabors.ImageSharp;
using SnapX.Core.ScreenCapture.Helpers;
using SnapX.Core.Utils.Converters;

namespace SnapX.Core.ScreenCapture;

public class RegionCaptureOptions
{
    public const int DefaultMinimumSize = 5;
    public const int MagnifierPixelCountMinimum = 3;
    public const int MagnifierPixelCountMaximum = 35;
    public const int MagnifierPixelSizeMinimum = 3;
    public const int MagnifierPixelSizeMaximum = 30;
    public const int SnapDistance = 30;
    public const int MoveSpeedMinimum = 1;
    public const int MoveSpeedMaximum = 10;

    public bool QuickCrop { get; set; } = true;
    public int MinimumSize { get; set; } = DefaultMinimumSize;
    public RegionCaptureAction RegionCaptureActionRightClick { get; set; } = RegionCaptureAction.RemoveShapeCancelCapture;
    public RegionCaptureAction RegionCaptureActionMiddleClick { get; set; } = RegionCaptureAction.SwapToolType;
    public RegionCaptureAction RegionCaptureActionX1Click { get; set; } = RegionCaptureAction.CaptureFullscreen;
    public RegionCaptureAction RegionCaptureActionX2Click { get; set; } = RegionCaptureAction.CaptureActiveMonitor;
    public bool DetectWindows { get; set; } = true;
    public bool DetectControls { get; set; } = true;
    public bool UseDimming { get; set; } = true;
    public int BackgroundDimStrength { get; set; } = 10;
    public bool UseCustomInfoText { get; set; } = false;
    public string CustomInfoText { get; set; } = "X: $x, Y: $y$nR: $r, G: $g, B: $b$nHex: $hex";
    public List<SnapSize> SnapSizes { get; set; } =
    [
        new(426, 240), // 240p
            new(640, 360), // 360p
            new(854, 480), // 480p
            new(1280, 720), // 720p
            new(1920, 1080) // 1080p
    ];
    public bool ShowInfo { get; set; } = true;
    public bool ShowMagnifier { get; set; } = true;
    public bool UseSquareMagnifier { get; set; } = false;
    public int MagnifierPixelCount { get; set; } = 15;
    public int MagnifierPixelSize { get; set; } = 10;
    public bool ShowCrosshair { get; set; } = false;
    public bool UseLightResizeNodes { get; set; } = false;
    public bool EnableAnimations { get; set; } = true;
    public bool IsFixedSize { get; set; } = false;
    [JsonConverter(typeof(JsonSizeConverter))]
    public Size FixedSize { get; set; } = new(250, 250);
    public bool ShowFPS { get; set; } = false;
    public int FPSLimit { get; set; } = 100;
    public int MenuIconSize { get; set; } = 0;
    public bool MenuLocked { get; set; } = false;
    public bool RememberMenuState { get; set; } = false;
    public bool MenuCollapsed { get; set; } = false;
    [JsonConverter(typeof(JsonPointConverter))]
    public Point MenuPosition { get; set; } = Point.Empty;
    public int InputDelay { get; set; } = 500;
    public bool SwitchToDrawingToolAfterSelection { get; set; } = false;
    public bool SwitchToSelectionToolAfterDrawing { get; set; } = false;
    public bool ActiveMonitorMode { get; set; } = false;

    // Annotation
    public ShapeType LastRegionTool { get; set; } = ShapeType.RegionRectangle;
    public ShapeType LastAnnotationTool { get; set; } = ShapeType.DrawingRectangle;
    public ShapeType LastEditorTool { get; set; } = ShapeType.DrawingRectangle;

    // Image editor
    public ImageEditorStartMode ImageEditorStartMode { get; set; } = ImageEditorStartMode.AutoSize;
    public WindowState ImageEditorWindowState { get; set; } = new();
    public bool ZoomToFitOnOpen { get; set; } = false;
    public bool EditorAutoCopyImage { get; set; } = false;
    public bool AutoCloseEditorOnTask { get; set; } = false;
    public bool ShowEditorPanTip { get; set; } = true;
    public ImageInterpolationMode ImageEditorResizeInterpolationMode { get; set; } = ImageInterpolationMode.Bicubic;
    [JsonConverter(typeof(JsonSizeConverter))]
    public Size EditorNewImageSize { get; set; } = new(800, 600);
    public bool EditorNewImageTransparent { get; set; } = false;
    [JsonConverter(typeof(JsonColorConverter))]
    public Color EditorNewImageBackgroundColor { get; set; } = Color.White;
    [JsonConverter(typeof(JsonColorConverter))]
    public Color EditorCanvasColor { get; set; } = Color.Transparent;
    public int SelectedImageEffectPreset { get; set; } = 0;

    // Screen color picker
    public string ScreenColorPickerInfoText { get; set; } = "";
}
