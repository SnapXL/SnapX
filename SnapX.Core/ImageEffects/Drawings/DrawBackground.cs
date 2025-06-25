
// SPDX-License-Identifier: GPL-3.0-or-later


using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using SnapX.Core.Utils.Extensions;

namespace SnapX.Core.ImageEffects.Drawings;

[Description("Background")]
public class DrawBackground : ImageEffect
{
    [DefaultValue(typeof(Color), "Black")]
    public Color Color { get; set; }

    [DefaultValue(false)]
    public bool UseGradient { get; set; }

    public GradientBrush Gradient { get; set; }

    [UnconditionalSuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code", Justification = "<Pending>")]
    public DrawBackground()
    {
        this.ApplyDefaultPropertyValues();
        AddDefaultGradient();
    }

    private void AddDefaultGradient()
    {
        var gradientStops = new ColorStop[]
        {
            new(0f, Color.FromRgba(68, 120, 194, 255)),   // 0% position
            new(0.5f, Color.FromRgba(13, 58, 122, 255)),  // 50% position
            new(0.5f, Color.FromRgba(6, 36, 78, 255)),    // 50% position
            new(1f, Color.FromRgba(23, 89, 174, 255))    // 100% position
        };

        // Create a LinearGradientBrush with the defined stops
        var gradientBrush = new LinearGradientBrush(
            new PointF(0, 0),
            new PointF(1, 1),
            GradientRepetitionMode.Repeat
        );

        // Create an image with a specified size
        int width = 500;
        int height = 500;
        var img = new Image<Rgba32>(width, height);

        // Apply the gradient to the image
        img.Mutate(ctx =>
                ctx.Fill(gradientBrush) // Fill the image with the gradient
        );
    }

    public override Image Apply(Image img)
    {
        using (img)
        {
            if (UseGradient && Gradient != null)
            {
                img.Mutate(ctx => ctx.Fill(Gradient));
                return img;
            }
            img.Mutate(ctx => ctx.Fill(Color));
            return img;
        }
    }

    protected override string? GetSummary()
    {
        if (!UseGradient)
        {
            return Color.ToString();
        }

        return null;
    }
}

