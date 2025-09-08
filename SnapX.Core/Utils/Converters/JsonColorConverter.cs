using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using SixLabors.ImageSharp;

namespace SnapX.Core.Utils.Converters;

public class JsonColorConverter : JsonConverter<Color>
{
    public override Color Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.String)
        {
            throw new JsonException("JSON color value must be a string.");
        }

        string? colorStr = reader.GetString();

        if (string.IsNullOrWhiteSpace(colorStr))
        {
            throw new JsonException("JSON color string cannot be empty.");
        }

        // Try to parse named color first (e.g. "Red")
        var knownColor = TryParseKnownColor(colorStr);
        if (knownColor is not null)
        {
            return knownColor.Value;
        }

        return colorStr.StartsWith("#")
            ? Color.ParseHex(colorStr)
            : TryParseRgb(colorStr) ?? throw new JsonException($"Cannot convert '{colorStr}' to ImageSharp Color.");

    }
    private Color? TryParseRgb(string value)
    {
        var parts = value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        if (parts.Length is >= 3 and <= 4 &&
            byte.TryParse(parts[0], out byte r) &&
            byte.TryParse(parts[1], out byte g) &&
            byte.TryParse(parts[2], out byte b))
        {
            // If alpha is provided, parse it; otherwise assume 255
            byte a = 255;
            if (parts.Length == 4 && !byte.TryParse(parts[3], out a))
            {
                return null; // Invalid alpha
            }

            return Color.FromRgba(r, g, b, a);
        }

        return null;
    }

    public override void Write(Utf8JsonWriter writer, Color value, JsonSerializerOptions options)
    {
        var knownColorName = TryGetKnownColorName(value);
        writer.WriteStringValue(!string.IsNullOrEmpty(knownColorName) ? knownColorName : "#" + value.ToHex());
    }

    [UnconditionalSuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code", Justification = "<Pending>")]
    [RequiresUnreferencedCode("Checks SixLabors predefined colors")]
    private Color? TryParseKnownColor(string name)
    {
        var colorProperty = typeof(Color).GetField(
            name,
            BindingFlags.Public | BindingFlags.Static | BindingFlags.IgnoreCase);
        if (colorProperty is not null && colorProperty.FieldType == typeof(Color))
        {
            return (Color)colorProperty.GetValue(null)!;
        }

        return null;
    }
    [UnconditionalSuppressMessage(
        "Trimming",
        "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code",
        Justification = "Uses reflection to access SixLabors.Color predefined colors, which may be trimmed.")]
    [RequiresUnreferencedCode("Checks SixLabors predefined colors via reflection")]
    private string? TryGetKnownColorName(Color color)
    {
        var properties = typeof(Color).GetFields(BindingFlags.Public | BindingFlags.Static);
        return (from property in properties
                where property.FieldType == typeof(Color) && Equals(property.GetValue(null), color)
                select property.Name).FirstOrDefault();
    }
}
