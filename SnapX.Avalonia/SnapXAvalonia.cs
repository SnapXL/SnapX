using Avalonia.Media.Imaging;
using SixLabors.ImageSharp;

namespace SnapX.Avalonia;

public class SnapXAvalonia : Core.SnapX
{

    public Bitmap ConvertImageSharpImgToAvalonia(Image image)
    {
        using var memoryStream = new MemoryStream();
        image.SaveAsPng(memoryStream);
        memoryStream.Position = 0;
        return new Bitmap(memoryStream);
    }
}
