
// SPDX-License-Identifier: GPL-3.0-or-later


using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using SnapX.Core.Utils.Extensions;

namespace SnapX.Core.ImageEffects.Filters;

[Description("Torn edge")]
internal class TornEdge : ImageEffect
{
    [DefaultValue(15)]
    public int Depth { get; set; }

    [DefaultValue(20)]
    public int Range { get; set; }

    [DefaultValue(AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right)]
    public AnchorStyles Sides { get; set; }

    [DefaultValue(true)]
    public bool CurvedEdges { get; set; }

    [UnconditionalSuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code", Justification = "<Pending>")]
    public TornEdge()
    {
        this.ApplyDefaultPropertyValues();
    }

    public override Image Apply(Image img)
    {
        // Define the range and depth of the torn effect
        int depth = Depth; // How much to distort the edge
        int range = Range; // How far the torn edges can go
        bool curvedEdges = CurvedEdges; // If true, will apply a curve to the tear

        int width = img.Width;
        int height = img.Height;

        img.Mutate(ctx =>
        {
            // Torn edge distortion along all 4 sides
            ctx.DrawImage(CreateTornEdgeImage(width, height, depth, range, curvedEdges), new Point(0, 0), 1);
        });

        return img;
    }

    private Image CreateTornEdgeImage(int width, int height, int depth, int range, bool curvedEdges)
    {
        var img = new Image<Rgba32>(width, height);

        // Creating the torn edges effect by manipulating the edges
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                if (x < depth || x > width - depth || y < depth || y > height - depth)
                {
                    // Simulate jaggedness along the edge
                    if (IsEdge(x, y, depth, range, curvedEdges))
                    {
                        img[x, y] = Color.Transparent; // Make torn edges transparent or use a color
                    }
                    else
                    {
                        img[x, y] = Color.White; // Set a background color
                    }
                }
                else
                {
                    img[x, y] = Color.Transparent;
                }
            }
        }

        return img;
    }

    private bool IsEdge(int x, int y, int depth, int range, bool curvedEdges)
    {
        // Randomly vary the edges to simulate the torn effect
        Random rand = new Random();
        int offset = rand.Next(-range, range);

        if (curvedEdges)
        {
            // Apply curvature to the edges for a smoother torn effect
            offset = (int)(Math.Sin(x * 0.1) * range);
        }

        return (x < depth + offset || x > depth + offset || y < depth + offset || y > depth + offset);
    }

    protected override string? GetSummary()
    {
        return $"{Depth}, {Range}";
    }
}
