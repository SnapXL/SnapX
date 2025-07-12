
// SPDX-License-Identifier: GPL-3.0-or-later


using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using SnapX.Core.Utils.Extensions;

namespace SnapX.Core.History;

[JsonSerializable(typeof(HistoryItem))]
[JsonSerializable(typeof(List<HistoryItem>))]

internal partial class HistoryContext : JsonSerializerContext;

[Table("HistoryItems")]
public record HistoryItem
{
    [Key]
    public int Id { get; set; }
    public string? FileName { get; set; }
    public string? FilePath { get; set; }
    public DateTime DateTime { get; set; }
    public string? Type { get; set; }
    public bool Hidden { get; set; }
    public string? Host { get; set; }
    public string? URL { get; set; }
    public string? ThumbnailURL { get; set; }
    public string? DeletionURL { get; set; }
    public string? ShortenedURL { get; set; }
    [Table("Tags")]
    public record Tag
    {
        public int Id { get; set; }
        public required string Name { get; set; }

        public string Value { get; set; } = "";
    }
    public List<Tag>? Tags { get; set; } = [];
    public override string ToString()
    {
        var text = "";

        if (!string.IsNullOrEmpty(ShortenedURL))
        {
            text = ShortenedURL;
        }
        else if (!string.IsNullOrEmpty(URL))
        {
            text = URL;
        }
        else if (!string.IsNullOrEmpty(FilePath))
        {
            text = FilePath;
        }

        return text;
    }
    [JsonIgnore]
    public string TrayMenuText
    {
        get
        {
            var text = ToString().Truncate(50, "...", false);

            return $"[{DateTime:HH:mm:ss}] {text}";
        }
    }
    [JsonIgnore]
    public string? BestImageSource =>
        !string.IsNullOrWhiteSpace(FilePath) && File.Exists(FilePath)
            ? FilePath
            : URL ?? ThumbnailURL;
}

