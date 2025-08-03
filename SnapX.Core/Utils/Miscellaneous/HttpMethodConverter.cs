using System.Text.Json;
using System.Text.Json.Serialization;

namespace SnapX.Core.Utils.Miscellaneous;

public class HttpMethodConverter : JsonConverter<HttpMethod>
{
    public override HttpMethod Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var method = reader.GetString();
        return HttpMethod.Parse(method);
    }

    public override void Write(Utf8JsonWriter writer, HttpMethod value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString());
    }
}
