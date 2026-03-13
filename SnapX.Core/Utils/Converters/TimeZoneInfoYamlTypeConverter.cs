using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.Serialization;

namespace SnapX.Core.Utils.Converters;

public class TimeZoneInfoYamlTypeConverter : IYamlTypeConverter
{
    public bool Accepts(Type type) => type == typeof(TimeZoneInfo);

    public object ReadYaml(IParser parser, Type type, ObjectDeserializer deserializer)
    {
        var scalar = parser.Consume<Scalar>();
        var id = scalar.Value;
        if (string.IsNullOrEmpty(id)) return null;
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById(id);
        }
        catch (TimeZoneNotFoundException)
        {
            // Fallback for known IDs like "UTC" that might not be found on some platforms
            if (string.Equals(id, "UTC", StringComparison.OrdinalIgnoreCase))
                return TimeZoneInfo.Utc;
            throw;
        }
    }

    public void WriteYaml(IEmitter emitter, object? value, Type type, ObjectSerializer serializer)
    {
        var timeZone = value as TimeZoneInfo;
        var id = timeZone?.Id ?? string.Empty;
        emitter.Emit(new Scalar(id));
    }
}
