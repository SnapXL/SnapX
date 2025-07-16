// SPDX-License-Identifier: GPL-3.0-or-later


using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using SnapX.Core.Utils.Extensions;

namespace SnapX.Core.ImageEffects.Drawings;

[Description("Border")]
public class DrawBorder : ImageEffect
{
    [DefaultValue(BorderType.Outside)]
    public BorderType Type { get; set; }

    private int size;

    [DefaultValue(1)]
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

    [DefaultValue(typeof(Color), "Black")]
    public Color Color { get; set; }

    [DefaultValue(false)]
    public bool UseGradient { get; set; }

    public GradientBrush Gradient { get; set; }

    [UnconditionalSuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code", Justification = "<Pending>")]
    public DrawBorder()
    {
        this.ApplyDefaultPropertyValues();
        AddDefaultGradient();
    }

    private void AddDefaultGradient()
    {
        Gradient = new LinearGradientBrush(
            new PointF(0, 0), // Start point of the gradient
            new PointF(1, 1), // End point of the gradient
            GradientRepetitionMode.Repeat
        );
    }

    public override Image Apply(Image img)
    {
        if (UseGradient && Gradient != null)
        {
            img.Mutate(ctx =>
            {
                // Use the gradient to draw the border (top, right, bottom, left)
                ctx.DrawLine(new SolidPen(new LinearGradientBrush(
                    new PointF(0, 0),
                    new PointF(1, 0),
                    GradientRepetitionMode.Repeat,
                    new ColorStop(0f, Rgba32.ParseHex("#4478C2")),
                    new ColorStop(0.5f, Rgba32.ParseHex("#0D3A7A")),
                    new ColorStop(0.5f, Rgba32.ParseHex("#06384E")),
                    new ColorStop(1f, Rgba32.ParseHex("#1759AE"))
                ), Size), new PointF(0, 0), new PointF(img.Width, 0));  // Top border

                ctx.DrawLine(new SolidPen(new LinearGradientBrush(
                    new PointF(0, 0),
                    new PointF(1, 0),
                    GradientRepetitionMode.Repeat,
                    new ColorStop(0f, Rgba32.ParseHex("#4478C2")),
                    new ColorStop(0.5f, Rgba32.ParseHex("#0D3A7A")),
                    new ColorStop(0.5f, Rgba32.ParseHex("#06384E")),
                    new ColorStop(1f, Rgba32.ParseHex("#1759AE"))
                ), Size), new PointF(img.Width, 0), new PointF(img.Width, img.Height));  // Right border

                ctx.DrawLine(new SolidPen(new LinearGradientBrush(
                    new PointF(0, 0),
                    new PointF(1, 0),
                    GradientRepetitionMode.Repeat,
                    new ColorStop(0f, Rgba32.ParseHex("#4478C2")),
                    new ColorStop(0.5f, Rgba32.ParseHex("#0D3A7A")),
                    new ColorStop(0.5f, Rgba32.ParseHex("#06384E")),
                    new ColorStop(1f, Rgba32.ParseHex("#1759AE"))
                ), Size), new PointF(img.Width, img.Height), new PointF(0, img.Height));  // Bottom border

                ctx.DrawLine(new SolidPen(new LinearGradientBrush(
                    new PointF(0, 0),
                    new PointF(1, 0),
                    GradientRepetitionMode.Repeat,
                    new ColorStop(0f, Rgba32.ParseHex("#4478C2")),
                    new ColorStop(0.5f, Rgba32.ParseHex("#0D3A7A")),
                    new ColorStop(0.5f, Rgba32.ParseHex("#06384E")),
                    new ColorStop(1f, Rgba32.ParseHex("#1759AE"))
                ), Size), new PointF(0, img.Height), new PointF(0, 0));  // Left border
            });
            return img;
        }

        // Fallback to solid color border if no gradient
        img.Mutate(ctx =>
       {
           ctx.DrawLine(new SolidPen(Rgba32.ParseHex(Color.ToHex()), Size), new PointF(0, 0), new PointF(img.Width, 0));  // Top border
           ctx.DrawLine(new SolidPen(Rgba32.ParseHex(Color.ToHex()), Size), new PointF(img.Width, 0), new PointF(img.Width, img.Height));  // Right border
           ctx.DrawLine(new SolidPen(Rgba32.ParseHex(Color.ToHex()), Size), new PointF(img.Width, img.Height), new PointF(0, img.Height));  // Bottom border
           ctx.DrawLine(new SolidPen(Rgba32.ParseHex(Color.ToHex()), Size), new PointF(0, img.Height), new PointF(0, 0));  // Left border
       });
        return img;
    }


    protected override string? GetSummary()
    {
        return Size + "px";
    }
}
