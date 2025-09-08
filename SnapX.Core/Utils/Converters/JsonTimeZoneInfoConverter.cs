using System.Text.Json;
using System.Text.Json.Serialization;

namespace SnapX.Core.Utils.Converters;


public class JsonTimeZoneInfoConverter : JsonConverter<TimeZoneInfo>
{
    public override TimeZoneInfo? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartObject)
            throw new JsonException("Expected StartObject token");

        string? id = null;
        string? displayName = null;
        string? standardName = null;
        string? daylightName = null;
        var baseUtcOffset = TimeSpan.Zero;
        var supportsDaylightSavingTime = false;

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
                break;

            if (reader.TokenType != JsonTokenType.PropertyName)
                continue;

            var propertyName = reader.GetString()!;

            reader.Read();

            switch (propertyName)
            {
                case "Id":
                    id = reader.GetString();
                    break;
                case "DisplayName":
                    displayName = reader.GetString();
                    break;
                case "StandardName":
                    standardName = reader.GetString();
                    break;
                case "DaylightName":
                    daylightName = reader.GetString();
                    break;
                case "BaseUtcOffset":
                    baseUtcOffset = TimeSpan.Parse(reader.GetString()!);
                    break;
                case "SupportsDaylightSavingTime":
                    supportsDaylightSavingTime = reader.GetBoolean();
                    break;
                case "AdjustmentRules":
                    if (reader.TokenType == JsonTokenType.Null)
                    {
                        // Already at null, nothing to do
                    }
                    else if (reader.TokenType == JsonTokenType.StartArray)
                    {
                        // Consume the whole array manually
                        int depth = 0;
                        do
                        {
                            if (reader.TokenType == JsonTokenType.StartArray || reader.TokenType == JsonTokenType.StartObject)
                                depth++;
                            else if (reader.TokenType == JsonTokenType.EndArray || reader.TokenType == JsonTokenType.EndObject)
                                depth--;
                        } while (depth > 0 && reader.Read());
                    }
                    else
                    {
                        // Unknown token, just read once
                        reader.Read();
                    }
                    break;
                default:
                    // Skip unknown properties, will throw
                    reader.Skip();
                    break;
            }
        }

        if (id == null || standardName == null || displayName == null || daylightName == null)
            throw new JsonException("Missing required properties for TimeZoneInfo");

        // Create custom TimeZoneInfo using CreateCustomTimeZone
        return TimeZoneInfo.CreateCustomTimeZone(
            id,
            baseUtcOffset,
            displayName,
            standardName,
            daylightName,
            supportsDaylightSavingTime ? new TimeZoneInfo.AdjustmentRule[0] : null // No adjustment rules implemented here
        );
    }

    public override void Write(Utf8JsonWriter writer, TimeZoneInfo value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();

        writer.WriteString("Id", value.Id);
        writer.WriteString("DisplayName", value.DisplayName);
        writer.WriteString("StandardName", value.StandardName);
        writer.WriteString("DaylightName", value.DaylightName);
        writer.WriteString("BaseUtcOffset", value.BaseUtcOffset.ToString());
        writer.WriteNull("AdjustmentRules");
        writer.WriteBoolean("SupportsDaylightSavingTime", value.SupportsDaylightSavingTime);

        writer.WriteEndObject();
    }
}
