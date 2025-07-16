// SPDX-License-Identifier: GPL-3.0-or-later


using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using SixLabors.ImageSharp;
using SnapX.Core.Utils;
using SnapX.Core.Utils.Extensions;


namespace SnapX.Core.ImageEffects.Drawings;

[Description("Background image")]
public class DrawBackgroundImage : ImageEffect
{
    [DefaultValue("")]
    public string? ImageFilePath { get; set; }

    [DefaultValue(true)]
    public bool Center { get; set; }

    [DefaultValue(false)]
    public bool Tile { get; set; }

    [UnconditionalSuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code", Justification = "<Pending>")]
    public DrawBackgroundImage()
    {
        this.ApplyDefaultPropertyValues();
    }

    public override Image Apply(Image img)
    {
        return ImageHelpers.DrawBackgroundImage(img, ImageFilePath, Center, Tile);
    }

    protected override string? GetSummary()
    {
        if (!string.IsNullOrEmpty(ImageFilePath))
        {
            return FileHelpers.GetFileNameSafe(ImageFilePath);
        }

        return null;
    }
}
