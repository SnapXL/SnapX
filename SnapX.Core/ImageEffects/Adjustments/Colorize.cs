
// SPDX-License-Identifier: GPL-3.0-or-later


using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using SnapX.Core.Utils.Extensions;

namespace SnapX.Core.ImageEffects.Adjustments;

internal class Colorize : ImageEffect
{
    [DefaultValue(typeof(Color))]
    public Rgba32 Color { get; set; }

    [DefaultValue(0f)]
    public float Value { get; set; }

    [UnconditionalSuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code", Justification = "<Pending>")]
    public Colorize()
    {
        this.ApplyDefaultPropertyValues();
    }

    public override Image Apply(Image img)
    {
        img.Mutate(ctx =>
        {
            ctx.ApplyColorize(Color, Value);
        });

        return img;
    }

    protected override string? GetSummary()
    {
        return $"{Color.R}, {Color.G}, {Color.B}";
    }
}
