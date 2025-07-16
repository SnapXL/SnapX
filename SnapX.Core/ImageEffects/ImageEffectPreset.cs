// SPDX-License-Identifier: GPL-3.0-or-later


using SixLabors.ImageSharp;
using SnapX.Core.ImageEffects.Drawings;
using SnapX.Core.ImageEffects.Manipulations;

namespace SnapX.Core.ImageEffects;

public class ImageEffectPreset
{
    public string Name { get; set; } = "";

    public List<ImageEffect> Effects { get; set; } = [];

    public Image ApplyEffects(Image img)
    {
        img.Metadata.HorizontalResolution = 96f;
        img.Metadata.VerticalResolution = 96f;

        if (Effects != null && Effects.Count > 0)
        {
            foreach (var effect in Effects.Where(x => x.Enabled))
            {
                img = effect.Apply(img);

                if (img == null)
                {
                    break;
                }
            }
        }

        return img;
    }

    public override string ToString() => Name ?? "Name";

    public static ImageEffectPreset GetDefaultPreset()
    {
        var preset = new ImageEffectPreset();

        var canvas = new Canvas();
        canvas.Margin = new Padding(0, 0, 0, 30);
        preset.Effects.Add(canvas);

        var text = new DrawText();
        text.Offset = new Point(0, 0);
        text.UseGradient = true;
        preset.Effects.Add(text);

        return preset;
    }
}
