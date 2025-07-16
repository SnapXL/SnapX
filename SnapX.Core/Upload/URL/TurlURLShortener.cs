// SPDX-License-Identifier: GPL-3.0-or-later


using SnapX.Core.Upload.BaseServices;
using SnapX.Core.Upload.BaseUploaders;
using SnapX.Core.Upload.Utils;

namespace SnapX.Core.Upload.URL;

public class TurlURLShortenerService : URLShortenerService
{
    public override UrlShortenerType EnumValue => UrlShortenerType.TURL;

    public override bool CheckConfig(UploadersConfig config) => true;

    public override URLShortener CreateShortener(UploadersConfig config, TaskReferenceHelper taskInfo)
    {
        return new TurlURLShortener();
    }
}

public sealed class TurlURLShortener : URLShortener
{
    public override UploadResult ShortenURL(string? url)
    {
        var result = new UploadResult { URL = url };

        if (!string.IsNullOrEmpty(url))
        {
            var arguments = new Dictionary<string, string?> { { "url", url } };

            result.Response = SendRequest(HttpMethod.Get, "https://turl.ca/api.php", arguments);

            if (!string.IsNullOrEmpty(result.Response))
            {
                if (result.Response.StartsWith("SUCCESS:"))
                {
                    result.ShortenedURL = string.Concat("https://turl.ca/", result.Response.AsSpan(8));
                }
                else if (result.Response.StartsWith("ERROR:"))
                {
                    Errors.Add(result.Response.Substring(6));
                }
            }
        }

        return result;
    }
}

