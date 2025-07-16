// SPDX-License-Identifier: GPL-3.0-or-later


using System.ComponentModel;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;

namespace SnapX.Core.ImageEffects.Adjustments;

[Description("Black & white")]
internal class BlackWhite : ImageEffect
{
    public override Image Apply(Image img)
    {
        img.Mutate(ctx => ctx.Grayscale());

        return img;
    }
}
