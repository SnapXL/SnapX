
// SPDX-License-Identifier: GPL-3.0-or-later


using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using SixLabors.ImageSharp;
using SnapX.Core.Utils;
using SnapX.Core.Utils.Extensions;

namespace SnapX.Core.ImageEffects.Manipulations;

internal class Crop : ImageEffect
{
    private Padding margin;

    [DefaultValue(typeof(Padding), "0, 0, 0, 0")]
    public Padding Margin
    {
        get
        {
            return margin;
        }
        set
        {
            if (value.Top >= 0 && value.Right >= 0 && value.Bottom >= 0 && value.Left >= 0)
            {
                margin = value;
            }
        }
    }

    [UnconditionalSuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code", Justification = "<Pending>")]
    public Crop()
    {
        this.ApplyDefaultPropertyValues();
    }

    public override Image Apply(Image img)
    {
        if (margin.Top == 0 && margin.Left == 0 && margin.Bottom == 0 && margin.Right == 0)
        {
            return img;  // No margin to apply, return the image as is.
        }

        return ImageHelpers.CropImage(img, new Rectangle(Margin.Left, Margin.Top, img.Width - Margin.Top, img.Height - Margin.Bottom));
    }

    protected override string? GetSummary() => Margin.ToString();
}
