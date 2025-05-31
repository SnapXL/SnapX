using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace SnapX.Core.Utils.Native;

public class Clipboard
{
    public static bool ContainsImage() => false;

    public static bool ContainsText() => false;
    public static bool ContainsFile() => false;
    public static bool ContainsData() => false;
    public static bool ContainsFileDropList() => false;
    public static List<string> GetFileDropList() => [];
    public static Image<Rgba64> GetImage() => new(1, 1);
    public static string GetText() => string.Empty;
    public static void CopyText(string text) => Methods.CopyText(text);
    public static void CopyImage(string imagePath) => CopyImage(Image.Load(imagePath), Path.GetFileName(imagePath));
    public static void CopyImage(Image image, string fileName = "")
    {
        var format = image.Metadata.DecodedImageFormat ?? null;
        if (string.IsNullOrEmpty(fileName)) fileName = $"image{Helpers.GetImageExtension(image)}";
        DebugHelper.WriteLine($"Clipboard.CopyImage: {image.Width}x{image.Height}: {fileName}");
        Methods.CopyImage(image, fileName);

    }

    public static void CopyFile(string path) => DebugHelper.WriteLine($"Clipboard.CopyFile: {path}");
    public static void CopyTextFromFile(string path) => DebugHelper.WriteLine($"Clipboard.CopyTextFromFile: {path}");
    public static void PasteText(string text) => DebugHelper.WriteLine($"Clipboard.PasteText: {text}");
    public static void CopyImageFromFile(string path) => DebugHelper.WriteLine($"Clipboard.CopyImageFromFile: {path}");
    public static void Clear() => DebugHelper.WriteLine("Use your imagination to clear the clipboard.");
}
