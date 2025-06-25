
// SPDX-License-Identifier: GPL-3.0-or-later


using SnapX.Core.Upload.BaseServices;
using SnapX.Core.Upload.BaseUploaders;
using SnapX.Core.Upload.Utils;
using SnapX.Core.Utils;

namespace SnapX.Core.Upload.URL;

public class PolrURLShortenerService : URLShortenerService
{
    public override UrlShortenerType EnumValue => UrlShortenerType.Polr;

    public override bool CheckConfig(UploadersConfig config)
    {
        return !string.IsNullOrEmpty(config.PolrAPIHostname) && !string.IsNullOrEmpty(config.PolrAPIKey);
    }

    public override URLShortener CreateShortener(UploadersConfig config, TaskReferenceHelper taskInfo)
    {
        return new PolrURLShortener
        {
            Host = config.PolrAPIHostname,
            Key = config.PolrAPIKey,
            IsSecret = config.PolrIsSecret,
            UseAPIv1 = config.PolrUseAPIv1
        };
    }
}

public sealed class PolrURLShortener : URLShortener
{
    public string? Host { get; set; }
    public string? Key { get; set; }
    public bool IsSecret { get; set; }
    public bool UseAPIv1 { get; set; }

    public override UploadResult ShortenURL(string? url)
    {
        var result = new UploadResult { URL = url };

        Host = URLHelpers.FixPrefix(Host);

        var args = new Dictionary<string, string?>
        {
            { "url", url }
        };

        if (!string.IsNullOrEmpty(Key))
        {
            if (UseAPIv1)
            {
                args.Add("apikey", Key);
                args.Add("action", "shorten");
            }
            else
            {
                args.Add("key", Key);
                if (IsSecret)
                {
                    args.Add("is_secret", "true");
                }
            }
        }

        var response = SendRequest(HttpMethod.Get, Host, args);

        if (!string.IsNullOrEmpty(response))
        {
            result.ShortenedURL = response;
        }

        return result;
    }
}

