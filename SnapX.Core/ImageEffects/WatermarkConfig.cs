// SPDX-License-Identifier: GPL-3.0-or-later


using SixLabors.ImageSharp;
using SnapX.Core.ImageEffects.Drawings;

namespace SnapX.Core.ImageEffects;

public class WatermarkConfig
{
    public WatermarkType Type = WatermarkType.Text;
    public AnchorStyles Placement = AnchorStyles.BottomRight;
    public int Offset = 5;
    public DrawText Text = new() { DrawTextShadow = false };
    public DrawImage Image = new();

    public Image Apply(Image img)
    {
        Text.Placement = Image.Placement = Placement;
        Text.Offset = Image.Offset = new Point(Offset, Offset);

        switch (Type)
        {
            default:
            case WatermarkType.Text:
                return Text.Apply(img);
            case WatermarkType.Image:
                return Image.Apply(img);
        }
    }
}
