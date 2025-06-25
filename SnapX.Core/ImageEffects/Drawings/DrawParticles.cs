
// SPDX-License-Identifier: GPL-3.0-or-later


using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using SnapX.Core.Utils;
using SnapX.Core.Utils.Extensions;
using SnapX.Core.Utils.Random;

namespace SnapX.Core.ImageEffects.Drawings;

[Description("Particles")]
public class DrawParticles : ImageEffect
{
    [DefaultValue("")]
    public string? ImageFolder { get; set; }

    private int imageCount;

    [DefaultValue(1)]
    public int ImageCount
    {
        get
        {
            return imageCount;
        }
        set
        {
            imageCount = value.Clamp(1, 1000);
        }
    }

    [DefaultValue(false)]
    public bool Background { get; set; }

    [DefaultValue(false)]
    public bool RandomSize { get; set; }

    [DefaultValue(64)]
    public int RandomSizeMin { get; set; }

    [DefaultValue(128)]
    public int RandomSizeMax { get; set; }

    [DefaultValue(false)]
    public bool RandomAngle { get; set; }

    [DefaultValue(0)]
    public int RandomAngleMin { get; set; }

    [DefaultValue(360)]
    public int RandomAngleMax { get; set; }

    [DefaultValue(false)]
    public bool RandomOpacity { get; set; }

    [DefaultValue(0)]
    public int RandomOpacityMin { get; set; }

    [DefaultValue(100)]
    public int RandomOpacityMax { get; set; }

    [DefaultValue(false)]
    public bool NoOverlap { get; set; }

    [DefaultValue(0)]
    public int NoOverlapOffset { get; set; }

    [DefaultValue(false)]
    public bool EdgeOverlap { get; set; }

    private List<Rectangle> imageRectangles = [];

    [UnconditionalSuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code", Justification = "<Pending>")]
    public DrawParticles()
    {
        this.ApplyDefaultPropertyValues();
    }
    public override Image Apply(Image img)
    {
        if (Background)
        {
            // Create a new image to hold the result
            var result = new Image<Rgba32>(img.Width, img.Height);

            // Draw particles from the folder
            DrawParticlesFromFolder(result, ImageFolder);

            // Draw the original image onto the result
            result.Mutate(ctx => ctx.DrawImage(img, new Point(0, 0), 1f));

            return result;
        }
        else
        {
            // If no background, directly draw the particles on the existing image
            DrawParticlesFromFolder(img.CloneAs<Rgba32>(), ImageFolder);
            return img;
        }
    }

    private void DrawParticlesFromFolder(Image<Rgba32> img, string? imageFolder)
    {
        imageFolder = FileHelpers.ExpandFolderVariables(imageFolder, true);

        if (!string.IsNullOrEmpty(imageFolder) && Directory.Exists(imageFolder))
        {
            string[] files = FileHelpers.GetFilesByExtensions(imageFolder, ".png", ".jpg").ToArray();

            if (files.Length > 0)
            {
                imageRectangles.Clear();

                using var imageCache = new SimpleImageCache();

                // Loop through the number of images to draw
                for (int i = 0; i < ImageCount; i++)
                {
                    // Pick a random image from the folder
                    string file = RandomFast.Pick(files);
                    var imgCached = imageCache.GetImage(file);

                    if (imgCached != null)
                    {
                        // Draw the image onto the main image
                        DrawImage(img, imgCached.CloneAs<Rgba32>());
                    }
                }
            }
        }
    }

    private void DrawImage(Image<Rgba32> img, Image<Rgba32> img2)
    {
        int width, height;

        // Calculate random size if necessary
        if (RandomSize)
        {
            int size = RandomFast.Next(Math.Min(RandomSizeMin, RandomSizeMax), Math.Max(RandomSizeMin, RandomSizeMax));
            width = size;
            height = size;

            if (img2.Width > img2.Height)
            {
                height = (int)Math.Round(size * ((double)img2.Height / img2.Width));
            }
            else if (img2.Width < img2.Height)
            {
                width = (int)Math.Round(size * ((double)img2.Width / img2.Height));
            }
        }
        else
        {
            width = img2.Width;
            height = img2.Height;
        }

        if (width < 1 || height < 1)
        {
            return;
        }

        int minOffsetX = EdgeOverlap ? -width + 1 : 0;
        int minOffsetY = EdgeOverlap ? -height + 1 : 0;
        int maxOffsetX = img.Width - (EdgeOverlap ? 0 : width) - 1;
        int maxOffsetY = img.Height - (EdgeOverlap ? 0 : height) - 1;

        Rectangle rect, overlapRect;
        int attemptCount = 0;

        // Try to place the image randomly without overlap
        do
        {
            attemptCount++;

            if (attemptCount > 1000)
            {
                return;
            }

            int x = RandomFast.Next(Math.Min(minOffsetX, maxOffsetX), Math.Max(minOffsetX, maxOffsetX));
            int y = RandomFast.Next(Math.Min(minOffsetY, maxOffsetY), Math.Max(minOffsetY, maxOffsetY));
            rect = new Rectangle(x, y, width, height);

            overlapRect = rect.Offset(NoOverlapOffset);
        } while (NoOverlap && imageRectangles.Any(x => x.IntersectsWith(overlapRect)));

        imageRectangles.Add(rect);

        // Apply rotation if needed
        if (RandomAngle)
        {
            float moveX = rect.X + (rect.Width / 2f);
            float moveY = rect.Y + (rect.Height / 2f);
            int rotate = RandomFast.Next(Math.Min(RandomAngleMin, RandomAngleMax), Math.Max(RandomAngleMin, RandomAngleMax));

            img.Mutate(ctx =>
            {
                // Create an affine transformation to combine translation and rotation
                ctx.Transform(
                    new AffineTransformBuilder()
                        .AppendTranslation(new PointF(-moveX, -moveY)) // Translate back to undo initial translate
                        .AppendRotationDegrees(rotate)            // Apply the rotation
                        .AppendTranslation(new PointF(moveX, moveY)));
            });
        }

        // Apply opacity if needed
        if (RandomOpacity)
        {
            float opacity = RandomFast.Next(Math.Min(RandomOpacityMin, RandomOpacityMax), Math.Max(RandomOpacityMin, RandomOpacityMax)) / 100f;

            img.Mutate(ctx => ctx
                .DrawImage(img2, rect.Location, opacity)
            );
        }
        else
        {
            img.Mutate(ctx => ctx.DrawImage(img2, rect.Location, 1f));
        }
    }


    protected override string? GetSummary()
    {
        if (!string.IsNullOrEmpty(ImageFolder))
        {
            return FileHelpers.GetFileNameSafe(ImageFolder);
        }

        return null;
    }
}
