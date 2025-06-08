using Avalonia.Markup.Xaml;

namespace SnapX.Avalonia.Extensions;

public class CanUploadExtension : MarkupExtension
{
    public override object ProvideValue(IServiceProvider serviceProvider)
    {
        return Core.SnapX.CanUpload();
    }
}
