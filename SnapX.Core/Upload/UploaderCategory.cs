using System.ComponentModel;

namespace SnapX.Core.Upload;

/// <summary>
/// Categorization for built-in uploaders to allow filtered navigation
/// within the CoreUploaderVM.
/// </summary>
public enum UploaderCategory
{
    [Description("Image uploaders")]
    ImageUploaders,
    [Description("Text uploaders")]
    TextUploaders,
    [Description("File uploaders")]
    FileUploaders,
    [Description("URL shorteners")]
    URLShorteners,
    [Description("URL sharing services")]
    URLSharing
}
