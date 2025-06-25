
// SPDX-License-Identifier: GPL-3.0-or-later


using SnapX.Core.Upload.BaseServices;
using SnapX.Core.Upload.BaseUploaders;
using SnapX.Core.Upload.Utils;

namespace SnapX.Core.Upload.URL;

public class YourlsURLShortenerService : URLShortenerService
{
    public override UrlShortenerType EnumValue => UrlShortenerType.YOURLS;

    public override bool CheckConfig(UploadersConfig config)
    {
        return !string.IsNullOrEmpty(config.YourlsAPIURL) && (!string.IsNullOrEmpty(config.YourlsSignature) ||
            (!string.IsNullOrEmpty(config.YourlsUsername) && !string.IsNullOrEmpty(config.YourlsPassword)));
    }

    public override URLShortener CreateShortener(UploadersConfig config, TaskReferenceHelper taskInfo)
    {
        return new YourlsURLShortener
        {
            APIURL = config.YourlsAPIURL,
            Signature = config.YourlsSignature,
            Username = config.YourlsUsername,
            Password = config.YourlsPassword
        };
    }
}

public sealed class YourlsURLShortener : URLShortener
{
    public string? APIURL { get; set; }
    public string? Signature { get; set; }
    public string? Username { get; set; }
    public string? Password { get; set; }

    public override UploadResult ShortenURL(string? url)
    {
        var result = new UploadResult { URL = url };

        if (string.IsNullOrEmpty(url))
            return result;

        var arguments = new Dictionary<string, string?>
        {
            { "url", url },
            { "action", "shorturl" },
            { "format", "simple" }
        };

        if (!string.IsNullOrEmpty(Signature))
        {
            arguments.Add("signature", Signature);
        }
        else if (!string.IsNullOrEmpty(Username) && !string.IsNullOrEmpty(Password))
        {
            arguments.Add("username", Username);
            arguments.Add("password", Password);
        }
        else
        {
            throw new Exception("Signature or Username/Password is missing.");
        }

        result.Response = SendRequestMultiPart(APIURL, arguments);
        result.ShortenedURL = result.Response;

        return result;
    }
}

