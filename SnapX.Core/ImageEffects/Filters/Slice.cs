// SPDX-License-Identifier: GPL-3.0-or-later


using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using SixLabors.ImageSharp;
using SnapX.Core.Utils;
using SnapX.Core.Utils.Extensions;

namespace SnapX.Core.ImageEffects.Filters;

[Description("Slice")]
internal class Slice : ImageEffect
{
    private int minSliceHeight;

    [DefaultValue(10)]
    public int MinSliceHeight
    {
        get
        {
            return minSliceHeight;
        }
        set
        {
            minSliceHeight = value.Max(1);
        }
    }

    private int maxSliceHeight;

    [DefaultValue(100)]
    public int MaxSliceHeight
    {
        get
        {
            return maxSliceHeight;
        }
        set
        {
            maxSliceHeight = value.Max(1);
        }
    }

    [DefaultValue(0)]
    public int MinSliceShift { get; set; }

    [DefaultValue(10)]
    public int MaxSliceShift { get; set; }

    [UnconditionalSuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code", Justification = "<Pending>")]
    public Slice()
    {
        this.ApplyDefaultPropertyValues();
    }

    public override Image Apply(Image img)
    {
        int minSliceHeight = Math.Min(MinSliceHeight, MaxSliceHeight);
        int maxSliceHeight = Math.Max(MinSliceHeight, MaxSliceHeight);
        int minSliceShift = Math.Min(MinSliceShift, MaxSliceShift);
        int maxSliceShift = Math.Max(MinSliceShift, MaxSliceShift);

        using (img)
        {
            return ImageHelpers.Slice(img, minSliceHeight, maxSliceHeight, minSliceShift, maxSliceShift);
        }
    }

    protected override string? GetSummary()
    {
        return $"{MinSliceHeight}, {MaxSliceHeight}";
    }
}
