
// SPDX-License-Identifier: GPL-3.0-or-later


using System.Collections.Specialized;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization;
using SnapX.Core.Upload.BaseServices;
using SnapX.Core.Upload.BaseUploaders;
using SnapX.Core.Upload.Utils;
using SnapX.Core.Utils;

namespace SnapX.Core.Upload.URL;

public class KuttURLShortenerService : URLShortenerService
{
    public override UrlShortenerType EnumValue => UrlShortenerType.Kutt;
    public override bool CheckConfig(UploadersConfig config)
    {
        return !string.IsNullOrEmpty(config.KuttSettings.APIKey);
    }

    public override URLShortener CreateShortener(UploadersConfig config, TaskReferenceHelper taskInfo)
    {
        return new KuttURLShortener(config.KuttSettings);
    }
}
[JsonSerializable(typeof(KuttURLShortener.KuttShortenLinkResponse))]
internal partial class KuttContext : JsonSerializerContext;
public sealed class KuttURLShortener : URLShortener
{
    public KuttSettings Settings { get; set; }

    public KuttURLShortener(KuttSettings settings)
    {
        Settings = settings;
    }

    [UnconditionalSuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code", Justification = "<Pending>")]
    public override UploadResult ShortenURL(string? url)
    {
        var result = new UploadResult { URL = url };
        result.ShortenedURL = Submit(url);
        return result;
    }

    [RequiresDynamicCode("Uploader")]
    [RequiresUnreferencedCode("Uploader")]
    public string? Submit(string? url)
    {
        if (string.IsNullOrEmpty(Settings.Host))
        {
            Settings.Host = "https://kutt.it";
        }
        else
        {
            Settings.Host = URLHelpers.FixPrefix(Settings.Host);
        }

        var requestURL = URLHelpers.CombineURL(Settings.Host, "/api/v2/links");

        var body = new KuttShortenLinkBody()
        {
            target = url,
            password = Settings.Password,
            customurl = null,
            reuse = Settings.Reuse,
            domain = Settings.Domain
        };

        var json = JsonSerializer.Serialize(body);

        var headers = new NameValueCollection { { "X-API-KEY", Settings.APIKey } };

        var response = SendRequest(HttpMethod.Post, requestURL, json, RequestHelpers.ContentTypeJSON, headers: headers);

        if (string.IsNullOrEmpty(response)) return null;
        var options = new JsonSerializerOptions
        {
            TypeInfoResolver = KuttContext.Default
        };
        var shortenLinkResponse = JsonSerializer.Deserialize<KuttShortenLinkResponse>(response, options);

        return shortenLinkResponse != null ? shortenLinkResponse.link : null;
    }

    private class KuttShortenLinkBody
    {
        /// <summary>Original long URL to be shortened.</summary>
        public string? target { get; set; }

        /// <summary>(optional) Set a password.</summary>
        public string password { get; set; }

        /// <summary>(optional) Set a custom URL.</summary>
        public string customurl { get; set; }

        /// <summary>(optional) If a URL with the specified target exists returns it, otherwise will send a new shortened URL.</summary>
        public bool reuse { get; set; }

        public string domain { get; set; }
    }

    public class KuttShortenLinkResponse
    {
        /// <summary>Unique ID of the URL</summary>
        public string id { get; set; }

        /// <summary>The shortened link</summary>
        public string? link { get; set; }
    }
}

public class KuttSettings
{
    public string? Host { get; set; } = "https://kutt.it";
    public string APIKey { get; set; }
    public string Password { get; set; }
    public bool Reuse { get; set; }
    public string Domain { get; set; }
}
