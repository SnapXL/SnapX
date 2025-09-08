using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using SnapX.Core.ImageEffects;

namespace SnapX.Core.Utils.Converters;

public class JsonPaddingConverter : JsonConverter<Padding>
{
    public override Padding Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        string[] parts = reader.GetString()?.Split(',') ?? throw new JsonException();
        if (parts.Length != 4) throw new JsonException("Invalid padding format.");

        return new Padding(
            int.Parse(parts[0].Trim(), CultureInfo.InvariantCulture),
            int.Parse(parts[1].Trim(), CultureInfo.InvariantCulture),
            int.Parse(parts[2].Trim(), CultureInfo.InvariantCulture),
            int.Parse(parts[3].Trim(), CultureInfo.InvariantCulture)
        );
    }

    public override void Write(Utf8JsonWriter writer, Padding value, JsonSerializerOptions options)
    {
        writer.WriteStringValue($"{value.Left}, {value.Top}, {value.Right}, {value.Bottom}");
    }
}
