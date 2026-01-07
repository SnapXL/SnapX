using System.Collections.Concurrent;
using System.IO.Hashing;
using System.Text;
using NeoSolve.ImageSharp.AVIF;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.Processing;
using SnapX.Core.Utils.Miscellaneous;
using Xdg.Directories;

namespace SnapX.Core.Media.Services;

public static class ThumbnailService
{
    static ThumbnailService()
    {
        var imageConfig = Configuration.Default;
        imageConfig.ImageFormatsManager.SetEncoder(AVIFFormat.Instance, AVIFEncoder.Instance);
        imageConfig.ImageFormatsManager.SetDecoder(AVIFFormat.Instance, AVIFDecoder.Instance);
        imageConfig.ImageFormatsManager.AddImageFormatDetector(new PatchedAVIFImageFormatDetector());
    }
    private static readonly ConcurrentDictionary<string, string> _pathCache = new();
    private static readonly string CacheFolder = Path.Combine(BaseDirectory.CacheHome, SnapX.AppName, "Thumbnails");
    private static readonly HttpClient _httpClient = HttpClientFactory.Get();
    private static readonly SemaphoreSlim _processingSemaphore = new(3, 3);
    public static async Task<string> GetCompatibleSourceAsync(string? source)
    {
        if (string.IsNullOrEmpty(source)) return string.Empty;
        var result = await Task.Factory.StartNew(async () =>
        {
            await _processingSemaphore.WaitAsync();
            try
            {
                return await GenerateWebpThumbnail(source);
            }
            finally
            {
                _processingSemaphore.Release();
            }
        }, CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default).Unwrap();
        _pathCache.TryAdd(source, result);
        return result;

    }

    private static async Task<string> GenerateWebpThumbnail(string source)
    {
        if (_pathCache.TryGetValue(source, out var cachedPath))
        {
            return cachedPath;
        }
        var isUrl = Uri.TryCreate(source, UriKind.Absolute, out var uri)
                     && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
        Directory.CreateDirectory(CacheFolder);

        var sourceBytes = Encoding.UTF8.GetBytes(source);
        var hashBytes = XxHash64.Hash(sourceBytes);
        var hashName = Convert.ToHexString(hashBytes).Replace("-", "") + ".webp";
        var cachePath = Path.Combine(CacheFolder, hashName);

        if (File.Exists(cachePath)) return cachePath;

        try
        {
            DebugHelper.Logger?.Debug($"ThumbnailService: Generating thumbnail for {(isUrl ? "URL" : "File")}: {source}");

            await using var imageStream = await GetImageStreamAsync(source, isUrl);
            using var image = await Image.LoadAsync(imageStream);
            image.Mutate(x => x.Resize(new ResizeOptions
            {
                Size = new Size(200, 150),
                Mode = ResizeMode.Max
            }));
            // CPU time is precious
            await image.SaveAsWebpAsync(cachePath, new WebpEncoder()
            {
                Quality = 70,
                Method = WebpEncodingMethod.Fastest
            });

            return cachePath;
        }
        catch (Exception ex)
        {
            DebugHelper.Logger?.Debug($"ThumbnailService: Error during conversion: {ex.Message}");
            return source;
        }
    }

    private static async Task<Stream> GetImageStreamAsync(string source, bool isUrl)
    {
        if (!isUrl) return File.OpenRead(source);
        using var request = new HttpRequestMessage(HttpMethod.Get, source);

        request.Headers.Accept.ParseAdd("image/avif,image/webp,image/apng,image/*,*/*;q=0.8");

        request.Headers.ExpectContinue = true;

        var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadAsStreamAsync();

    }
}
