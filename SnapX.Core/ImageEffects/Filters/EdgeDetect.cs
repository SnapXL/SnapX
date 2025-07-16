// SPDX-License-Identifier: GPL-3.0-or-later


using System.ComponentModel;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Processing.Processors.Convolution;

namespace SnapX.Core.ImageEffects.Filters;

[Description("Edge detect")]
internal class EdgeDetect : ImageEffect
{
    public override Image Apply(Image img)
    {
        var edgeDetectKernel = new EdgeDetectorKernel();

        img.Mutate(ctx => ctx.ApplyProcessor(new EdgeDetectorProcessor(edgeDetectKernel, false)));

        return img;
    }
}
