using System.Numerics;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using SnapX.Core.Utils;

namespace SnapX.Core.ImageEffects;

public static class ImageProcessingExtensions
{
    public static void ApplyColorize(this IImageProcessingContext ctx, Rgba32 color, float intensity)
    {
        // Ensure intensity is between 0 and 1.
        intensity = Math.Clamp(intensity, 0f, 1f);

        // Apply colorization by looping through each pixel
        ctx.ProcessPixelRowsAsVector4((pixelRow, y) =>
        {
            for (int x = 0; x < pixelRow.Length; x++)
            {
                var pixel = pixelRow[x];

                // Get the current pixel's color
                var r = pixel.X;
                var g = pixel.Y;
                var b = pixel.Z;

                // Apply the colorize effect using the specified color and intensity
                var targetR = color.R / 255f;
                var targetG = color.G / 255f;
                var targetB = color.B / 255f;

                // Blend the original pixel color with the target color based on the intensity
                r = r + (targetR - r) * intensity;
                g = g + (targetG - g) * intensity;
                b = b + (targetB - b) * intensity;

                // Store the updated pixel color back
                pixelRow[x] = new Vector4(r, g, b, pixel.W); // Keep the alpha channel the same
            }
        });
    }
    public static void ApplyGamma(this IImageProcessingContext ctx, float gamma)
    {
        gamma = Math.Max(gamma, 0.01f);

        ctx.ProcessPixelRowsAsVector4((pixelRow, y) =>
        {
            for (int x = 0; x < pixelRow.Length; x++)
            {
                var pixel = pixelRow[x];

                pixelRow[x] = new Vector4(
                    GammaCorrection(pixel.X, gamma),
                    GammaCorrection(pixel.Y, gamma),
                    GammaCorrection(pixel.Z, gamma),
                    pixel.W
                );
            }
        });
    }

    private static float GammaCorrection(float colorValue, float gamma)
    {
        // Gamma correction formula: result = (colorValue / 255) ^ gamma
        // We normalize the color to [0, 1], apply gamma correction, and then scale back to [0, 255]
        return MathF.Pow(colorValue, gamma);
    }
    public static void ReplaceColor(this IImageProcessingContext ctx, Rgba32 sourceColor, Color targetColor, bool autoSourceColor, int threshold)
    {
        ctx.ProcessPixelRowsAsVector4((pixelRow, y) =>
        {
            for (int x = 0; x < pixelRow.Length; x++)
            {
                var pixel = pixelRow[x];

                // If AutoSourceColor is true, automatically determine the source color based on the most common color in the image
                if (autoSourceColor)
                {
                    sourceColor = ImageHelpers.GetMostCommonColor(pixelRow);
                }

                // Calculate the color distance between the current pixel and the source color
                if (ImageHelpers.IsColorClose(new Rgba32(pixel), sourceColor, threshold))
                {
                    // If the color is close to the source color, replace it with the target color
                    pixelRow[x] = pixel;
                }
            }
        });
    }
    public static void SelectiveColor(this IImageProcessingContext ctx, Color lightColor, Color darkColor, int paletteSize)
    {
        // Adjust colors based on luminance
        ctx.ProcessPixelRowsAsVector4((pixelRow, y) =>
        {
            for (int x = 0; x < pixelRow.Length; x++)
            {
                var pixel = pixelRow[x];

                // Calculate the luminance of the pixel
                var luminance = CalculateLuminance(pixel.X, pixel.Y, pixel.Z);

                // Interpolate between lightColor and darkColor based on luminance
                pixelRow[x] = ApplyColorInterpolation(new Rgba32(pixel), lightColor, darkColor, luminance, paletteSize);
            }
        });
    }

    private static float CalculateLuminance(float r, float g, float b)
    {
        // Standard luminance calculation (ITU-R BT.601)
        return 0.2126f * r + 0.7152f * g + 0.0722f * b;
    }

    private static Vector4 ApplyColorInterpolation(Rgba32 pixel, Rgba32 lightColor, Rgba32 darkColor, float luminance, int paletteSize)
    {
        // Calculate how much to interpolate based on luminance (0 = dark, 1 = light)
        var t = luminance; // Use luminance directly for interpolation (adjust for range if necessary)

        // Interpolate between lightColor and darkColor based on luminance
        var red = (byte)(darkColor.R + (lightColor.R - darkColor.R) * t);
        var green = (byte)(darkColor.G + (lightColor.G - darkColor.G) * t);
        var blue = (byte)(darkColor.B + (lightColor.B - darkColor.B) * t);

        // Optionally, you can adjust the transparency (alpha) based on luminance or use a fixed alpha
        var alpha = pixel.A; // Preserve the original alpha value

        return new Vector4(red, green, blue, alpha);
    }
}
