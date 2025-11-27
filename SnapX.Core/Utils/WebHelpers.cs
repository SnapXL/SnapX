
// SPDX-License-Identifier: GPL-3.0-or-later


using System.Net;
using System.Net.Sockets;
using System.Text.RegularExpressions;
using SixLabors.ImageSharp;
using SnapX.Core.Utils.Miscellaneous;

namespace SnapX.Core.Utils;

public static class WebHelpers
{
    public static async Task DownloadFileAsync(string url, string? filePath)
    {
        if (string.IsNullOrEmpty(url) || string.IsNullOrEmpty(filePath))
        {
            return;
        }

        FileHelpers.CreateDirectoryFromFilePath(filePath);

        var client = HttpClientFactory.Get();
        using var responseMessage = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);

        if (!responseMessage.IsSuccessStatusCode)
        {
            DebugHelper.Logger.Error("{url}: {responseMessage.ReasonPhrase}", url, responseMessage);
            return;
        }

        await using var responseStream = await responseMessage.Content.ReadAsStreamAsync();
        await using var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write);

        await responseStream.CopyToAsync(fileStream);
    }

    public static async Task<Image> DataURLToImage(string? url)
    {
        // Ensure the URL is valid and starts with "data:"
        if (url == null || !url.ToString().StartsWith("data:"))
        {
            throw new ArgumentException("Invalid data URL.");
        }

        var dataUrl = url;
        var regex = new Regex(@"^data:image\/(?<type>.*?);base64,(?<data>.+)$");
        var match = regex.Match(dataUrl);

        if (!match.Success)
        {
            throw new ArgumentException("Invalid data URL format.");
        }

        var base64Data = match.Groups["data"].Value;

        byte[] imageBytes = Convert.FromBase64String(base64Data);

        using var ms = new MemoryStream(imageBytes);
        var image = await Image.LoadAsync(ms);
        return image;
    }

    public static async Task<string> DownloadStringAsync(string url)
    {
        if (string.IsNullOrEmpty(url))
        {
            return null;
        }

        var client = HttpClientFactory.Get();
        using var responseMessage = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
        if (!responseMessage.IsSuccessStatusCode)
        {
            DebugHelper.Logger.Error("{url}: {responseMessage.ReasonPhrase}", url, responseMessage);
            return null;
        }

        return await responseMessage.Content.ReadAsStringAsync();
    }



    public static async Task<string?> GetFileNameFromWebServerAsync(string url)
    {
        if (string.IsNullOrEmpty(url)) return null;

        var client = HttpClientFactory.Get();
        using var requestMessage = new HttpRequestMessage(HttpMethod.Head, url);

        using var responseMessage = await client.SendAsync(requestMessage);

        return responseMessage.Content.Headers.ContentDisposition?.FileName;
    }


    public static async Task<Image?> DownloadImageAsync(string? url)
    {
        if (string.IsNullOrEmpty(url)) return null;
        try
        {

            var client = HttpClientFactory.Get();

            using var responseMessage = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);

            if (!responseMessage.IsSuccessStatusCode)
            {
                DebugHelper.Logger.Error("{url}: {responseMessage.ReasonPhrase}", url, responseMessage);
                return null;
            }


            var mediaType = responseMessage.Content.Headers.ContentType?.MediaType;
            if (mediaType == null)
            {
                DebugHelper.Logger.Error("{url}: mediaType is null.", url);
                return null;
            }

            if (!MimeTypesPlus.IsImageMimeType(mediaType))
            {
                DebugHelper.Logger.Error("{url}: mediaType/Mimetype is not a known image type.", url);
                return null;
            }

            var data = await responseMessage.Content.ReadAsByteArrayAsync();

            using var memoryStream = new MemoryStream(data);
            return await Image.LoadAsync(memoryStream);
        }
        catch (Exception ex)
        {
            DebugHelper.Logger.Error("{url}: {message}", url, ex.Message);
            DebugHelper.WriteException(ex);
            return null;
        }
    }

    public static bool IsSuccessStatusCode(HttpStatusCode statusCode)
    {
        var statusCodeNum = (int)statusCode;
        return statusCodeNum >= 200 && statusCodeNum <= 299;
    }

    public static int GetRandomUnusedPort()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);

        try
        {
            listener.Start();
            return ((IPEndPoint)listener.LocalEndpoint).Port;
        }
        finally
        {
            listener.Stop();
        }
    }
}

