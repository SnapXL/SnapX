
// SPDX-License-Identifier: GPL-3.0-or-later


using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using SnapX.Core.Utils.Extensions;

namespace SnapX.Core.ImageEffects.Adjustments;

internal class Alpha : ImageEffect
{
    [DefaultValue(1f), Description("Pixel alpha = Pixel alpha * Value\r\nExample 0.5 will decrease alpha of pixel 50%")]
    public float Value { get; set; }

    [DefaultValue(0f), Description("Pixel alpha = Pixel alpha + Addition\r\nExample 0.5 will increase alpha of pixel 127.5")]
    public float Addition { get; set; }

    [UnconditionalSuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code", Justification = "<Pending>")]
    public Alpha()
    {
        this.ApplyDefaultPropertyValues();
    }

    public override Image Apply(Image img)
    {
        img.Mutate(ctx => ctx
            .Opacity(Value));
        return img;
    }

    protected override string? GetSummary()
    {
        return $"{Value}, {Addition}";
    }
}
