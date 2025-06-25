
// SPDX-License-Identifier: GPL-3.0-or-later


using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SnapX.Core.Utils.Extensions;

namespace SnapX.Core.ImageEffects.Drawings;

[Description("Text")]
public class DrawTextEx : ImageEffect
{
    [DefaultValue("Text")]
    public string Text { get; set; }

    [DefaultValue(ContentAlignment.TopLeft)]
    public ContentAlignment Placement { get; set; }

    [DefaultValue(typeof(Point), "0, 0")]
    public Point Offset { get; set; }

    [DefaultValue(0)]
    public int Angle { get; set; }

    [DefaultValue(false), Description("If text size bigger than source image then don't draw it.")]
    public bool AutoHide { get; set; }

    [DefaultValue(typeof(Font), "Arial, 36pt")]
    public Font Font => new Font(new FontFamily(), 36, FontStyle.Regular);

    [DefaultValue(typeof(Color), "235, 235, 235"),]
    public Color Color { get; set; }

    [DefaultValue(false)]
    public bool UseGradient { get; set; }

    public GradientBrush Gradient { get; set; }

    [DefaultValue(false)]
    public bool Outline { get; set; }

    [DefaultValue(5)]
    public int OutlineSize { get; set; }

    [DefaultValue(typeof(Color), "235, 0, 0")]
    public Color OutlineColor { get; set; }

    [DefaultValue(false)]
    public bool OutlineUseGradient { get; set; }

    public GradientBrush OutlineGradient { get; set; }

    [DefaultValue(false)]
    public bool Shadow { get; set; }

    [DefaultValue(typeof(Point), "0, 5")]
    public Point ShadowOffset { get; set; }

    [DefaultValue(typeof(Color), "125, 0, 0, 0")]
    public Color ShadowColor { get; set; }

    [DefaultValue(false)]
    public bool ShadowUseGradient { get; set; }

    public GradientBrush ShadowGradient { get; set; }

    [UnconditionalSuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code", Justification = "<Pending>")]
    public DrawTextEx()
    {
        this.ApplyDefaultPropertyValues();
    }

    public override Image Apply(Image img)
    {
        // TODO: Implement DrawTextEx
        return img;
    }

    protected override string? GetSummary()
    {
        if (!string.IsNullOrEmpty(Text))
        {
            return Text.Truncate(20, "...");
        }

        return null;
    }
}
