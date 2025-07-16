// SPDX-License-Identifier: GPL-3.0-or-later

using System.Numerics;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace SnapX.Core.ImageEffects.Filters;
internal class Emboss : ImageEffect
{
    public static Vector3 GetPixelRGB(Image<Rgba32> originalImage, int xPos, int yPos)
    {
        xPos = Math.Clamp(xPos, 0, originalImage.Width - 1);
        yPos = Math.Clamp(yPos, 0, originalImage.Height - 1);
        var result = originalImage[xPos, yPos].ToVector4();
        return new(result.X, result.Y, result.Z);
    }

    public static Vector4 GetPixelRGBA(Image<Rgba32> originalImage, int xPos, int yPos)
    {
        xPos = Math.Clamp(xPos, 0, originalImage.Width - 1);
        yPos = Math.Clamp(yPos, 0, originalImage.Height - 1);
        var result = originalImage[xPos, yPos].ToVector4();
        return new(result.X, result.Y, result.Z, result.W);
    }

    public static Image EmbossImage(Image originalImage, bool emboss, bool greyscale)
    {
        var kernel = new[] { +2f, 0f, 0f, 0f, -1f, 0f, 0f, 0f, -1f };
        var offsets = new[]
        {
    new Vector2(-1, 1),
    new Vector2(0, 1),
    new Vector2(1, 1),
    new Vector2(-1, 0),
    new Vector2(0, 0),
    new Vector2(1, 0),
    new Vector2(-1, -1),
    new Vector2(0, -1),
    new Vector2(1, -1)
};

        var imageResult = new Image<Rgba32>(originalImage.Width, originalImage.Height);

        for (var y = 0; y < originalImage.Height; y++)
        {
            for (var x = 0; x < originalImage.Width; x++)
            {
                var sum = new Vector4(0);
                for (var i = 0; i < 9; i++)
                {
                    var temp = GetPixelRGBA(originalImage.CloneAs<Rgba32>(), x + (int)offsets[i].X, y + (int)offsets[i].Y);
                    sum += temp * kernel[i];
                }

                var result = new Rgba32(sum.X + 0.5f, sum.Y + 0.5f, sum.Z + 0.5f, 1f);
                if (greyscale)
                    result = new Rgba32((byte)((result.R + result.G + result.B) / 3),
                                        (byte)((result.R + result.G + result.B) / 3),
                                        (byte)((result.R + result.G + result.B) / 3), 255);

                if (!emboss)
                    result = new Rgba32((byte)Math.Abs(255 - result.R),
                                        (byte)Math.Abs(255 - result.G),
                                        (byte)Math.Abs(255 - result.B), 255);

                imageResult[x, y] = result;
            }
        }

        return imageResult;
    }

    public override Image Apply(Image img) => EmbossImage(img, true, false);

}
