// SPDX-License-Identifier: GPL-3.0-or-later


using System.ComponentModel;
using SixLabors.ImageSharp;
using SnapX.Core.Utils;

namespace SnapX.Core.ImageEffects.Manipulations;

internal class Scale : ImageEffect
{
    [DefaultValue(100f),
     Description("Use width percentage as 0 to maintain aspect ratio by automatically adjusting width.")]
    public float WidthPercentage => 100f;

    [DefaultValue(0f),
     Description("Use height percentage as 0 to maintain aspect ratio by automatically adjusting height.")]
    public float HeightPercentage { get; set; } = 0f;

    public override Image Apply(Image img)
    {
        if (WidthPercentage <= 0 && HeightPercentage <= 0)
        {
            return img;
        }

        var width = (int)Math.Round(WidthPercentage / 100 * img.Width);
        var height = (int)Math.Round(HeightPercentage / 100 * img.Height);
        var size = ImageHelpers.ApplyAspectRatio(width, height, img);

        return ImageHelpers.ResizeImage(img, size);
    }

    protected override string? GetSummary()
    {
        string? summary = WidthPercentage.ToString();

        if (WidthPercentage > 0)
        {
            summary += "%";
        }

        summary += ", " + HeightPercentage.ToString();

        if (HeightPercentage > 0)
        {
            summary += "%";
        }

        return summary;
    }
}
