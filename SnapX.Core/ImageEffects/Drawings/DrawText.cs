
// SPDX-License-Identifier: GPL-3.0-or-later


using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.Processing;
using SnapX.Core.Utils;
using SnapX.Core.Utils.Extensions;
using SnapX.Core.Utils.Parsers;

namespace SnapX.Core.ImageEffects.Drawings;

[Description("Text watermark")]
public class DrawText : ImageEffect
{
    [DefaultValue("Text watermark")]
    public string? Text { get; set; }

    [DefaultValue(AnchorStyles.BottomRight)]
    public AnchorStyles Placement { get; set; }

    [DefaultValue(typeof(Point), "5, 5")]
    public Point Offset { get; set; }

    [DefaultValue(false), Description("If text watermark size bigger than source image then don't draw it.")]
    public bool AutoHide { get; set; }

    [DefaultValue(typeof(Font), "Arial, 11.25pt")]
    public Font TextFont => new(new FontFamily(), 11.25f, FontStyle.Regular);


    [DefaultValue(typeof(Color), "235, 235, 235")]
    public Color TextColor { get; set; }

    [DefaultValue(true)]
    public bool DrawTextShadow { get; set; }

    [DefaultValue(typeof(Color), "Black")]
    public Color TextShadowColor { get; set; }

    [DefaultValue(typeof(Point), "-1, -1")]
    public Point TextShadowOffset { get; set; }

    private int cornerRadius;

    [DefaultValue(4)]
    public int CornerRadius
    {
        get
        {
            return cornerRadius;
        }
        set
        {
            cornerRadius = value.Max(0);
        }
    }

    [DefaultValue(typeof(Padding), "5, 5, 5, 5")]
    public Padding Padding { get; set; }

    [DefaultValue(true)]
    public bool DrawBorder { get; set; }

    [DefaultValue(typeof(Color), "Black")]
    public Color BorderColor { get; set; }

    [DefaultValue(1)]
    public int BorderSize { get; set; }

    [DefaultValue(true)]
    public bool DrawBackground { get; set; }

    [DefaultValue(typeof(Color), "42, 47, 56")]
    public Color BackgroundColor { get; set; }

    [DefaultValue(false)]
    public bool UseGradient { get; set; }

    public GradientBrush Gradient { get; set; }

    [UnconditionalSuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code", Justification = "<Pending>")]
    public DrawText()
    {
        this.ApplyDefaultPropertyValues();
        AddDefaultGradient();
    }

    private void AddDefaultGradient()
    {
        Gradient = new LinearGradientBrush(
            new PointF(0, 0), // Start point of the gradient
            new PointF(1, 1), // End point of the gradient
            GradientRepetitionMode.Repeat
        );
    }

    public override Image Apply(Image img)
    {
        if (string.IsNullOrEmpty(Text))
        {
            return img;
        }

        // Ensure TextFont is available
        if (TextFont == null || TextFont.Size < 1)
        {
            return img;
        }

        // Name parser equivalent
        NameParser parser = new NameParser(NameParserType.Text);
        parser.ImageWidth = img.Width;
        parser.ImageHeight = img.Height;

        string? parsedText = parser.Parse(Text);

        // Measure the text size
        var font = TextFont;
        var textSize = TextMeasurer.MeasureSize(parsedText, new TextOptions(font));
        var watermarkSize = new Size(Padding.Left + (int)textSize.Width + Padding.Right, Padding.Top + (int)textSize.Height + Padding.Bottom);

        var watermarkPosition = ImageHelpers.GetPosition(Placement, Offset, img.Size, watermarkSize);
        var watermarkRectangle = new Rectangle(watermarkPosition, watermarkSize);

        // Check if the watermark should be hidden
        if (AutoHide && !new Rectangle(0, 0, img.Width, img.Height).Contains(watermarkRectangle))
        {
            return img;
        }

        img.Mutate(ctx =>
        {
            // Draw background for the watermark (if enabled)
            if (DrawBackground)
            {
                Brush backgroundBrush;

                if (UseGradient && Gradient != null)
                {
                    var gradientBrush = new LinearGradientBrush(
                        new PointF(watermarkRectangle.Left, watermarkRectangle.Top),
                        new PointF(watermarkRectangle.Right, watermarkRectangle.Bottom),
                        GradientRepetitionMode.Repeat
                    );
                    backgroundBrush = gradientBrush;
                }
                else
                {
                    backgroundBrush = new SolidBrush(BackgroundColor);
                }

                ctx.Fill(backgroundBrush, watermarkRectangle);
            }

            if (DrawBorder)
            {
                int borderSize = BorderSize.Max(1);

                var borderPen = new SolidPen(BorderColor, borderSize);
                var topLeft = watermarkRectangle.Location;
                var topRight = new PointF(watermarkRectangle.Right, watermarkRectangle.Top);
                var bottomRight = new PointF(watermarkRectangle.Right, watermarkRectangle.Bottom);
                var bottomLeft = new PointF(watermarkRectangle.Left, watermarkRectangle.Bottom);

                ctx.DrawPolygon(borderPen, [topLeft, topRight, bottomRight, bottomLeft]);
            }

            // Set text rendering options
            var textOptions = new RichTextOptions(font)
            {
                WrappingLength = 0,
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Top,
            };

            ctx.DrawText(textOptions, parsedText, new SolidBrush(TextColor));
        });

        return img;
    }

    protected override string? GetSummary()
    {
        if (!string.IsNullOrEmpty(Text))
        {
            return Text.Truncate(20, "...");
        }

        return null;
    }
}
