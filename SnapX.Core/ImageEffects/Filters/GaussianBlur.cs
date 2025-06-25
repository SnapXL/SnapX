
// SPDX-License-Identifier: GPL-3.0-or-later

using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using SixLabors.ImageSharp;
using SnapX.Core.Utils;
using SnapX.Core.Utils.Extensions;

namespace SnapX.Core.ImageEffects.Filters;
[Description("Gaussian blur")]
internal class GaussianBlur : ImageEffect
{
    private int radius;

    [DefaultValue(15)]
    public int Radius
    {
        get
        {
            return radius;
        }
        set
        {
            radius = Math.Max(value, 1);
        }
    }

    [UnconditionalSuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code", Justification = "<Pending>")]
    public GaussianBlur()
    {
        this.ApplyDefaultPropertyValues();
    }

    public override Image Apply(Image img)
    {
        using (img)
        {
            return ImageHelpers.GaussianBlur(img, Radius);
        }
    }

    protected override string? GetSummary()
    {
        return Radius.ToString();
    }
}
