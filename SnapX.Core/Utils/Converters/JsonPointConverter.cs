using System.Text.Json;
using System.Text.Json.Serialization;
using SixLabors.ImageSharp;
using SnapX.Core.Utils.Extensions;

namespace SnapX.Core.Utils.Converters;

public class JsonPointConverter : JsonConverter<Point>
{
    public override Point Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        string? value = reader.GetString();
        if (string.IsNullOrWhiteSpace(value))
        {
            return default;
        }

        var parts = value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        if (parts.Length != 2 ||
            !int.TryParse(parts[0], out int x) ||
            !int.TryParse(parts[1], out int y))
        {
            throw new JsonException(
                $"Invalid point format: '{value}' " +
                $"at position: {reader.GetPosition() ?? (0, 0)}, " +
                $"Type: {typeToConvert}."
            );
        }

        return new Point(x, y);
    }

    public override void Write(Utf8JsonWriter writer, Point value, JsonSerializerOptions options)
    {
        string formatted = $"{value.X}, {value.Y}";
        writer.WriteStringValue(formatted);
    }
}
