// SPDX-License-Identifier: GPL-3.0-or-later


using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using SixLabors.ImageSharp;
using SnapX.Core.Utils;
using SnapX.Core.Utils.Extensions;

namespace SnapX.Core.ImageEffects.Filters;

internal class Blur : ImageEffect
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
            radius = value.Max(3);

            if (radius.IsEvenNumber())
            {
                radius++;
            }
        }
    }

    [UnconditionalSuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code", Justification = "<Pending>")]
    public Blur()
    {
        this.ApplyDefaultPropertyValues();
    }

    public override Image Apply(Image img)
    {
        ImageHelpers.BoxBlur(img, Radius);
        return img;
    }

    protected override string? GetSummary()
    {
        return Radius.ToString();
    }
}
