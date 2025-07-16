// SPDX-License-Identifier: GPL-3.0-or-later


using System.Collections.Specialized;
using System.Net;
using System.Text;
using SnapX.Core.Utils.Cryptographic;

namespace SnapX.Core.Upload.Utils;

internal static class RequestHelpers
{
    public const string ContentTypeMultipartFormData = "multipart/form-data";
    public const string ContentTypeJSON = "application/json";
    public const string ContentTypeXML = "application/xml";
    public const string ContentTypeURLEncoded = "application/x-www-form-urlencoded";
    public const string ContentTypeOctetStream = "application/octet-stream";

    public static async Task<HttpRequestMessage> CreateHttpRequest(
          HttpMethod method,
          string? url,
          NameValueCollection headers = null,
          CookieCollection cookies = null,
          string contentType = null,
          long contentLength = 0,
          HttpContent content = null)
    {
        var requestMessage = new HttpRequestMessage(method, url);

        if (headers != null)
        {
            if (headers["Accept"] != null)
            {
                requestMessage.Headers.Accept.ParseAdd(headers["Accept"]);
                headers.Remove("Accept");
            }

            if (headers["Content-Type"] != null)
            {
                contentType = headers["Content-Type"];
                headers.Remove("Content-Type");
            }

            if (headers["Content-Length"] != null && long.TryParse(headers["Content-Length"], out var parsedContentLength))
            {
                contentLength = parsedContentLength;
                headers.Remove("Content-Length");
            }

            if (headers["Cookie"] != null)
            {
                cookies ??= [];
                var cookieHeader = headers["Cookie"];
                foreach (var cookie in cookieHeader.Split(["; "], StringSplitOptions.RemoveEmptyEntries))
                {
                    var cookieValues = cookie.Split(['='], StringSplitOptions.RemoveEmptyEntries);
                    if (cookieValues.Length == 2)
                    {
                        cookies.Add(new Cookie(cookieValues[0], cookieValues[1], "/", new Uri(url).Host));
                    }
                }
                headers.Remove("Cookie");
            }

            foreach (var key in headers.AllKeys)
            {
                requestMessage.Headers.TryAddWithoutValidation(key, headers[key]);
            }

            if (cookies != null)
            {
                requestMessage.Headers.Add("Cookie", string.Join("; ", cookies.Select(c => $"{c.Name}={c.Value}")));
            }

            requestMessage.Headers.UserAgent.ParseAdd(SnapXResources.UserAgent);

            if (headers["Referer"] != null)
            {
                requestMessage.Headers.Referrer = new Uri(headers["Referer"]!);
                headers.Remove("Referer");
            }
        }

        if (!string.IsNullOrEmpty(contentType))
        {
            if (content == null) content = new StringContent(string.Empty, Encoding.UTF8, contentType);
            else content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(contentType);
        }

        if (content != null)
        {
            requestMessage.Content = content;
            if (contentLength > 0)
            {
                requestMessage.Content.Headers.ContentLength = contentLength;
            }
        }
        return requestMessage;
    }



    public static string CreateBoundary()
    {
        return new string('-', 20) + DateTime.Now.Ticks.ToString("x");
    }

    public static byte[] MakeInputContent(string boundary, string name, string value)
    {
        var content = $"--{boundary}\r\nContent-Disposition: form-data; name=\"{name}\"\r\n\r\n{value}\r\n";
        return Encoding.UTF8.GetBytes(content);
    }

    public static byte[] MakeInputContent(string boundary, Dictionary<string, string?> contents, bool isFinal = true)
    {
        if (string.IsNullOrEmpty(boundary))
            boundary = CreateBoundary();

        if (contents == null || contents.Count == 0)
            return Array.Empty<byte>();

        using var stream = new MemoryStream();
        foreach (var content in contents.Where(c => !string.IsNullOrEmpty(c.Key)))
        {
            var bytes = MakeInputContent(boundary, content.Key, content.Value);
            stream.Write(bytes, 0, bytes.Length);
        }

        if (isFinal)
        {
            var bytes = Encoding.UTF8.GetBytes($"--{boundary}--\r\n");
            stream.Write(bytes, 0, bytes.Length);
        }

        return stream.ToArray();
    }


    public static byte[] MakeFileInputContentOpen(string boundary, string fileFormName, string? fileName)
    {
        var mimeType = MimeTypes.GetMimeType(fileName);
        var content = $"--{boundary}\r\nContent-Disposition: form-data; name=\"{fileFormName}\"; filename=\"{fileName}\"\r\nContent-Type: {mimeType}\r\n\r\n";
        return Encoding.UTF8.GetBytes(content);
    }

    public static byte[] MakeRelatedFileInputContentOpen(string boundary, string contentType, string relatedData, string? fileName)
    {
        var mimeType = MimeTypes.GetMimeType(fileName);
        var content = $"--{boundary}\r\nContent-Type: {contentType}\r\n\r\n{relatedData}\r\n\r\n";
        content += $"--{boundary}\r\nContent-Type: {mimeType}\r\n\r\n";
        return Encoding.UTF8.GetBytes(content);
    }

    public static byte[] MakeFileInputContentClose(string boundary)
    {
        return Encoding.UTF8.GetBytes($"\r\n--{boundary}--\r\n");
    }

    public static string ResponseToString(HttpResponseMessage response)
    {
        if (response?.Content == null)
            return null;

        return response.Content.ReadAsStringAsync().ConfigureAwait(false).GetAwaiter().GetResult();
    }



    public static NameValueCollection CreateAuthenticationHeader(string username, string password)
    {
        var authorization = TranslatorHelper.TextToBase64($"{username}:{password}");
        return new NameValueCollection
        {
            ["Authorization"] = $"Basic {authorization}"
        };
    }
}

