using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.Serialization;

namespace SnapX.Core.Utils.Converters;

public class HttpMethodYamlConverter : IYamlTypeConverter
{
    public bool Accepts(Type type)
        => type == typeof(HttpMethod);

    public object? ReadYaml(IParser parser, Type type, ObjectDeserializer rootDeserializer)
    {
        if (parser.TryConsume<Scalar>(out var scalar))
        {
            var value = scalar.Value;

            if (string.IsNullOrWhiteSpace(value))
                return HttpMethod.Post;

            return HttpMethod.Parse(value.ToUpperInvariant());
        }

        throw new YamlException($"Invalid YAML value for {nameof(HttpMethod)}.");
    }

    public void WriteYaml(IEmitter emitter, object? value, Type type, ObjectSerializer serializer)
    {
        if (value is HttpMethod method)
        {
            emitter.Emit(new Scalar(method.Method));
            return;
        }

        emitter.Emit(new Scalar(string.Empty));
    }
}
