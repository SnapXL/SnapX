// SPDX-License-Identifier: GPL-3.0-or-later


using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization;
using SnapX.Core.Upload.BaseServices;
using SnapX.Core.Upload.BaseUploaders;
using SnapX.Core.Upload.Utils;
using SnapX.Core.Utils;

namespace SnapX.Core.Upload.URL;

public class FirebaseDynamicLinksURLShortenerService : URLShortenerService
{
    public override UrlShortenerType EnumValue => UrlShortenerType.FirebaseDynamicLinks;

    public override bool CheckConfig(UploadersConfig config)
    {
        return !string.IsNullOrEmpty(config.FirebaseWebAPIKey) && !string.IsNullOrEmpty(config.FirebaseDynamicLinkDomain);
    }

    public override URLShortener CreateShortener(UploadersConfig config, TaskReferenceHelper taskInfo)
    {
        return new FirebaseDynamicLinksURLShortener
        {
            WebAPIKey = config.FirebaseWebAPIKey,
            DynamicLinkDomain = config.FirebaseDynamicLinkDomain,
            IsShort = config.FirebaseIsShort
        };
    }
}

public class FirebaseRequest
{
    public DynamicLinkInfo dynamicLinkInfo { get; set; }
    public FirebaseSuffix suffix { get; set; }
}

public class DynamicLinkInfo
{
    public string? dynamicLinkDomain { get; set; }
    public string? link { get; set; }
}

public class FirebaseSuffix
{
    public string option { get; set; }
}
[JsonSerializable(typeof(FirebaseResponse))]
internal partial class FirebaseContext : JsonSerializerContext;
public class FirebaseResponse
{
    public string? shortLink { get; set; }
    public string previewLink { get; set; }
}

public sealed class FirebaseDynamicLinksURLShortener : URLShortener
{
    public string WebAPIKey { get; set; }
    public string? DynamicLinkDomain { get; set; }
    public bool IsShort { get; set; }

    [RequiresDynamicCode("Uploader")]
    [RequiresUnreferencedCode("Uploader")]
    public override UploadResult ShortenURL(string? url)
    {
        var result = new UploadResult { URL = url };

        var requestOptions = new FirebaseRequest
        {
            dynamicLinkInfo = new DynamicLinkInfo
            {
                dynamicLinkDomain = URLHelpers.RemovePrefixes(DynamicLinkDomain),
                link = url
            }
        };

        if (IsShort)
        {
            requestOptions.suffix = new FirebaseSuffix
            {
                option = "SHORT"
            };
        }

        var args = new Dictionary<string, string?>
        {
            { "key", WebAPIKey },
            { "fields", "shortLink" }
        };

        var serializedRequestOptions = JsonSerializer.Serialize(requestOptions);
        result.Response = SendRequest(HttpMethod.Post, "https://firebasedynamiclinks.googleapis.com/v1/shortLinks", serializedRequestOptions, RequestHelpers.ContentTypeJSON, args);
        var options = new JsonSerializerOptions()
        {
            TypeInfoResolver = FirebaseContext.Default
        };
        var firebaseResponse = JsonSerializer.Deserialize<FirebaseResponse>(result.Response, options);

        if (firebaseResponse != null)
        {
            result.ShortenedURL = firebaseResponse.shortLink;
        }

        return result;
    }
}

