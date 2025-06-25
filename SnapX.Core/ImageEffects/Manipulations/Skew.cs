
// SPDX-License-Identifier: GPL-3.0-or-later


using System.ComponentModel;
using SixLabors.ImageSharp;
using SnapX.Core.Utils;

namespace SnapX.Core.ImageEffects.Manipulations;

internal class Skew : ImageEffect
{
    [DefaultValue(0), Description("How much pixel skew left to right.")]
    public int Horizontally { get; set; } = 0;

    [DefaultValue(0), Description("How much pixel skew top to bottom.")]
    public int Vertically { get; set; } = 0;

    public override Image Apply(Image img)
    {
        if (Horizontally == 0 && Vertically == 0)
        {
            return img;
        }

        return ImageHelpers.AddSkew(img, Horizontally, Vertically);
    }

    protected override string? GetSummary()
    {
        return $"{Horizontally}px, {Vertically}px";
    }
}
