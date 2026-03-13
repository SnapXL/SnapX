using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.Serialization;

namespace SnapX.Core.Utils.Converters;

public class UIFontYamlTypeConverter : IYamlTypeConverter
{
    private readonly UIFontTypeConverter _inner = new();

    public bool Accepts(Type type) => type == typeof(Theme.UIFont);

    public object ReadYaml(IParser parser, Type type, ObjectDeserializer deserializer)
    {
        var scalar = parser.Consume<Scalar>();
        return _inner.ConvertFromInvariantString(scalar.Value)!;
    }

    public void WriteYaml(IEmitter emitter, object? value, Type type, ObjectSerializer serializer)
    {
        var str = _inner.ConvertToInvariantString(value);
        emitter.Emit(new Scalar(str));
    }
}
