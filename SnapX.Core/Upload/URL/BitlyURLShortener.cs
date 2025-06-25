
// SPDX-License-Identifier: GPL-3.0-or-later


using System.Collections.Specialized;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Web;
using SnapX.Core.Upload.BaseServices;
using SnapX.Core.Upload.BaseUploaders;
using SnapX.Core.Upload.OAuth;
using SnapX.Core.Upload.Utils;
using SnapX.Core.Utils;
using SnapX.Core.Utils.Miscellaneous;

namespace SnapX.Core.Upload.URL;

public class BitlyURLShortenerService : URLShortenerService
{
    public override UrlShortenerType EnumValue { get; } = UrlShortenerType.BITLY;

    public override bool CheckConfig(UploadersConfig config)
    {
        return OAuth2Info.CheckOAuth(config.BitlyOAuth2Info);
    }

    public override URLShortener CreateShortener(UploadersConfig config, TaskReferenceHelper taskInfo)
    {
        if (config.BitlyOAuth2Info == null)
        {
            config.BitlyOAuth2Info = new OAuth2Info(APIKeys.BitlyClientID, APIKeys.BitlyClientSecret);
        }

        return new BitlyURLShortener(config.BitlyOAuth2Info)
        {
            Domain = config.BitlyDomain
        };
    }
}
[JsonSerializable(typeof(BitlyURLShortener.BitlyShortenResponse))]
internal partial class BitlyContext : JsonSerializerContext;
public sealed class BitlyURLShortener : URLShortener, IOAuth2Basic
{
    private const string URLAPI = "https://api-ssl.bitly.com/";
    private const string? URLAccessToken = URLAPI + "oauth/access_token";
    private const string? URLShorten = URLAPI + "v4/shorten";

    public OAuth2Info AuthInfo { get; private set; }
    public string Domain { get; set; }

    public BitlyURLShortener(OAuth2Info oauth)
    {
        AuthInfo = oauth;
    }

    public string? GetAuthorizationURL()
    {
        var args = new Dictionary<string, string?>
        {
            { "client_id", AuthInfo.Client_ID },
            { "redirect_uri", Links.Callback }
        };

        return URLHelpers.CreateQueryString("https://bitly.com/oauth/authorize", args);
    }

    public bool GetAccessToken(string? code)
    {
        Dictionary<string, string?> args = new Dictionary<string, string?>
        {
            { "client_id", AuthInfo.Client_ID },
            { "client_secret", AuthInfo.Client_Secret },
            { "code", code },
            { "redirect_uri", Links.Callback }
        };

        string? response = SendRequestURLEncoded(HttpMethod.Post, URLAccessToken, args);

        if (!string.IsNullOrEmpty(response))
        {
            string token = HttpUtility.ParseQueryString(response)["access_token"];

            if (!string.IsNullOrEmpty(token))
            {
                AuthInfo.Token = new OAuth2Token { access_token = token };
                return true;
            }
        }

        return false;
    }

    private NameValueCollection GetAuthHeaders()
    {
        NameValueCollection headers = new NameValueCollection
        {
            { "Authorization", "Bearer " + AuthInfo.Token.access_token }
        };
        return headers;
    }

    [RequiresDynamicCode("Uploader")]
    [RequiresUnreferencedCode("Uploader")]
    public override UploadResult ShortenURL(string? url)
    {
        var result = new UploadResult { URL = url };

        if (string.IsNullOrEmpty(url)) return result;

        var requestBody = new BitlyShortenRequestBody { long_url = url };
        if (!string.IsNullOrEmpty(Domain)) requestBody.domain = Domain;

        var json = JsonSerializer.Serialize(requestBody);
        var headers = GetAuthHeaders();

        result.Response = SendRequest(HttpMethod.Post, URLShorten, json, RequestHelpers.ContentTypeJSON, null, headers);
        var options = new JsonSerializerOptions
        {
            TypeInfoResolver = BitlyContext.Default
        };
        var responseData = JsonSerializer.Deserialize<BitlyShortenResponse>(result.Response, options);

        if (responseData?.link != null)
            result.ShortenedURL = responseData.link;

        return result;
    }

    private class BitlyShortenRequestBody
    {
        public string? long_url { get; set; }
        public string domain { get; set; } = "bit.ly";
    }

    public class BitlyShortenResponse
    {
        public DateTime created_at { get; set; }
        public string id { get; set; }
        public string? link { get; set; }
        public string long_url { get; set; }
    }
}

