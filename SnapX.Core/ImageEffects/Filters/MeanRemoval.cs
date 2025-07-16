// SPDX-License-Identifier: GPL-3.0-or-later


using System.ComponentModel;
using System.Numerics;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace SnapX.Core.ImageEffects.Filters;

[Description("Mean removal")]
internal class MeanRemoval : ImageEffect
{
    public override Image Apply(Image img)
    {
        var rgbaImg = img.CloneAs<Rgba32>();
        var imageResult = new Image<Rgba32>(img.Width, img.Height);
        float[,] matrix = new float[,]
        {
            { 1f/9f, 1f/9f, 1f/9f },
            { 1f/9f, 1f/9f, 1f/9f },
            { 1f/9f, 1f/9f, 1f/9f }
        };

        for (var y = 0; y < img.Height; y++)
        {
            for (var x = 0; x < img.Width; x++)
            {
                var sum = new Vector4(0);
                for (var kernelY = -1; kernelY <= 1; kernelY++)
                {
                    for (var kernelX = -1; kernelX <= 1; kernelX++)
                    {
                        int newX = x + kernelX;
                        int newY = y + kernelY;

                        if (newX >= 0 && newX < img.Width && newY >= 0 && newY < img.Height)
                        {
                            var pixel = rgbaImg[newX, newY];
                            sum += new Vector4(pixel.R, pixel.G, pixel.B, pixel.A) * matrix[kernelY + 1, kernelX + 1];
                        }
                    }
                }

                sum.X = Math.Clamp(sum.X, 0, 255);
                sum.Y = Math.Clamp(sum.Y, 0, 255);
                sum.Z = Math.Clamp(sum.Z, 0, 255);

                imageResult[x, y] = new Rgba32(sum.X, sum.Y, sum.Z, sum.W);
            }
        }

        return imageResult;
    }
}
