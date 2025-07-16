// SPDX-License-Identifier: GPL-3.0-or-later


using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using SixLabors.ImageSharp;
using SnapX.Core.Utils;
using SnapX.Core.Utils.Extensions;

namespace SnapX.Core.ImageEffects.Manipulations;

internal class Canvas : ImageEffect
{
    [DefaultValue(typeof(Padding), "0, 0, 0, 0")]
    public Padding Margin { get; set; }

    [DefaultValue(CanvasMarginMode.AbsoluteSize), Description("How the margin around the canvas will be calculated.")]
    public CanvasMarginMode MarginMode { get; set; }

    [DefaultValue(typeof(Color), "Transparent")]
    public Color Color { get; set; }

    [UnconditionalSuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code", Justification = "<Pending>")]
    public Canvas()
    {
        this.ApplyDefaultPropertyValues();
    }

    public enum CanvasMarginMode
    {
        AbsoluteSize,
        PercentageOfCanvas
    }

    public override Image Apply(Image img)
    {
        Padding canvasMargin;

        if (MarginMode == CanvasMarginMode.PercentageOfCanvas)
        {
            canvasMargin = new Padding
            {
                Left = (int)Math.Round(Margin.Left / 100f * img.Width),
                Right = (int)Math.Round(Margin.Right / 100f * img.Width),
                Top = (int)Math.Round(Margin.Top / 100f * img.Height),
                Bottom = (int)Math.Round(Margin.Bottom / 100f * img.Height),
            };
        }
        else
        {
            canvasMargin = Margin;
        }

        var imgResult = ImageHelpers.AddCanvas(img, canvasMargin, Color);

        img.Dispose();
        return imgResult;
    }

    protected override string? GetSummary() => Margin.ToString();
}
