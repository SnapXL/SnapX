using System.Collections.ObjectModel;
using SnapX.Core.Upload.Custom;
using SnapX.Core.Utils.Extensions;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.Serialization;

namespace SnapX.Core.Utils.Converters;

public class HeaderCollectionYamlConverter(SecurePropertyStore Store, Func<IEnumerable<string>> GetSensitiveKeys, bool UseEncryption = true)
    : IYamlTypeConverter
{
    public bool Accepts(Type type) => type == typeof(ObservableCollection<HeaderItem>);

    public object? ReadYaml(IParser parser, Type type, ObjectDeserializer rootDeserializer)
    {
        if (parser.Accept<Scalar>(out var scalar) &&
            (string.IsNullOrWhiteSpace(scalar.Value) || scalar.Value == "null"))
        {
            parser.Consume<Scalar>();
            return new ObservableCollection<HeaderItem>();
        }

        var result = rootDeserializer(typeof(object));

        if (result is not Dictionary<object, object> rawDict) return new ObservableCollection<HeaderItem>();
        var collection = new ObservableCollection<HeaderItem>();
        foreach (var kvp in rawDict)
        {
            var key = kvp.Key?.ToString();
            var value = kvp.Value?.ToString();

            if (string.IsNullOrWhiteSpace(key)) continue;
            if (value?.StartsWith(SecurePropertyStore.Header) == true)
            {
                try
                {
                    value = Store.Unprotect(value);
                }
                catch (Exception ex)
                {
                    ex.ShowError(true, "Reading encrypted value");
                }
            }

            collection.Add(new HeaderItem
            {
                Key = key,
                Value = value ?? string.Empty
            });
        }
        return collection;

    }

    public void WriteYaml(IEmitter emitter, object? value, Type type, ObjectSerializer serializer)
    {
        var headers = value as ObservableCollection<HeaderItem> ?? [];
        var sensitiveKeys = GetSensitiveKeys()?.ToList() ?? [];
        var sensitive = new HashSet<string>(sensitiveKeys, StringComparer.OrdinalIgnoreCase);

        emitter.Emit(new MappingStart(null, null, true, MappingStyle.Block));

        foreach (var header in headers.Where(x => !string.IsNullOrWhiteSpace(x.Key)))
        {
            var key = header.Key ?? string.Empty;
            var val = header.Value ?? string.Empty;

            if (sensitive.Contains(key) && !string.IsNullOrWhiteSpace(val) && !val.StartsWith(SecurePropertyStore.Header) && UseEncryption)
            {
                val = Store.Protect(val);
            }
            else if (!UseEncryption)
            {
                val = Store.Unprotect(val);
            }

            emitter.Emit(new Scalar(null, null, key, ScalarStyle.Plain, true, false));

            emitter.Emit(new Scalar(null, null, val, ScalarStyle.DoubleQuoted, true, false));
        }

        emitter.Emit(new MappingEnd());
    }
}
