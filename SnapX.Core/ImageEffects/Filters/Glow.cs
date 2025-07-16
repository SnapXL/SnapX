// SPDX-License-Identifier: GPL-3.0-or-later


using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SnapX.Core.Utils;
using SnapX.Core.Utils.Extensions;

namespace SnapX.Core.ImageEffects.Filters;
internal class Glow : ImageEffect
{
    private int size;

    [DefaultValue(20)]
    public int Size
    {
        get
        {
            return size;
        }
        set
        {
            size = value.Max(0);
        }
    }

    private float strength;

    [DefaultValue(1f)]
    public float Strength
    {
        get
        {
            return strength;
        }
        set
        {
            strength = value.Max(0.1f);
        }
    }

    [DefaultValue(typeof(Color), "White")]
    public Color Color { get; set; }

    [DefaultValue(false)]
    public bool UseGradient { get; set; }

    public GradientBrush Gradient { get; set; }

    [DefaultValue(typeof(Point), "0, 0")]
    public Point Offset { get; set; }

    [UnconditionalSuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code", Justification = "<Pending>")]
    public Glow()
    {
        this.ApplyDefaultPropertyValues();
        Gradient = AddDefaultGradient();
    }

    private LinearGradientBrush AddDefaultGradient()
    {
        var start = new PointF(0, 0);
        var end = new PointF(1, 1);

        return new LinearGradientBrush(start, end, GradientRepetitionMode.None);
    }

    public override Image Apply(Image img)
    {
        return ImageHelpers.AddGlow(img, Size, Strength, Color, Offset, UseGradient ? Gradient : null);
    }

    protected override string? GetSummary()
    {
        return Size.ToString();
    }
}
