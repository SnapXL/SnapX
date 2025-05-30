
// SPDX-License-Identifier: GPL-3.0-or-later


using System.ComponentModel;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace SnapX.Core.ImageEffects.Filters;
[Description("RGB split")]
internal class RGBSplit : ImageEffect
{
    [DefaultValue(typeof(Point), "-5, 0")]
    public Point OffsetRed { get; set; } = new(-5, 0);

    [DefaultValue(typeof(Point), "0, 0")]
    public Point OffsetGreen { get; set; }

    [DefaultValue(typeof(Point), "5, 0")]
    public Point OffsetBlue { get; set; } = new(5, 0);

    public override Image Apply(Image img)
    {
        using var rgbaImg = img.CloneAs<Rgba32>();
        var resultImage = img.CloneAs<Rgba32>(); // Clone the image to preserve original

        var width = img.Width;
        var height = img.Height;

        var offsetRed = new Point(5, 0);   // Example: Shift Red by 5 pixels in the X direction
        var offsetGreen = new Point(0, 5); // Example: Shift Green by 5 pixels in the Y direction
        var offsetBlue = new Point(-5, 0); // Example: Shift Blue by -5 pixels in the X direction

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                // Clamp the pixel positions to stay within bounds
                var colorR = rgbaImg[Math.Clamp(x - offsetRed.X, 0, width - 1), Math.Clamp(y - offsetRed.Y, 0, height - 1)];
                var colorG = rgbaImg[Math.Clamp(x - offsetGreen.X, 0, width - 1), Math.Clamp(y - offsetGreen.Y, 0, height - 1)];
                var colorB = rgbaImg[Math.Clamp(x - offsetBlue.X, 0, width - 1), Math.Clamp(y - offsetBlue.Y, 0, height - 1)];

                // Calculate the shifted color with adjusted alpha
                var shiftedColor = new Rgba32(
                    (byte)(colorB.B * colorB.A / 255),
                    (byte)(colorG.G * colorG.A / 255),
                    (byte)(colorR.R * colorR.A / 255),
                    (byte)((colorR.A + colorG.A + colorB.A) / 3) // Average alpha
                );

                // Set the pixel in the resulting image
                resultImage[x, y] = shiftedColor;
            }
        }

        return resultImage;
    }
}
