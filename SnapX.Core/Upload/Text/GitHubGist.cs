
// SPDX-License-Identifier: GPL-3.0-or-later


using System.Collections.Specialized;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using SnapX.Core.Upload.BaseServices;
using SnapX.Core.Upload.BaseUploaders;
using SnapX.Core.Upload.OAuth;
using SnapX.Core.Upload.Utils;
using SnapX.Core.Utils;
using SnapX.Core.Utils.Miscellaneous;

namespace SnapX.Core.Upload.Text;

public class GitHubGistTextUploaderService : TextUploaderService
{
    public override TextDestination EnumValue => TextDestination.Gist;


    public override bool CheckConfig(UploadersConfig config)
    {
        return OAuth2Info.CheckOAuth(config.GistOAuth2Info);
    }

    public override GenericUploader CreateUploader(UploadersConfig config, TaskReferenceHelper taskInfo)
    {
        return new GitHubGist(config.GistOAuth2Info)
        {
            PublicUpload = config.GistPublishPublic,
            RawURL = config.GistRawURL,
            CustomURLAPI = config.GistCustomURL
        };
    }
}
[JsonSerializable(typeof(OAuth2Token))]
[JsonSerializable(typeof(GitHubGist.GistResponse))]
internal partial class GitHubContext : JsonSerializerContext;
public sealed class GitHubGist : TextUploader, IOAuth2Basic
{
    private const string URLAPI = "https://api.github.com";

    public OAuth2Info AuthInfo { get; private set; }

    public bool PublicUpload { get; set; }
    public bool RawURL { get; set; }
    public string? CustomURLAPI { get; set; }

    public GitHubGist(OAuth2Info oAuthInfos)
    {
        AuthInfo = oAuthInfos;
    }

    public string? GetAuthorizationURL()
    {
        var args = new Dictionary<string, string?>
        {
            { "client_id", AuthInfo.Client_ID },
            { "redirect_uri", Links.Callback },
            { "scope", "gist" }
        };

        return URLHelpers.CreateQueryString("https://github.com/login/oauth/authorize", args);
    }

    [RequiresDynamicCode("Uploader")]
    [RequiresUnreferencedCode("Uploader")]
    public bool GetAccessToken(string? code)
    {
        var args = new Dictionary<string, string?>
        {
            { "client_id", AuthInfo.Client_ID },
            { "client_secret", AuthInfo.Client_Secret },
            { "code", code }
        };

        var headers = new WebHeaderCollection()
        {
            "Accept", RequestHelpers.ContentTypeJSON
        };

        var response = SendRequestMultiPart("https://github.com/login/oauth/access_token", args, headers);
        if (string.IsNullOrEmpty(response)) return false;
        var options = new JsonSerializerOptions
        {
            TypeInfoResolver = GitHubContext.Default
        };
        var token = JsonSerializer.Deserialize<OAuth2Token>(response, options);
        if (string.IsNullOrEmpty(token?.access_token)) return false;

        AuthInfo.Token = token;
        return true;
    }

    [RequiresDynamicCode("Uploader")]
    [RequiresUnreferencedCode("Uploader")]
    public override UploadResult UploadText(string? text, string? fileName)
    {
        var ur = new UploadResult();
        if (string.IsNullOrEmpty(text) || string.IsNullOrEmpty(fileName)) return ur;

        var url = !string.IsNullOrEmpty(CustomURLAPI) ? CustomURLAPI : URLAPI;
        url = URLHelpers.CombineURL(url, "gists");

        var gistUpload = new GistUpload
        {
            description = "",
            @public = PublicUpload,
            files = new Dictionary<string?, GistUploadFileInfo>
            {
                { fileName, new GistUploadFileInfo { content = text } }
            }
        };

        var json = JsonSerializer.Serialize(gistUpload);

        var headers = new NameValueCollection
        {
            { "Authorization", "token " + AuthInfo.Token.access_token }
        };

        var response = SendRequest(HttpMethod.Post, url, json, RequestHelpers.ContentTypeJSON, null, headers);

        var gistResponse = JsonSerializer.Deserialize<GistResponse>(response);

        if (gistResponse != null)
        {
            ur.URL = RawURL ? gistResponse.files.First().Value.raw_url : gistResponse.html_url;
        }

        return ur;
    }

    private class GistUpload
    {
        public string description { get; set; }
        public bool @public { get; set; }
        public Dictionary<string?, GistUploadFileInfo> files { get; set; }
    }

    private class GistUploadFileInfo
    {
        public string? content { get; set; }
    }

    public class GistResponse
    {
        public string? html_url { get; set; }
        public Dictionary<string, GistResponseFileInfo> files { get; set; }
    }

    public class GistResponseFileInfo
    {
        public string filename { get; set; }
        public string? raw_url { get; set; }
    }
}

