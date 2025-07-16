// SPDX-License-Identifier: GPL-3.0-or-later


using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using SixLabors.ImageSharp;
using SnapX.Core.Utils;
using SnapX.Core.Utils.Extensions;

namespace SnapX.Core.ImageEffects.Filters;
internal class Shadow : ImageEffect
{
    private float opacity;

    [DefaultValue(0.6f), Description("Choose a value between 0.1 and 1.0")]
    public float Opacity
    {
        get
        {
            return opacity;
        }
        set
        {
            opacity = value.Clamp(0.1f, 1.0f);
        }
    }

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
            size = value.Max(0);
        }
    }

    [DefaultValue(0f)]
    public float Darkness { get; set; }

    [DefaultValue(typeof(Color), "Black")]
    public Color Color { get; set; }

    [DefaultValue(typeof(Point), "0, 0")]
    public Point Offset { get; set; }

    [DefaultValue(true)]
    public bool AutoResize { get; set; }

    [UnconditionalSuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code", Justification = "<Pending>")]
    public Shadow()
    {
        this.ApplyDefaultPropertyValues();
    }

    public override Image Apply(Image img)
    {
        return ImageHelpers.AddShadow(img, Opacity, Size, Darkness + 1, Color, Offset, AutoResize);
    }

    protected override string? GetSummary() => Size.ToString();
}
