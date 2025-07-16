// SPDX-License-Identifier: GPL-3.0-or-later


using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using SnapX.Core.Utils.Extensions;

namespace SnapX.Core.ImageEffects;

[Description("Wave edge")]
internal class WaveEdge : ImageEffect
{
    [DefaultValue(15)]
    public int Depth { get; set; }

    [DefaultValue(20)]
    public int Range { get; set; }

    [DefaultValue(AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right)]
    public AnchorStyles Sides { get; set; }

    [UnconditionalSuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code", Justification = "<Pending>")]
    public WaveEdge()
    {
        this.ApplyDefaultPropertyValues();
    }

    public override Image Apply(Image img)
    {
        var depth = Depth; // Intensity of the wave
        var range = Range; // Range of the wave displacement
        var sides = Sides; // Which sides to affect (top, bottom, left, right)

        img.Mutate(ctx =>
        {
            ctx.DrawImage(CreateWavyEdgesImage(img.Width, img.Height, depth, range, sides), new Point(0, 0), 1);
        });

        return img;
    }

    private Image CreateWavyEdgesImage(int width, int height, int depth, int range, AnchorStyles sides)
    {
        var img = new Image<Rgba32>(width, height);
        var rand = new Random();

        // Apply wavy distortion to the edges
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                // Check if pixel is on any of the sides
                if (IsEdge(x, y, width, height, depth, range, sides))
                {
                    // Apply a sine wave distortion on the edge pixels
                    int offsetX = (int)(Math.Sin((y + rand.Next(-5, 5)) * 0.1) * depth); // Sinusoidal wave for X-axis
                    int offsetY = (int)(Math.Cos((x + rand.Next(-5, 5)) * 0.1) * depth); // Sinusoidal wave for Y-axis

                    // Apply the wave offset to the pixel
                    int newX = Math.Min(Math.Max(x + offsetX, 0), width - 1);
                    int newY = Math.Min(Math.Max(y + offsetY, 0), height - 1);

                    img[newX, newY] = img[x, y];
                }
                else
                {
                    img[x, y] = Color.Transparent;
                }
            }
        }

        return img;
    }

    private bool IsEdge(int x, int y, int width, int height, int depth, int range, AnchorStyles sides)
    {
        bool isEdge = false;

        // Check for top or bottom edge
        if ((sides & AnchorStyles.Top) != 0 && y < depth)
            isEdge = true;
        if ((sides & AnchorStyles.Bottom) != 0 && y >= height - depth)
            isEdge = true;

        // Check for left or right edge
        if ((sides & AnchorStyles.Left) != 0 && x < depth)
            isEdge = true;
        if ((sides & AnchorStyles.Right) != 0 && x >= width - depth)
            isEdge = true;

        return isEdge;
    }

    protected override string? GetSummary()
    {
        return $"{Depth}, {Range}";
    }
}
