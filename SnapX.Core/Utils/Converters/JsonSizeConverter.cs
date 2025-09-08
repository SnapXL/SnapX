using System.Text.Json;
using System.Text.Json.Serialization;
using SixLabors.ImageSharp;

namespace SnapX.Core.Utils.Converters;

public class JsonSizeConverter : JsonConverter<Size>
{
    public override Size Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        string? value = reader.GetString();
        if (string.IsNullOrWhiteSpace(value))
        {
            return default;
        }

        var parts = value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        if (parts.Length != 2 ||
            !int.TryParse(parts[0], out int width) ||
            !int.TryParse(parts[1], out int height))
        {
            throw new JsonException($"Invalid size format: '{value}'");
        }

        return new Size(width, height);
    }

    public override void Write(Utf8JsonWriter writer, Size value, JsonSerializerOptions options)
    {
        var formatted = $"{value.Width}, {value.Height}";
        writer.WriteStringValue(formatted);
    }
}
