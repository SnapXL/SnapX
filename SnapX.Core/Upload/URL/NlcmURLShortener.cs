
// SPDX-License-Identifier: GPL-3.0-or-later


using SnapX.Core.Upload.BaseUploaders;

namespace SnapX.Core.Upload.URL;

public sealed class NlcmURLShortener : URLShortener
{
    public override UploadResult ShortenURL(string? url)
    {
        var result = new UploadResult { URL = url };
        if (string.IsNullOrEmpty(url)) return result;

        var arguments = new Dictionary<string, string?> { { "url", url } };

        result.Response = result.ShortenedURL = SendRequest(HttpMethod.Get, "https://nl.cm/api/", arguments);

        return result;
    }
}

