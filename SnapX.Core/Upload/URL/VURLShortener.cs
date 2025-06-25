
// SPDX-License-Identifier: GPL-3.0-or-later


using SnapX.Core.Upload.BaseServices;
using SnapX.Core.Upload.BaseUploaders;
using SnapX.Core.Upload.Utils;

namespace SnapX.Core.Upload.URL;

public class VURLShortenerService : URLShortenerService
{
    public override UrlShortenerType EnumValue => UrlShortenerType.VURL;

    public override bool CheckConfig(UploadersConfig config) => true;

    public override URLShortener CreateShortener(UploadersConfig config, TaskReferenceHelper taskInfo)
    {
        return new VURLShortener();
    }
}

public sealed class VURLShortener : URLShortener
{
    private const string? API_ENDPOINT = "https://vurl.com/api.php";

    public override UploadResult ShortenURL(string? url)
    {
        var result = new UploadResult { URL = url };

        var args = new Dictionary<string, string?> { { "url", url } };

        var response = SendRequest(HttpMethod.Get, API_ENDPOINT, args);

        if (!string.IsNullOrEmpty(response) && response != "Invalid URL")
        {
            result.ShortenedURL = response;
        }

        return result;
    }
}

