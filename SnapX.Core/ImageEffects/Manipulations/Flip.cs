// SPDX-License-Identifier: GPL-3.0-or-later


using System.ComponentModel;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;

namespace SnapX.Core.ImageEffects.Manipulations;
public enum FlipType
{
    None,      // No flip (RotateNoneFlipNone)
    Horizontal, // Flip horizontally (RotateNoneFlipX)
    Vertical,   // Flip vertically (RotateNoneFlipY)
    Both        // Flip both horizontally and vertically (RotateNoneFlipXY)
}
internal class Flip : ImageEffect
{
    [DefaultValue(false)] public bool Horizontally { get; set; } = false;

    [DefaultValue(false)] public bool Vertically { get; set; } = false;

    public override Image Apply(Image img)
    {
        var flipType = FlipType.None;

        if (Horizontally && Vertically)
        {
            flipType = FlipType.Both;  // RotateNoneFlipXY
        }
        else if (Horizontally)
        {
            flipType = FlipType.Horizontal;  // RotateNoneFlipX
        }
        else if (Vertically)
        {
            flipType = FlipType.Vertical;  // RotateNoneFlipY
        }

        if (flipType != FlipType.None)
        {
            switch (flipType)
            {
                case FlipType.Horizontal:
                    img.Mutate(ctx => ctx.Flip(FlipMode.Horizontal));
                    break;
                case FlipType.Vertical:
                    img.Mutate(ctx => ctx.Flip(FlipMode.Vertical));
                    break;
                case FlipType.Both:
                    img.Mutate(ctx =>
                    {
                        ctx.Flip(FlipMode.Horizontal)
                            .Flip(FlipMode.Vertical);
                    });
                    break;
                case FlipType.None:
                default:
                    // No flip needed, do nothing
                    break;
            }
        }

        return img;
    }

    protected override string? GetSummary()
    {
        return $"{Horizontally}, {Vertically}";
    }
}
