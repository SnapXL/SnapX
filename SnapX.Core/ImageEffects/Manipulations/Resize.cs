// SPDX-License-Identifier: GPL-3.0-or-later


using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using SixLabors.ImageSharp;
using SnapX.Core.Utils;
using SnapX.Core.Utils.Extensions;

namespace SnapX.Core.ImageEffects.Manipulations;

public class Resize : ImageEffect
{
    [DefaultValue(250), Description("Use width as 0 to automatically adjust width to maintain aspect ratio.")]
    public int Width { get; set; } = 250;

    [DefaultValue(0), Description("Use height as 0 to automatically adjust height to maintain aspect ratio.")]
    public int Height { get; set; } = 0;

    [DefaultValue(ResizeMode.ResizeAll)]
    public ResizeMode Mode { get; set; } = ResizeMode.ResizeAll;

    [UnconditionalSuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code", Justification = "<Pending>")]
    public Resize()
    {
        this.ApplyDefaultPropertyValues();
    }

    public Resize(int width, int height)
    {
        Width = width;
        Height = height;
    }

    public override Image Apply(Image img)
    {
        if (Width <= 0 && Height <= 0)
        {
            return img;
        }

        var size = ImageHelpers.ApplyAspectRatio(Width, Height, img);

        if ((Mode == ResizeMode.ResizeIfBigger && img.Width <= size.Width && img.Height <= size.Height) ||
            (Mode == ResizeMode.ResizeIfSmaller && img.Width >= size.Width && img.Height >= size.Height))
        {
            return img;
        }

        return ImageHelpers.ResizeImage(img, size);
    }

    protected override string? GetSummary()
    {
        var summary = Width.ToString();

        if (Width > 0)
        {
            summary += "px";
        }

        summary += ", " + Height.ToString();

        if (Height > 0)
        {
            summary += "px";
        }

        return summary;
    }
}
