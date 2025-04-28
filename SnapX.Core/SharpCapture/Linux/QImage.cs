using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SnapX.Core.SharpCapture.Linux.DBus;

namespace SnapX.Core.SharpCapture.Linux;

internal static class QImage
{
    public static Task<Image> LoadAsync(string imageFile, CaptureWorkspaceResult workspaceResult)
    {
        var format = workspaceResult.Format;

        return format switch
        {
            // Seems like the only format returned by KWin at the moment.
            QImageFormat.ARGB32Premultiplied => FromArgb32Premultiplied(imageFile, workspaceResult.Width, workspaceResult.Height),
            // TODO add all possible formats that can be returned by KWin
            _ => throw new NotImplementedException()
        };
    }

    private static async Task<Image> FromArgb32Premultiplied(string imageFile, uint width, uint height)
    {
        var imageStream = File.OpenRead(imageFile);

        var pixelCount = width * height;

        var convertedPixels = new Argb32[pixelCount];

        // Pixel formatted as [B, G, R, A]
        // Q: "But isn't the format called `Argb`? Why is it in a different order?"
        // A: Yes, but for some reason the format provided by KWin isn't correct.
        // Q: "Are you sure you didn't just mess up the QImageFormat enum?"
        // A: The values of the enum match the documentation here: https://doc.qt.io/qt-6/qimage.html#Format-enum
        var pixel = new byte[4];

        for (int i = 0; i < pixelCount; i++)
        {
            if (await imageStream.ReadAsync(pixel) != 4)
                throw new Exception("Unexpected EOF while reading QImage pixel data!");

            var a = pixel[3];
            var b = pixel[0];
            var g = pixel[1];
            var r = pixel[2];

            if (a == 0)
            {
                r = 0;
                g = 0;
                b = 0;
            }
            else
            {
                var aFloat = a / 255f;
                var rFloat = r / 255f;
                var gFloat = g / 255f;
                var bFloat = b / 255f;

                r = (byte)(rFloat / aFloat * 255f);
                g = (byte)(gFloat / aFloat * 255f);
                b = (byte)(bFloat / aFloat * 255f);
            }

            convertedPixels[i] = new Argb32(r, g, b, a);
        }

        return Image.LoadPixelData<Argb32>(convertedPixels, (int)width, (int)height);
    }
}
