using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;

namespace SnapX.Avalonia.Views.Controls;

public class SmartImage : Image
{
    protected override Size ArrangeOverride(Size finalSize)
    {
        if (Source is not Bitmap bmp) return base.ArrangeOverride(finalSize);
        var imgSize = new Size(bmp.PixelSize.Width, bmp.PixelSize.Height);
        // If the image is smaller than its container, don't stretch
        Stretch = (imgSize.Width <= finalSize.Width && imgSize.Height <= finalSize.Height)
            ? Stretch.None
            : Stretch.Uniform;

        return base.ArrangeOverride(finalSize);
    }
}
