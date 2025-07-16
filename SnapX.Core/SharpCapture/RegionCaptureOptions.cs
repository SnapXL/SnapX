// SPDX-License-Identifier: GPL-3.0-or-later


using SixLabors.ImageSharp;
using SnapX.Core.History;
using SnapX.Core.ImageEffects;
using SnapX.Core.SharpCapture.Helpers;

namespace SnapX.Core.SharpCapture;

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

    public bool QuickCrop = true;
    public int MinimumSize = DefaultMinimumSize;
    public RegionCaptureAction RegionCaptureActionRightClick = RegionCaptureAction.RemoveShapeCancelCapture;
    public RegionCaptureAction RegionCaptureActionMiddleClick = RegionCaptureAction.SwapToolType;
    public RegionCaptureAction RegionCaptureActionX1Click = RegionCaptureAction.CaptureFullscreen;
    public RegionCaptureAction RegionCaptureActionX2Click = RegionCaptureAction.CaptureActiveMonitor;
    public bool DetectWindows = true;
    public bool DetectControls = true;
    // TEMP: For backward compatibility
    public bool UseDimming = true;
    public int BackgroundDimStrength = 10;
    public bool UseCustomInfoText = false;
    public string CustomInfoText = "X: $x, Y: $y$nR: $r, G: $g, B: $b$nHex: $hex"; // Formats: $x, $y, $r, $g, $b, $hex, $HEX, $n
    public List<SnapSize> SnapSizes =
    [
        new(426, 240),    // 240p
        new(640, 360),    // 360p
        new(854, 480),    // 480p
        new(1280, 720),   // 720p
        new(1920, 1080),  // 1080p
        new(2560, 1440),  // 1440p
        new(3840, 2160),  // 2160p
        new(5120, 2880),  // 2880p / 5K
        new(7680, 4320)   // 4320p / 8K
    ];
    public bool ShowInfo = true;
    public bool ShowMagnifier = true;
    public bool UseSquareMagnifier = false;
    public int MagnifierPixelCount = 15; // Must be odd number like 11, 13, 15, etc.
    public int MagnifierPixelSize = 10;
    public bool ShowCrosshair = false;
    public bool UseLightResizeNodes = false;
    public bool EnableAnimations = true;
    public bool IsFixedSize = false;
    public Size FixedSize = new Size(250, 250);
    public bool ShowFPS = false;
    public int FPSLimit = 100;
    public int MenuIconSize = 0;
    public bool MenuLocked = false;
    public bool RememberMenuState = false;
    public bool MenuCollapsed = false;
    public Point MenuPosition = Point.Empty;
    public int InputDelay = 500;
    public bool SwitchToDrawingToolAfterSelection = false;
    public bool SwitchToSelectionToolAfterDrawing = false;
    public bool ActiveMonitorMode = false;

    // Annotation
    // public AnnotationOptions AnnotationOptions = new();
    public ShapeType LastRegionTool = ShapeType.RegionRectangle;
    public ShapeType LastAnnotationTool = ShapeType.DrawingRectangle;
    public ShapeType LastEditorTool = ShapeType.DrawingRectangle;

    // Image editor
    public ImageEditorStartMode ImageEditorStartMode = ImageEditorStartMode.AutoSize;
    public WindowState ImageEditorWindowState = new();
    public bool ZoomToFitOnOpen = false;
    public bool EditorAutoCopyImage = false;
    public bool AutoCloseEditorOnTask = false;
    public bool ShowEditorPanTip = true;
    public ImageInterpolationMode ImageEditorResizeInterpolationMode = ImageInterpolationMode.Bicubic;
    public Size EditorNewImageSize = new(800, 600);
    public bool EditorNewImageTransparent = false;
    public Color EditorNewImageBackgroundColor = Color.White;
    public Color EditorCanvasColor = Color.Transparent;
    public List<ImageEffectPreset> ImageEffectPresets = [];
    public int SelectedImageEffectPreset = 0;

    // Screen color picker
    public string ScreenColorPickerInfoText = "";
}
