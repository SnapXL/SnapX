using System.ComponentModel;
using System.Text.Json.Serialization;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using SnapX.Core.Utils;

namespace SnapX.Core.Media;

public enum LinearGradientMode
{
    Horizontal,
    Vertical
}

public record GradientStop(Color Color, float Location)
{
    public Color Color { get; set; } = Color;
    public float Location { get; set; } = Location; // 0-100
}

public class GradientInfo(LinearGradientMode Type)
{
    [DefaultValue(LinearGradientMode.Vertical)]
    public LinearGradientMode Type { get; set; } = Type;

    /// <summary>
    /// Gradient color stops, each with a Ratio in [0,1]
    /// </summary>
    public List<ColorStop> Colors { get; set; } = new();

    [JsonIgnore]
    public bool IsValid => Colors is { Count: > 0 };

    [JsonIgnore]
    public bool IsVisible => IsValid && Colors.Any(x => x.Color.ToPixel<Rgba32>().A > 0);

    [JsonIgnore]
    public bool IsTransparent => IsValid && Colors.Any(x => x.Color.ToPixel<Rgba32>().A < 255);

    public GradientInfo() : this(LinearGradientMode.Vertical) { }

    public GradientInfo(LinearGradientMode type, params ColorStop[] colors) : this(type)
    {
        Colors = colors.ToList();
    }

    public GradientInfo(LinearGradientMode type, params Color[] colors) : this(type)
    {
        var count = colors.Length;
        for (var i = 0; i < count; i++)
        {
            var ratio = (count == 1) ? 0f : i / (float)(count - 1);
            Colors.Add(new ColorStop(ratio, colors[i]));
        }
    }

    public GradientInfo(params ColorStop[] colors) : this(LinearGradientMode.Vertical, colors) { }

    public GradientInfo(params Color[] colors) : this(LinearGradientMode.Vertical, colors) { }

    public void Clear()
    {
        Colors.Clear();
    }

    public void Sort()
    {
        Colors.Sort((x, y) => x.Ratio.CompareTo(y.Ratio));
    }

    public void Reverse()
    {
        Colors = Colors
            .Select(s => new ColorStop(1f - s.Ratio, s.Color))
            .OrderBy(s => s.Ratio)
            .ToList();
    }

    /// <summary>
    /// Returns an ImageSharp LinearGradientBrush based on this gradient info.
    /// </summary>
    public Brush GetGradientBrush(RectangleF rect)
    {
        var stops = Colors
            .OrderBy(x => x.Ratio)
            .ToArray();

        if (stops.All(x => x.Ratio != 0f))
        {
            var first = stops.FirstOrDefault();
            stops = new[] { new ColorStop(0f, first.Color) }.Concat(stops).ToArray();
        }

        if (stops.All(x => x.Ratio != 1f))
        {
            var last = stops.LastOrDefault();
            stops = stops.Append(new ColorStop(1f, last.Color)).ToArray();
        }

        var start = new PointF(rect.Left, rect.Top);

        var end = Type == LinearGradientMode.Horizontal
            ? new PointF(rect.Right, rect.Top)
            : new PointF(rect.Left, rect.Bottom);

        return new LinearGradientBrush(start, end, GradientRepetitionMode.Repeat, stops);
    }

    public Image<Rgba32> CreateGradientPreview(int width, int height, bool border = false, bool checkers = false)
    {
        var image = new Image<Rgba32>(width, height);

        if (checkers && IsTransparent)
        {
            ImageHelpers.DrawCheckerPattern(image, 10, [Color.LightGray, Color.White]);
        }

        var brush = GetGradientBrush(new RectangleF(0, 0, width, height));

        image.Mutate(ctx =>
        {
            ctx.Fill(brush);

            if (border)
            {
                ctx.Draw(Color.Black, 1, new RectangleF(0, 0, width - 1, height - 1));
            }
        });

        return image;
    }

    public override string ToString() => "Gradient";
}
