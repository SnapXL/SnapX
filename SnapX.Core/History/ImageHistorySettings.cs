using SixLabors.ImageSharp;

namespace SnapX.Core.History;

public class ImageHistorySettings
{
    public bool RememberWindowState { get; set; } = true;
    public WindowState WindowState { get; set; } = new();
    public Size ThumbnailSize { get; set; } = new(150, 150);
    public int MaxItemCount { get; set; } = 250;
    public bool FilterMissingFiles { get; set; } = false;
    public bool ImageOnly { get; set; } = true;
    public bool RememberSearchText { get; set; } = false;
    public string SearchText { get; set; } = "";
    public bool Favorites { get; set; } = false;
}
