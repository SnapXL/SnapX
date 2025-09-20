using System.Text.Json;
using System.Text.Json.Serialization;

namespace SnapX.Core.Utils.Converters;


public class SafeEnumConverter<T> : JsonConverter<T> where T : struct, Enum
{
    public override T Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        switch (reader.TokenType)
        {
            case JsonTokenType.String:
                {
                    var enumString = reader.GetString();
                    if (Enum.TryParse(enumString, ignoreCase: true, out T value))
                    {
                        return value;
                    }

                    break;
                }
            case JsonTokenType.Number when reader.TryGetInt32(out int intValue) && Enum.IsDefined(typeof(T), intValue):
                return (T)Enum.ToObject(typeof(T), intValue);
        }

        return default;
    }

    public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString());
    }
}
