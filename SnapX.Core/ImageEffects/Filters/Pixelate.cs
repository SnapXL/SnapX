
// SPDX-License-Identifier: GPL-3.0-or-later


using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using SixLabors.ImageSharp;
using SnapX.Core.Utils;
using SnapX.Core.Utils.Extensions;

namespace SnapX.Core.ImageEffects.Filters;

internal class Pixelate : ImageEffect
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
            size = value.Max(2);
        }
    }

    private int borderSize;

    [DefaultValue(0)]
    public int BorderSize
    {
        get
        {
            return borderSize;
        }
        set
        {
            borderSize = value.Max(0);
        }
    }

    [DefaultValue(typeof(Color), "Black")]
    public Color BorderColor { get; set; }

    [UnconditionalSuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code", Justification = "<Pending>")]
    public Pixelate()
    {
        this.ApplyDefaultPropertyValues();
    }

    public override Image Apply(Image img)
    {
        ImageHelpers.Pixelate(img, Size, BorderSize, BorderColor);
        return img;
    }

    protected override string? GetSummary()
    {
        return Size.ToString();
    }
}

