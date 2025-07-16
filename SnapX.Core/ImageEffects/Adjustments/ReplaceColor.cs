// SPDX-License-Identifier: GPL-3.0-or-later


using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using SnapX.Core.Utils.Extensions;

namespace SnapX.Core.ImageEffects.Adjustments;

[Description("Replace color")]
internal class ReplaceColor : ImageEffect
{
    [DefaultValue(typeof(Color), "White")]
    public Color SourceColor { get; set; }

    [DefaultValue(false)]
    public bool AutoSourceColor { get; set; }

    [DefaultValue(typeof(Color), "Transparent")]
    public Color TargetColor { get; set; }

    [DefaultValue(0)]
    public int Threshold { get; set; }

    [UnconditionalSuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code", Justification = "<Pending>")]
    public ReplaceColor()
    {
        this.ApplyDefaultPropertyValues();
    }

    public override Image Apply(Image img)
    {
        img.Mutate(ctx => ctx.ReplaceColor(SourceColor, TargetColor, AutoSourceColor, Threshold));
        return img;
    }
}
