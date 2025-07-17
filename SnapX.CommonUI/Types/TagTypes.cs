// var myDeserializedClass = JsonSerializer.Deserialize<List<Tag>>(myJsonResponse);
#pragma warning disable

using System.Text.Json.Serialization;

namespace SnapX.CommonUI.Types;
public record Tag(
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("zipball_url")] string ZipballUrl,
    [property: JsonPropertyName("tarball_url")] string TarballUrl,
    [property: JsonPropertyName("commit")] Commit? Commit,
    [property: JsonPropertyName("node_id")] string NodeId
);
