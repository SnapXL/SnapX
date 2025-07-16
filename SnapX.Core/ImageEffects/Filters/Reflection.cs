// SPDX-License-Identifier: GPL-3.0-or-later


using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using SixLabors.ImageSharp;
using SnapX.Core.Utils;
using SnapX.Core.Utils.Extensions;

namespace SnapX.Core.ImageEffects.Filters;

internal class Reflection : ImageEffect
{
    private int percentage;

    [DefaultValue(20), Description("Reflection height size relative to screenshot height.\nValue need to be between 1 to 100.")]
    public int Percentage
    {
        get
        {
            return percentage;
        }
        set
        {
            percentage = value.Clamp(1, 100);
        }
    }

    private int maxAlpha;

    [DefaultValue(255), Description("Reflection transparency start from this value to MinAlpha.\nValue need to be between 0 to 255.")]
    public int MaxAlpha
    {
        get
        {
            return maxAlpha;
        }
        set
        {
            maxAlpha = value.Clamp(0, 255);
        }
    }

    private int minAlpha;

    [DefaultValue(0), Description("Reflection transparency start from MaxAlpha to this value.\nValue need to be between 0 to 255.")]
    public int MinAlpha
    {
        get
        {
            return minAlpha;
        }
        set
        {
            minAlpha = value.Clamp(0, 255);
        }
    }

    [DefaultValue(0), Description("Reflection start position will be: Screenshot height + Offset")]
    public int Offset { get; set; }

    [DefaultValue(false), Description("Adding skew to reflection from bottom left to bottom right.")]
    public bool Skew { get; set; }

    [DefaultValue(25), Description("How much pixel skew left to right.")]
    public int SkewSize { get; set; }

    [UnconditionalSuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code", Justification = "<Pending>")]
    public Reflection()
    {
        this.ApplyDefaultPropertyValues();
    }

    public override Image Apply(Image img)
    {
        return ImageHelpers.DrawReflection(img, Percentage, MaxAlpha, MinAlpha, Offset, Skew, SkewSize);
    }

    protected override string? GetSummary()
    {
        return Percentage.ToString();
    }
}
