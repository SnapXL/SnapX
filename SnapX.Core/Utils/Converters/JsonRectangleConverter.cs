using System.Text.Json;
using System.Text.Json.Serialization;
using SixLabors.ImageSharp;

namespace SnapX.Core.Utils.Converters;

public class JsonRectangleConverter : JsonConverter<Rectangle>
{
    public override Rectangle Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        string? value = reader.GetString();
        if (string.IsNullOrWhiteSpace(value))
        {
            return default;
        }

        var parts = value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        if (parts.Length != 4 ||
            !int.TryParse(parts[0], out int x) ||
            !int.TryParse(parts[1], out int y) ||
            !int.TryParse(parts[2], out int width) ||
            !int.TryParse(parts[3], out int height))
        {
            throw new JsonException($"Invalid rectangle format: '{value}'");
        }

        return new Rectangle(x, y, width, height);
    }

    public override void Write(Utf8JsonWriter writer, Rectangle value, JsonSerializerOptions options)
    {
        string formatted = $"{value.X}, {value.Y}, {value.Width}, {value.Height}";
        writer.WriteStringValue(formatted);
    }
}
