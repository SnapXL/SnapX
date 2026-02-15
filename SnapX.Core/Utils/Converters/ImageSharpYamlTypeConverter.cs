using System.Globalization;
using System.Reflection;
using SixLabors.ImageSharp;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.Serialization;

namespace SnapX.Core.Utils.Converters;

public class ImageSharpYamlTypeConverter : IYamlTypeConverter
{
    public bool Accepts(Type type) =>
        type == typeof(Color) ||
        type == typeof(Point) ||
        type == typeof(PointF) ||
        type == typeof(Rectangle) ||
        type == typeof(Size) ||
        type == typeof(SizeF);

    public object ReadYaml(IParser parser, Type type, ObjectDeserializer deserializer)
    {
        var scalar = parser.Consume<Scalar>();
        var value = scalar.Value;

        return type switch
        {
            Type t when t == typeof(Color) => ParseColor(value),
            Type t when t == typeof(Point) => ParsePoint(value),
            Type t when t == typeof(PointF) => ParsePointF(value),
            Type t when t == typeof(Rectangle) => ParseRectangle(value),
            Type t when t == typeof(Size) => ParseSize(value),
            Type t when t == typeof(SizeF) => ParseSizeF(value),
            _ => throw new YamlException($"Unsupported type: {type}")
        };
    }

    public void WriteYaml(IEmitter emitter, object? value, Type type, ObjectSerializer serializer)
    {
        var str = value switch
        {
            Color c => FormatColor(c),
            Point p => $"{p.X}, {p.Y}",
            PointF pf => $"{pf.X.ToString(CultureInfo.InvariantCulture)}, {pf.Y.ToString(CultureInfo.InvariantCulture)}",
            Rectangle r => $"{r.X}, {r.Y}, {r.Width}, {r.Height}",
            Size s => $"{s.Width}, {s.Height}",
            SizeF sf => $"{sf.Width.ToString(CultureInfo.InvariantCulture)}, {sf.Height.ToString(CultureInfo.InvariantCulture)}",
            _ => throw new YamlException($"Unsupported type: {type}")
        };
        emitter.Emit(new Scalar(str));
    }

    private Color ParseColor(string s)
    {
        var named = TryParseNamedColor(s);
        if (named.HasValue) return named.Value;
        if (s.StartsWith("#")) return Color.ParseHex(s);
        var rgb = TryParseRgb(s);
        if (rgb.HasValue) return rgb.Value;
        throw new YamlException($"Invalid color format: '{s}'");
    }

    private Color? TryParseNamedColor(string name)
    {
        var field = typeof(Color).GetField(name, BindingFlags.Public | BindingFlags.Static | BindingFlags.IgnoreCase);
        if (field?.FieldType == typeof(Color))
            return (Color)field.GetValue(null)!;
        return null;
    }

    private Color? TryParseRgb(string s)
    {
        var parts = s.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length >= 3 && parts.Length <= 4 &&
            byte.TryParse(parts[0], out var r) &&
            byte.TryParse(parts[1], out var g) &&
            byte.TryParse(parts[2], out var b))
        {
            var a = (byte)255;
            if (parts.Length == 4 && !byte.TryParse(parts[3], out a))
                return null;
            return Color.FromRgba(r, g, b, a);
        }
        return null;
    }

    private string FormatColor(Color c)
    {
        var name = TryGetNamedColorName(c);
        return name ?? "#" + c.ToHex();
    }

    private string? TryGetNamedColorName(Color color)
    {
        foreach (var field in typeof(Color).GetFields(BindingFlags.Public | BindingFlags.Static))
        {
            if (field.FieldType == typeof(Color) && Equals(field.GetValue(null), color))
                return field.Name;
        }
        return null;
    }

    private Point ParsePoint(string s)
    {
        var parts = s.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length == 2 &&
            int.TryParse(parts[0], out var x) &&
            int.TryParse(parts[1], out var y))
            return new Point(x, y);
        throw new YamlException($"Invalid point format: '{s}'");
    }

    private PointF ParsePointF(string s)
    {
        var parts = s.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length == 2 &&
            float.TryParse(parts[0], NumberStyles.Float, CultureInfo.InvariantCulture, out var x) &&
            float.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out var y))
            return new PointF(x, y);
        throw new YamlException($"Invalid PointF format: '{s}'");
    }

    private Rectangle ParseRectangle(string s)
    {
        var parts = s.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length == 4 &&
            int.TryParse(parts[0], out var x) &&
            int.TryParse(parts[1], out var y) &&
            int.TryParse(parts[2], out var w) &&
            int.TryParse(parts[3], out var h))
            return new Rectangle(x, y, w, h);
        throw new YamlException($"Invalid rectangle format: '{s}'");
    }

    private Size ParseSize(string s)
    {
        var parts = s.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length == 2 &&
            int.TryParse(parts[0], out var w) &&
            int.TryParse(parts[1], out var h))
            return new Size(w, h);
        throw new YamlException($"Invalid size format: '{s}'");
    }

    private SizeF ParseSizeF(string s)
    {
        var parts = s.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length == 2 &&
            float.TryParse(parts[0], NumberStyles.Float, CultureInfo.InvariantCulture, out var w) &&
            float.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out var h))
            return new SizeF(w, h);
        throw new YamlException($"Invalid SizeF format: '{s}'");
    }
}
