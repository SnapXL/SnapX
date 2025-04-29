namespace SnapX.Core.Media;

public class ImageData : IDisposable
{
    public Stream ImageStream { get; set; }
    public EImageFormat ImageFormat { get; set; }

    public void Write(string filePath)
    {
        const int maxRetries = 5;
        const int retryDelayMilliseconds = 1000;
        int retryCount = 0;
        bool fileSaved = false;

        while (retryCount < maxRetries && !fileSaved)
        {
            try
            {
                if (ImageStream.CanSeek)
                {
                    ImageStream.Position = 0;
                }
                using var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write);
                ImageStream.CopyTo(fileStream);
                fileSaved = true;
            }
            catch (IOException ex)
            {
                retryCount++;

                DebugHelper.WriteLine(
                    $"Attempt {retryCount} failed. IOException: {ex.Message}. Retrying in {retryDelayMilliseconds / 1000} second(s)...");

                if (retryCount < maxRetries)
                {
                    Thread.Sleep(retryDelayMilliseconds);
                }
                else
                {
                    DebugHelper.WriteLine($"Failed to save the file after {maxRetries} retries. Throwing :(");
                    throw;
                }
            }
        }
    }
    public void Dispose()
    {
        DebugHelper.Logger?.Debug($"ImageData.Dispose: {ImageFormat}");
        ImageStream?.Dispose();
    }

}
