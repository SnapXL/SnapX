using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SnapX.Core.Utils.Converters;

/// <summary>
/// It serializes and deserializes the UIFont to and from a string
/// in the format "Font Name, Sizept".
/// </summary>
public class JsonFontConverter : JsonConverter<Theme.UIFont>
{
    /// <summary>
    /// Reads and converts the JSON to a UIFont object.
    /// </summary>
    /// <param name="reader">The reader to read from.</param>
    /// <param name="typeToConvert">The type of the object to convert.</param>
    /// <param name="options">An object that specifies serialization options.</param>
    /// <returns>A new UIFont instance.</returns>
    public override Theme.UIFont Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.String)
        {
            throw new JsonException("UIFont value must be a string.");
        }

        var fontString = reader.GetString();

        if (string.IsNullOrWhiteSpace(fontString))
        {
            throw new JsonException("UIFont string cannot be empty or null.");
        }

        var parts = fontString.Split(',');

        if (parts.Length != 2)
        {
            throw new JsonException($"Invalid UIFont format: '{fontString}'. Expected format is 'Name, Sizept'.");
        }

        var fontName = parts[0].Trim();

        var sizeString = parts[1].Trim().Replace("pt", "", StringComparison.OrdinalIgnoreCase);

        return float.TryParse(sizeString, NumberStyles.Float, CultureInfo.InvariantCulture, out float fontSize) ? new Theme.UIFont(fontName, fontSize) : throw new JsonException($"Invalid font size format in string: '{fontString}'.");
    }

    /// <summary>
    /// Writes a UIFont object as JSON.
    /// </summary>
    /// <param name="writer">The writer to write to.</param>
    /// <param name="value">The value to convert.</param>
    /// <param name="options">An object that specifies serialization options.</param>
    public override void Write(Utf8JsonWriter writer, Theme.UIFont value, JsonSerializerOptions options)
    {
        if (value is not null)
        {
            // Format the string as "Name, Sizept" using the InvariantCulture for consistent decimal formatting.
            var formattedString = $"{value.Name}, {value.Size.ToString(CultureInfo.InvariantCulture)}pt";
            writer.WriteStringValue(formattedString);
        }
        else
        {
            writer.WriteNullValue();
        }
    }
}
