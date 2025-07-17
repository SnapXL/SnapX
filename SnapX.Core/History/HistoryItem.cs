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

        public virtual bool Equals(Tag? other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            return Id == other.Id &&
                   Name == other.Name &&
                   Value == other.Value;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Id, Name, Value);
        }
    }
    public List<Tag>? Tags = [];
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
    public virtual bool Equals(HistoryItem? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;

        return FileName == other.FileName &&
               FilePath == other.FilePath &&
               DateTime == other.DateTime &&
               Type == other.Type &&
               Id == other.Id &&
               Hidden == other.Hidden &&
               Host == other.Host &&
               URL == other.URL &&
               ThumbnailURL == other.ThumbnailURL &&
               DeletionURL == other.DeletionURL &&
               ShortenedURL == other.ShortenedURL &&
               TagsEqual(Tags, other.Tags);
    }

    private static bool TagsEqual(List<Tag>? a, List<Tag>? b)
    {
        if (a is null && b is null) return true;
        if (a is null || b is null) return false;
        if (a.Count != b.Count) return false;

        var orderedA = a.OrderBy(t => t.Id).ThenBy(t => t.Name).ThenBy(t => t.Value);
        var orderedB = b.OrderBy(t => t.Id).ThenBy(t => t.Name).ThenBy(t => t.Value);
        return orderedA.SequenceEqual(orderedB);
    }

    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(FileName);
        hash.Add(FilePath);
        hash.Add(DateTime);
        hash.Add(Type);
        hash.Add(Id);
        hash.Add(Hidden);
        hash.Add(Host);
        hash.Add(URL);
        hash.Add(ThumbnailURL);
        hash.Add(DeletionURL);
        hash.Add(ShortenedURL);

        if (Tags != null)
        {
            foreach (var tag in Tags.OrderBy(t => t.Id).ThenBy(t => t.Name).ThenBy(t => t.Value))
            {
                hash.Add(tag);
            }
        }

        return hash.ToHashCode();
    }
}

