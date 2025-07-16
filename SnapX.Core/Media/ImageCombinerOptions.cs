// SPDX-License-Identifier: GPL-3.0-or-later


namespace SnapX.Core.Media;

public class ImageCombinerOptions
{
    // public Orientation Orientation { get; set; } = Orientation.Vertical;
    public ImageCombinerAlignment Alignment { get; set; } = ImageCombinerAlignment.LeftOrTop;
    public int Space { get; set; } = 0;
    public int WrapAfter { get; set; } = 0;
    public bool AutoFillBackground { get; set; } = true;
}

