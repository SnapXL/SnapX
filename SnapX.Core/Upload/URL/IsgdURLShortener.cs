// SPDX-License-Identifier: GPL-3.0-or-later


using SnapX.Core.Upload.BaseServices;
using SnapX.Core.Upload.BaseUploaders;
using SnapX.Core.Upload.Utils;

namespace SnapX.Core.Upload.URL;

public class IsgdURLShortenerService : URLShortenerService
{
    public override UrlShortenerType EnumValue { get; } = UrlShortenerType.ISGD;

    public override bool CheckConfig(UploadersConfig config) => true;

    public override URLShortener CreateShortener(UploadersConfig config, TaskReferenceHelper taskInfo)
    {
        return new IsgdURLShortener();
    }
}

public class IsgdURLShortener : URLShortener
{
    protected virtual string? APIURL => "https://is.gd/create.php";

    public override UploadResult ShortenURL(string? url)
    {
        var result = new UploadResult { URL = url };

        if (string.IsNullOrEmpty(url)) return result;

        var arguments = new Dictionary<string, string?>
        {
            { "format", "simple" },
            { "url", url }
        };

        result.Response = SendRequest(HttpMethod.Get, APIURL, arguments);

        if (!result.Response.StartsWith("Error:", StringComparison.OrdinalIgnoreCase))
        {
            result.ShortenedURL = result.Response;
        }

        return result;
    }
}

