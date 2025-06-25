// SPDX-License-Identifier: GPL-3.0-or-later


using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using SixLabors.ImageSharp;
using SnapX.Core.Utils;
using SnapX.Core.Utils.Extensions;

namespace SnapX.Core.ImageEffects.Filters;

[Description("Color depth")]
internal class ColorDepth : ImageEffect
{
    private int bitsPerChannel;

    [DefaultValue(4)]
    public int BitsPerChannel
    {
        get
        {
            return bitsPerChannel;
        }
        set
        {
            bitsPerChannel = MathHelpers.Clamp(value, 1, 8);
        }
    }

    [UnconditionalSuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code", Justification = "<Pending>")]
    public ColorDepth()
    {
        this.ApplyDefaultPropertyValues();
    }

    public override Image Apply(Image img)
    {
        ImageHelpers.ColorDepth(img, BitsPerChannel);
        return img;
    }

    protected override string? GetSummary()
    {
        string? summary = BitsPerChannel + " bit";

        if (BitsPerChannel > 1)
        {
            summary += "s";
        }

        return summary;
    }
}
