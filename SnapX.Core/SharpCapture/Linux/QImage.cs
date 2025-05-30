using System.Numerics;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
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
        var imageData = await File.ReadAllBytesAsync(imageFile);

        // Q: "But isn't the format called `Argb`? Why are you loading the data as `Bgra`?"
        // A: Yes, but for some reason the format provided by KWin isn't accurate to the format of the pixel data.
        // Q: "Are you sure you didn't just mess up the QImageFormat enum?"
        // A: The values of the enum match the documentation here: https://doc.qt.io/qt-6/qimage.html#Format-enum
        var image = Image.LoadPixelData<Bgra32>(imageData, (int)width, (int)height);
        image.Mutate(c => c.ProcessPixelRowsAsVector4(row =>
        {
            for (int col = 0; col < row.Length; col++)
            {
                var pixel = row[col];

                var r = (byte)(pixel[2] * 255);
                var g = (byte)(pixel[1] * 255);
                var b = (byte)(pixel[0] * 255);
                var a = (byte)(pixel[3] * 255);

                if (a == 0)
                {
                    r = 0;
                    g = 0;
                    b = 0;

                    row[col] = new Vector4(b, g, r, a);
                }
                else
                {
                    var aFloat = a / 255f;
                    var rFloat = r / 255f;
                    var gFloat = g / 255f;
                    var bFloat = b / 255f;

                    row[col] = new Vector4(bFloat / aFloat, gFloat / aFloat, rFloat / aFloat, aFloat);
                }
            }
        }));

        return image;
    }
}
