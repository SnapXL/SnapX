
// SPDX-License-Identifier: GPL-3.0-or-later


using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using SnapX.Core.Utils.Extensions;

namespace SnapX.Core.ImageEffects.Drawings;

[Description("Checkerboard")]
public class DrawCheckerboard : ImageEffect
{
    private int size;

    [DefaultValue(10)]
    public int Size
    {
        get
        {
            return size;
        }
        set
        {
            size = value.Max(1);
        }
    }

    [DefaultValue(typeof(Color), "LightGray")]
    public Color Color { get; set; }

    [DefaultValue(typeof(Color), "White")]
    public Color Color2 { get; set; }

    [UnconditionalSuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code", Justification = "<Pending>")]
    public DrawCheckerboard()
    {
        this.ApplyDefaultPropertyValues();
    }

    public override Image Apply(Image img)
    {
        int squareSize = Size; // Size of the checkerboard squares
        var color1 = Rgba32.ParseHex(Color.ToHex()); // Primary color for checkers
        var color2 = Rgba32.ParseHex(Color2.ToHex()); // Secondary color for checkers

        img.Mutate(ctx =>
        {
            // Loop through the image in steps of `squareSize` to create the checkerboard pattern
            for (int y = 0; y < img.Height; y += squareSize)
            {
                for (int x = 0; x < img.Width; x += squareSize)
                {
                    // Determine whether to use color1 or color2 based on the position
                    var currentColor = ((x / squareSize + y / squareSize) % 2 == 0) ? color1 : color2;

                    // Draw a square at the current position
                    ctx.Fill(currentColor, new Rectangle(x, y, squareSize, squareSize));
                }
            }
        });

        return img;
    }

    protected override string? GetSummary()
    {
        return $"{Size}x{Size}";
    }
}
