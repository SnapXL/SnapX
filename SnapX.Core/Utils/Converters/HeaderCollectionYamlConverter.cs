using System.Collections.ObjectModel;
using SnapX.Core.Upload.Custom;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.Serialization;

namespace SnapX.Core.Utils.Converters;

public class HeaderCollectionYamlConverter : IYamlTypeConverter
{
    public bool Accepts(Type type)
        => type == typeof(ObservableCollection<HeaderItem>);

    public object? ReadYaml(IParser parser, Type type, ObjectDeserializer rootDeserializer)
    {
        // Handle null explicitly
        if (parser.TryConsume<Scalar>(out var scalar) &&
            scalar.Value == null)
        {
            return new ObservableCollection<HeaderItem>();
        }

        var result = rootDeserializer(typeof(object));

        if (result is not Dictionary<object, object> rawDict || rawDict.Count == 0)
            return new ObservableCollection<HeaderItem>();

        var collection = new ObservableCollection<HeaderItem>();

        foreach (var kvp in rawDict)
        {
            var key = kvp.Key?.ToString();
            var value = kvp.Value?.ToString();

            if (!string.IsNullOrWhiteSpace(key))
            {
                collection.Add(new HeaderItem
                {
                    Key = key,
                    Value = value ?? string.Empty
                });
            }
        }

        return collection;
    }


    public void WriteYaml(IEmitter emitter, object? value, Type type, ObjectSerializer serializer)
    {
        var headers = value as ObservableCollection<HeaderItem>;

        if (headers == null)
        {
            serializer(new Dictionary<string, string>());
            return;
        }

        var dict = headers
            .Where(x => !string.IsNullOrWhiteSpace(x.Key))
            .GroupBy(x => x.Key)
            .ToDictionary(g => g.Key, g => g.First().Value);

        serializer(dict);
    }
}
