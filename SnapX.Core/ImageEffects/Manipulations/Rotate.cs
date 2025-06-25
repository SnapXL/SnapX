
// SPDX-License-Identifier: GPL-3.0-or-later


using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using SixLabors.ImageSharp;
using SnapX.Core.Utils;
using SnapX.Core.Utils.Extensions;

namespace SnapX.Core.ImageEffects.Manipulations;

internal class Rotate : ImageEffect
{
    [DefaultValue(0f), Description("Choose a value between -360 and 360.")]
    public float Angle { get; set; }

    [DefaultValue(true), Description("If true, output image will be larger than the input and no clipping will occur.")]
    public bool Upsize { get; set; }

    [DefaultValue(false), Description("Upsize must be false for this setting to work. If true, clipping will occur or else image size will be reduced.")]
    public bool Clip { get; set; }

    [UnconditionalSuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code", Justification = "<Pending>")]
    public Rotate()
    {
        this.ApplyDefaultPropertyValues();
    }

    public override Image Apply(Image img)
    {
        if (Angle == 0)
        {
            return img;
        }

        using (img)
        {
            return ImageHelpers.RotateImage(img, Angle, Upsize, Clip);
        }
    }

    protected override string? GetSummary()
    {
        return Angle + "°";
    }
}
