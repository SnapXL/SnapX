using System.Collections.ObjectModel;
using System.Text.Json;
using System.Text.Json.Serialization;
using SnapX.Core.Upload.Custom;

namespace SnapX.Core.Utils.Converters;

public class HeaderCollectionConverter : JsonConverter<ObservableCollection<HeaderItem>>
{
    public override ObservableCollection<HeaderItem>? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var dictionary = JsonSerializer.Deserialize<Dictionary<string, string>>(ref reader, options);
        if (dictionary == null) return [];

        return new ObservableCollection<HeaderItem>(
            dictionary.Select(kvp => new HeaderItem { Key = kvp.Key, Value = kvp.Value })
        );
    }

    public override void Write(Utf8JsonWriter writer, ObservableCollection<HeaderItem> value, JsonSerializerOptions options)
    {
        var dict = value
            .Where(x => !string.IsNullOrWhiteSpace(x.Key))
            .GroupBy(x => x.Key)
            .ToDictionary(g => g.Key, g => g.First().Value);

        JsonSerializer.Serialize(writer, dict, options);
    }
}
