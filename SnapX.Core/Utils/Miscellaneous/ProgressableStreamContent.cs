using System.Buffers;
using System.Net;

namespace SnapX.Core.Utils.Miscellaneous;

public class ProgressableStreamContent : HttpContent
{
    private readonly HttpContent _innerContent;
    private readonly Action<long> _onProgress;

    public ProgressableStreamContent(HttpContent innerContent, Action<long> onProgress)
    {
        _innerContent = innerContent;
        _onProgress = onProgress;

        // Copy headers (like Content-Type) from original content
        foreach (var header in innerContent.Headers)
            Headers.TryAddWithoutValidation(header.Key, header.Value);
    }

    protected override async Task SerializeToStreamAsync(Stream stream, TransportContext? context)
    {
        await using var sourceStream = await _innerContent.ReadAsStreamAsync().ConfigureAwait(false);

        var buffer = ArrayPool<byte>.Shared.Rent(32768);

        try
        {
            long totalRead = 0;
            int read;

            var memoryBuffer = buffer.AsMemory();

            while ((read = await sourceStream.ReadAsync(memoryBuffer).ConfigureAwait(false)) != 0)
            {
                await stream.WriteAsync(memoryBuffer[..read]).ConfigureAwait(false);

                totalRead += read;
                //
                // int jitter = System.Random.Shared.Next(100, 300);
                // if (System.Random.Shared.Next(0, 100) > 95) jitter = System.Random.Shared.Next(500, 2500);
                //
                // // 4. Optimization: Only delay if we aren't at the very end
                // // and only report progress to avoid UI saturation
                // await Task.Delay(jitter).ConfigureAwait(false);

                _onProgress(totalRead);
            }
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    protected override bool TryComputeLength(out long length)
    {
        length = _innerContent.Headers.ContentLength ?? -1;
        return length != -1;
    }
}
