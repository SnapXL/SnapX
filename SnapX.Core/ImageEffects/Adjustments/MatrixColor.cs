// SPDX-License-Identifier: GPL-3.0-or-later


using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using SnapX.Core.Utils.Extensions;

namespace SnapX.Core.ImageEffects.Adjustments;

[Description("Color matrix")]
internal class MatrixColor : ImageEffect
{
    [DefaultValue(1f), Description("Red = (Red * Rr) + (Green * Rg) + (Blue * Rb) + (Alpha * Ra) + Ro")]
    public float Rr { get; set; }
    [DefaultValue(0f)]
    public float Rg { get; set; }
    [DefaultValue(0f)]
    public float Rb { get; set; }
    [DefaultValue(0f)]
    public float Ra { get; set; }
    [DefaultValue(0f)]
    public float Ro { get; set; }

    [DefaultValue(0f)]
    public float Gr { get; set; }
    [DefaultValue(1f)]
    public float Gg { get; set; }
    [DefaultValue(0f)]
    public float Gb { get; set; }
    [DefaultValue(0f)]
    public float Ga { get; set; }
    [DefaultValue(0f)]
    public float Go { get; set; }

    [DefaultValue(0f)]
    public float Br { get; set; }
    [DefaultValue(0f)]
    public float Bg { get; set; }
    [DefaultValue(1f)]
    public float Bb { get; set; }
    [DefaultValue(0f)]
    public float Ba { get; set; }
    [DefaultValue(0f)]
    public float Bo { get; set; }

    [DefaultValue(0f)]
    public float Ar { get; set; }
    [DefaultValue(0f)]
    public float Ag { get; set; }
    [DefaultValue(0f)]
    public float Ab { get; set; }
    [DefaultValue(1f)]
    public float Aa { get; set; }
    [DefaultValue(0f)]
    public float Ao { get; set; }

    [UnconditionalSuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code", Justification = "<Pending>")]
    public MatrixColor()
    {
        this.ApplyDefaultPropertyValues();
    }

    public override Image Apply(Image img)
    {
        var colorMatrix = new ColorMatrix(
            Rr, Gr, Br, Ar, 0f,
            Rg, Gg, Bg, Ag, 0f,
            Rb, Gb, Bb, Ab, 0f,
            Ra, Ga, Ba, Aa, 0f
        );
        img.Mutate(ctx => ctx.Filter(colorMatrix));
        return img;
    }
}
