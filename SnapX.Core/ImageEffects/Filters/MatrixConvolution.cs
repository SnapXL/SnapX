// SPDX-License-Identifier: GPL-3.0-or-later


using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SnapX.Core.Utils.Extensions;
using Vector4 = System.Numerics.Vector4;

namespace SnapX.Core.ImageEffects.Filters;

[Description("Convolution matrix")]
internal class MatrixConvolution : ImageEffect
{
    [DefaultValue(0)]
    public int X0Y0 { get; set; }
    [DefaultValue(0)]
    public int X1Y0 { get; set; }
    [DefaultValue(0)]
    public int X2Y0 { get; set; }

    [DefaultValue(0)]
    public int X0Y1 { get; set; }
    [DefaultValue(1)]
    public int X1Y1 { get; set; }
    [DefaultValue(0)]
    public int X2Y1 { get; set; }

    [DefaultValue(0)]
    public int X0Y2 { get; set; }
    [DefaultValue(0)]
    public int X1Y2 { get; set; }
    [DefaultValue(0)]
    public int X2Y2 { get; set; }

    [DefaultValue(1.0)]
    public double Factor { get; set; }

    [DefaultValue((byte)0)]
    public byte Offset { get; set; }

    [UnconditionalSuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code", Justification = "<Pending>")]
    public MatrixConvolution()
    {
        this.ApplyDefaultPropertyValues();
    }

    public override Image Apply(Image img)
    {
        var rgbaImg = img.CloneAs<Rgba32>();
        var imageResult = new Image<Rgba32>(img.Width, img.Height);

        var matrix = new[,]
        {
            { (float)(X0Y0 / Factor), (float)(X1Y0 / Factor), (float)(X2Y0 / Factor) },
            { (float)(X0Y1 / Factor), (float)(X1Y1 / Factor), (float)(X2Y1 / Factor) },
            { (float)(X0Y2 / Factor), (float)(X1Y2 / Factor), (float)(X2Y2 / Factor) }
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

                sum += new Vector4(Offset, Offset, Offset, 0); // Apply offset

                // Clamp values to byte range
                sum.X = Math.Clamp(sum.X, 0, 255);
                sum.Y = Math.Clamp(sum.Y, 0, 255);
                sum.Z = Math.Clamp(sum.Z, 0, 255);

                imageResult[x, y] = new Rgba32(sum.X, sum.Y, sum.Z, sum.W);
            }
        }

        return imageResult;
    }
}
