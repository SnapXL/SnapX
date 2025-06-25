
// SPDX-License-Identifier: GPL-3.0-or-later


using System.ComponentModel;
using SixLabors.ImageSharp;
using SnapX.Core.Utils;

namespace SnapX.Core.ImageEffects.Manipulations;

[Description("Force proportions")]
internal class ForceProportions : ImageEffect
{
    private int proportionalWidth = 1;

    [DefaultValue(1)]
    public int ProportionalWidth
    {
        get
        {
            return proportionalWidth;
        }
        set
        {
            proportionalWidth = Math.Max(1, value);
        }
    }

    private int proportionalHeight = 1;

    [DefaultValue(1)]
    public int ProportionalHeight
    {
        get
        {
            return proportionalHeight;
        }
        set
        {
            proportionalHeight = Math.Max(1, value);
        }
    }

    public enum ForceProportionsMethod
    {
        Grow,
        Crop
    }

    [DefaultValue(ForceProportionsMethod.Grow)]
    public ForceProportionsMethod Method { get; set; } = ForceProportionsMethod.Grow;

    [DefaultValue(typeof(Color), "Transparent")]
    public Color GrowFillColor { get; set; } = Color.Transparent;

    public override Image Apply(Image img)
    {
        var currentRatio = img.Width / (float)img.Height;
        var targetRatio = proportionalWidth / (float)proportionalHeight;

        var isTargetWider = targetRatio > currentRatio;

        var targetWidth = img.Width;
        var targetHeight = img.Height;
        var marginLeft = 0;
        var marginTop = 0;

        if (Method == ForceProportionsMethod.Crop)
        {
            if (isTargetWider)
            {
                targetHeight = (int)Math.Round(img.Width / targetRatio);
                marginTop = (img.Height - targetHeight) / 2;
            }
            else
            {
                targetWidth = (int)Math.Round(img.Height * targetRatio);
                marginLeft = (img.Width - targetWidth) / 2;
            }

            return ImageHelpers.CropImage(img, new Rectangle(marginLeft, marginTop, targetWidth, targetHeight));
        }
        else if (Method == ForceProportionsMethod.Grow)
        {
            if (isTargetWider)
            {
                targetWidth = (int)Math.Round(img.Height * targetRatio);
            }
            else
            {
                targetHeight = (int)Math.Round(img.Width / targetRatio);
            }

            return ImageHelpers.ResizeImage(img, new Size(targetWidth, targetHeight), false, true, GrowFillColor);
        }

        return img;
    }

    protected override string? GetSummary()
    {
        return $"{ProportionalWidth}, {ProportionalHeight}";
    }
}
