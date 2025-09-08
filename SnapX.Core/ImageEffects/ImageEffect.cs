
// SPDX-License-Identifier: GPL-3.0-or-later


using System.ComponentModel;
using System.Text.Json.Serialization;
using SixLabors.ImageSharp;
using SnapX.Core.ImageEffects.Adjustments;
using SnapX.Core.ImageEffects.Drawings;
using SnapX.Core.ImageEffects.Filters;
using SnapX.Core.ImageEffects.Manipulations;
using SnapX.Core.Utils.Extensions;

namespace SnapX.Core.ImageEffects;
[JsonPolymorphic(TypeDiscriminatorPropertyName = "$type")]
[JsonDerivedType(typeof(Inverse), "Inverse")]
[JsonDerivedType(typeof(Polaroid), "Polaroid")]
[JsonDerivedType(typeof(Alpha), "Alpha")]
[JsonDerivedType(typeof(BlackWhite), "BlackWhite")]
[JsonDerivedType(typeof(Brightness), "Brightness")]
[JsonDerivedType(typeof(Colorize), "Colorize")]
[JsonDerivedType(typeof(Contrast), "Contrast")]
[JsonDerivedType(typeof(Gamma), "Gamma")]
[JsonDerivedType(typeof(Grayscale), "Grayscale")]
[JsonDerivedType(typeof(Hue), "Hue")]
[JsonDerivedType(typeof(MatrixColor), "MatrixColor")]
[JsonDerivedType(typeof(ReplaceColor), "ReplaceColor")]
[JsonDerivedType(typeof(Saturation), "Saturation")]
[JsonDerivedType(typeof(SelectiveColor), "SelectiveColor")]
[JsonDerivedType(typeof(Sepia), "Sepia")]
[JsonDerivedType(typeof(DrawBackground), "DrawBackground")]
[JsonDerivedType(typeof(DrawBackgroundImage), "DrawBackgroundImage")]
[JsonDerivedType(typeof(DrawBorder), "DrawBorder")]
[JsonDerivedType(typeof(DrawCheckerboard), "DrawCheckerboard")]
[JsonDerivedType(typeof(DrawImage), "DrawImage")]
[JsonDerivedType(typeof(DrawParticles), "DrawParticles")]
[JsonDerivedType(typeof(DrawText), "DrawText")]
[JsonDerivedType(typeof(DrawTextEx), "DrawTextEx")]
[JsonDerivedType(typeof(ColorDepth), "ColorDepth")]
[JsonDerivedType(typeof(Sharpen), "Sharpen")]
[JsonDerivedType(typeof(Smooth), "Smooth")]
[JsonDerivedType(typeof(Blur), "Blur")]
[JsonDerivedType(typeof(EdgeDetect), "EdgeDetect")]
[JsonDerivedType(typeof(Emboss), "Emboss")]
[JsonDerivedType(typeof(GaussianBlur), "GaussianBlur")]
[JsonDerivedType(typeof(Glow), "Glow")]
[JsonDerivedType(typeof(MatrixConvolution), "MatrixConvolution")]
[JsonDerivedType(typeof(MeanRemoval), "MeanRemoval")]
[JsonDerivedType(typeof(Outline), "Outline")]
[JsonDerivedType(typeof(Pixelate), "Pixelate")]
[JsonDerivedType(typeof(RGBSplit), "RGBSplit")]
[JsonDerivedType(typeof(Reflection), "Reflection")]
[JsonDerivedType(typeof(Shadow), "Shadow")]
[JsonDerivedType(typeof(Slice), "Slice")]
[JsonDerivedType(typeof(TornEdge), "TornEdge")]
[JsonDerivedType(typeof(WaveEdge), "WaveEdge")]
[JsonDerivedType(typeof(AutoCrop), "AutoCrop")]
[JsonDerivedType(typeof(Canvas), "Canvas")]
[JsonDerivedType(typeof(Crop), "Crop")]
[JsonDerivedType(typeof(Flip), "Flip")]
[JsonDerivedType(typeof(ForceProportions), "ForceProportions")]
[JsonDerivedType(typeof(Resize), "Resize")]
[JsonDerivedType(typeof(Rotate), "Rotate")]
[JsonDerivedType(typeof(RoundedCorners), "RoundedCorners")]
[JsonDerivedType(typeof(Scale), "Scale")]
[JsonDerivedType(typeof(Skew), "Skew")]
public abstract class ImageEffect
{
    [DefaultValue(true), Browsable(false)]
    public bool Enabled { get; set; }
    [DefaultValue(""), Browsable(false)]
    public string Name { get; set; }

    protected ImageEffect()
    {
        Enabled = true;
    }

    public abstract Image Apply(Image img);

    protected virtual string? GetSummary()
    {
        return null;
    }

    public override string ToString()
    {
        if (!string.IsNullOrEmpty(Name))
        {
            return Name;
        }

        string name = GetType().GetDescription();
        string? summary = GetSummary();

        if (!string.IsNullOrEmpty(summary))
        {
            name = $"{name}: {summary}";
        }

        return name;
    }
}
