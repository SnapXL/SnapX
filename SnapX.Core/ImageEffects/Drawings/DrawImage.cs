
// SPDX-License-Identifier: GPL-3.0-or-later


using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using SnapX.Core.Utils;
using SnapX.Core.Utils.Extensions;

namespace SnapX.Core.ImageEffects.Drawings;

[Description("Image")]
public class DrawImage : ImageEffect
{
    [DefaultValue("")]
    public string? ImageLocation { get; set; }

    [DefaultValue(AnchorStyles.TopLeft)]
    public AnchorStyles Placement { get; set; }

    [DefaultValue(typeof(Point), "0, 0")]
    public Point Offset { get; set; }

    [DefaultValue(DrawImageSizeMode.DontResize), Description("How the image watermark should be rescaled, if at all.")]
    public DrawImageSizeMode SizeMode { get; set; }

    [DefaultValue(typeof(Size), "0, 0")]
    public Size Size { get; set; }

    [DefaultValue(ImageRotateFlipType.None)]
    public ImageRotateFlipType RotateFlip { get; set; }

    [DefaultValue(false)]
    public bool Tile { get; set; }

    [DefaultValue(false), Description("If image watermark size bigger than source image then don't draw it.")]
    public bool AutoHide { get; set; }

    [DefaultValue(ImageInterpolationMode.HighQualityBicubic)]
    public ImageInterpolationMode InterpolationMode { get; set; }

    private int opacity;

    [DefaultValue(100)]
    public int Opacity
    {
        get
        {
            return opacity;
        }
        set
        {
            opacity = value.Clamp(0, 100);
        }
    }

    [UnconditionalSuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code", Justification = "<Pending>")]
    public DrawImage()
    {
        this.ApplyDefaultPropertyValues();
    }

    public override Image Apply(Image img)
    {
        if (Opacity < 1 || (SizeMode != DrawImageSizeMode.DontResize && Size.Width <= 0 && Size.Height <= 0))
        {
            return img;
        }

        var imageFilePath = FileHelpers.ExpandFolderVariables(ImageLocation, true);

        if (!string.IsNullOrEmpty(imageFilePath) && File.Exists(imageFilePath))
        {
            using var watermark = Image.Load(imageFilePath);
            // Apply rotation/flip if necessary
            if (RotateFlip != ImageRotateFlipType.None)
            {
                watermark.Mutate(ctx =>
                {
                    if (RotateFlip == ImageRotateFlipType.Rotate90)
                        ctx.Rotate(90);
                    else if (RotateFlip == ImageRotateFlipType.Rotate180)
                        ctx.Rotate(180);
                    else if (RotateFlip == ImageRotateFlipType.Rotate270)
                        ctx.Rotate(270);
                    else if (RotateFlip == ImageRotateFlipType.FlipX)
                        ctx.Flip(FlipMode.Horizontal);
                    else if (RotateFlip == ImageRotateFlipType.FlipY)
                        ctx.Flip(FlipMode.Vertical);
                });
            }

            Size imageSize;
            // Calculate watermark size based on SizeMode
            if (SizeMode == DrawImageSizeMode.AbsoluteSize)
            {
                var width = Size.Width == -1 ? img.Width : Size.Width;
                var height = Size.Height == -1 ? img.Height : Size.Height;
                imageSize = ImageHelpers.ApplyAspectRatio(width, height, watermark);
            }
            else if (SizeMode == DrawImageSizeMode.PercentageOfWatermark)
            {
                var width = (int)Math.Round(Size.Width / 100f * watermark.Width);
                var height = (int)Math.Round(Size.Height / 100f * watermark.Height);
                imageSize = ImageHelpers.ApplyAspectRatio(width, height, watermark);
            }
            else if (SizeMode == DrawImageSizeMode.PercentageOfCanvas)
            {
                var width = (int)Math.Round(Size.Width / 100f * img.Width);
                var height = (int)Math.Round(Size.Height / 100f * img.Height);
                imageSize = ImageHelpers.ApplyAspectRatio(width, height, watermark);
            }
            else
            {
                imageSize = watermark.Size;
            }

            var imagePosition = ImageHelpers.GetPosition(Placement, Offset, img.Size, imageSize);
            var imageRectangle = new Rectangle(imagePosition, imageSize);

            // If AutoHide is enabled and the watermark is outside the image, don't apply it
            if (AutoHide && !new Rectangle(0, 0, img.Width, img.Height).Contains(imageRectangle))
            {
                return img;
            }

            img.Mutate(ctx =>
            {
                if (Tile)
                {
                    // Tile the watermark across the image
                    ctx.DrawImage(watermark, new Point(imageRectangle.X, imageRectangle.Y), 1f);
                }
                else
                {
                    // Apply opacity to the watermark
                    var opacityValue = Opacity / 100f;

                    // If opacity is less than 100%, apply alpha blending
                    if (opacityValue < 1f)
                    {
                        ctx.DrawImage(watermark, new Point(imageRectangle.X, imageRectangle.Y), opacityValue);
                    }
                    else
                    {
                        // No opacity change, just draw the image
                        ctx.DrawImage(watermark, new Point(imageRectangle.X, imageRectangle.Y), 1f);
                    }
                }
            });
        }

        return img;
    }

    protected override string? GetSummary() =>
        string.IsNullOrEmpty(ImageLocation) ? FileHelpers.GetFileNameSafe(ImageLocation) : null;
}
