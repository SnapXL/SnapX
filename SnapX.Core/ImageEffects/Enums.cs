// SPDX-License-Identifier: GPL-3.0-or-later


using System.ComponentModel;

namespace SnapX.Core.ImageEffects;

public enum WatermarkType
{
    Text,
    Image
}
[Flags]
public enum AnchorStyles
{
    None = 0,           // No anchor style
    Top = 1,            // Anchor to the top
    Bottom = 2,         // Anchor to the bottom
    Left = 4,           // Anchor to the left
    Right = 8,          // Anchor to the right
    TopLeft = Top | Left,        // Top-left corner
    TopRight = Top | Right,      // Top-right corner
    BottomLeft = Bottom | Left,  // Bottom-left corner
    BottomRight = Bottom | Right, // Bottom-right corner
    All = Top | Bottom | Left | Right // All sides (full anchoring)
}

public enum ResizeMode
{
    [Description("Resizes all images to the specified size.")]
    ResizeAll,
    [Description("Only resize image if it is bigger than specified size.")]
    ResizeIfBigger,
    [Description("Only resize image if it is smaller than specified size.")]
    ResizeIfSmaller
}

public enum DrawImageSizeMode // Localized
{
    DontResize,
    AbsoluteSize,
    PercentageOfWatermark,
    PercentageOfCanvas
}

public enum ImageRotateFlipType
{
    None = 0,
    Rotate90 = 1,
    Rotate180 = 2,
    Rotate270 = 3,
    FlipX = 4,
    Rotate90FlipX = 5,
    FlipY = 6,
    Rotate90FlipY = 7
}
