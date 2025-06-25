
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
public class HistoryItem
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
        public string? Text { get; set; }
        public string? WindowTitle { get; set; }
        public string? ProcessName { get; set; }
    }
    public List<Tag>? Tags { get; set; } = [];
    public override string ToString()
    {
        string text = "";

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
    public string TrayMenuText
    {
        get
        {
            var text = ToString().Truncate(50, "...", false);

            return $"[{DateTime:HH:mm:ss}] {text}";
        }
    }
    public string? BestImageSource =>
        !string.IsNullOrWhiteSpace(FilePath) && File.Exists(FilePath)
            ? FilePath
            : URL ?? ThumbnailURL;

    public override bool Equals(object? obj)
    {
        if (obj is not HistoryItem other)
        {
            return false;
        }

        return Id == other.Id &&
               FileName == other.FileName &&
               FilePath == other.FilePath &&
               DateTime == other.DateTime &&
               Type == other.Type &&
               Hidden == other.Hidden &&
               Host == other.Host &&
               URL == other.URL &&
               ThumbnailURL == other.ThumbnailURL &&
               DeletionURL == other.DeletionURL &&
               ShortenedURL == other.ShortenedURL &&
               // Deep equality for Tags might be needed if you care about changes within the list
               // This gets complex. Often, for lists, you might compare counts and then item-by-item,
               // or just rely on a "LastModified" timestamp on the parent object.
               (Tags?.SequenceEqual(other.Tags ?? Enumerable.Empty<Tag>()) ?? (other.Tags == null));
    }
    public override int GetHashCode()
    {
        var hash = new HashCode();

        hash.Add(Id);
        hash.Add(FileName);
        hash.Add(FilePath);
        hash.Add(DateTime);
        hash.Add(Type);
        hash.Add(Hidden);
        hash.Add(Host);
        hash.Add(URL);
        hash.Add(ThumbnailURL);
        hash.Add(DeletionURL);
        hash.Add(ShortenedURL);

        if (Tags != null)
        {
            foreach (var tag in Tags)
            {
                hash.Add(tag);
            }
        }

        return hash.ToHashCode();
    }
}

