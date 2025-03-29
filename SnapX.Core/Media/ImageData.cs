using SnapX.Core.Utils.Extensions;

namespace SnapX.Core.Media;

public class ImageData : IDisposable
{
    public Stream ImageStream { get; set; }
    public EImageFormat ImageFormat { get; set; }

    public void Write(string filePath)
    {
        ImageStream.WriteToFile(filePath);
    }
    public void Dispose()
    {
        DebugHelper.Logger?.Debug($"ImageData.Dispose: {ImageFormat}");
        ImageStream?.Dispose();
    }

}
