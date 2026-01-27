using System.Collections.ObjectModel;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using CommunityToolkit.Mvvm.ComponentModel;
using HttpClientFactory = SnapX.Core.Utils.Miscellaneous.HttpClientFactory;

namespace SnapX.Avalonia.ViewModels;

public record GithubContentItem
{
    [JsonPropertyName("name")] public string Name { get; init; } = string.Empty;

    [JsonPropertyName("path")] public string Path { get; init; } = string.Empty;

    [JsonPropertyName("sha")] public string Sha { get; init; } = string.Empty;

    [JsonPropertyName("size")] public int Size { get; init; }

    [JsonPropertyName("url")] public string Url { get; init; } = string.Empty;

    [JsonPropertyName("html_url")] public string HtmlUrl { get; init; } = string.Empty;

    [JsonPropertyName("git_url")] public string GitUrl { get; init; } = string.Empty;

    [JsonPropertyName("download_url")] public string? DownloadUrl { get; init; }

    [JsonPropertyName("type")] public string Type { get; init; } = string.Empty; // "file" or "dir"

    [JsonIgnore] public bool IsFile => Type == "file";

    [JsonIgnore] public bool IsSxcu => Name.EndsWith(".sxcu", StringComparison.OrdinalIgnoreCase);
}

[JsonSerializable(typeof(List<GithubContentItem>))]
[JsonSerializable(typeof(GithubContentItem))]
internal partial class GithubJsonContext : JsonSerializerContext
{
}

public partial class CustomUploaderCatalogVM : ViewModelBase
{
    [ObservableProperty] private ObservableCollection<UploaderInfo> _availableUploaders = new();

    [ObservableProperty] private bool? _isAllSelected = false;

    partial void OnIsAllSelectedChanged(bool? value)
    {
        if (value == null)
            return;
        RefreshSelectionState();
        foreach (var item in AvailableUploaders)
            item.IsSelected = value.Value;
    }

    // Logic to update the CheckBox state when individual items are clicked
    public void RefreshSelectionState()
    {
        var selectedCount = AvailableUploaders.Count(x => x.IsSelected);

        IsAllSelected =
            selectedCount == 0 ? false
            : selectedCount == AvailableUploaders.Count ? true
            : null;
    }

    public static string GetName(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            return string.Empty;

        var name = fileName.EndsWith(".sxcu", StringComparison.OrdinalIgnoreCase)
            ? fileName[..^5]
            : fileName;
        return name;
    }

    public async Task LoadCatalogAsync()
    {
        var client = HttpClientFactory.Get();

        var response = await client.GetFromJsonAsync(
            "https://api.github.com/repos/SnapXL/CustomUploaders/contents/",
            GithubJsonContext.Default.ListGithubContentItem
        );

        if (response != null)
        {
            AvailableUploaders.Clear();
            foreach (var file in response.Where(f => f.IsSxcu))
            {
                AvailableUploaders.Add(
                    new UploaderInfo
                    {
                        Name = GetName(file.Name),
                        DownloadUrl = file.DownloadUrl!
                    }
                );
            }
        }
    }

    public void UpdateSelection(List<UploaderInfo> selectedItems)
    {
        var selectedSet = selectedItems.ToHashSet();

        foreach (var item in AvailableUploaders)
        {
            // This updates the property on the model inside the ObservableCollection
            item.IsSelected = selectedSet.Contains(item);
        }

        RefreshSelectionState();
    }
}

public partial class UploaderInfo : ObservableObject
{
    [JsonIgnore][ObservableProperty] private bool _isSelected;

    public string Name { get; init; }
    public string DownloadUrl { get; init; }
}
