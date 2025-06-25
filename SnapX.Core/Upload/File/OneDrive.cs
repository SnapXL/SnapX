
// SPDX-License-Identifier: GPL-3.0-or-later


using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization;
using SnapX.Core.Upload.BaseServices;
using SnapX.Core.Upload.BaseUploaders;
using SnapX.Core.Upload.OAuth;
using SnapX.Core.Upload.Utils;
using SnapX.Core.Utils;
using SnapX.Core.Utils.Miscellaneous;

namespace SnapX.Core.Upload.File;

public class OneDriveFileUploaderService : FileUploaderService
{
    public override FileDestination EnumValue => FileDestination.OneDrive;

    public override bool CheckConfig(UploadersConfig config)
    {
        return OAuth2Info.CheckOAuth(config.OneDriveV2OAuth2Info);
    }

    public override GenericUploader CreateUploader(UploadersConfig config, TaskReferenceHelper taskInfo)
    {
        return new OneDrive(config.OneDriveV2OAuth2Info)
        {
            FolderID = config.OneDriveV2SelectedFolder.id,
            AutoCreateShareableLink = config.OneDriveAutoCreateShareableLink,
            UseDirectLink = config.OneDriveUseDirectLink
        };
    }
}

[JsonSerializable(typeof(OneDriveFileList))]
[JsonSerializable(typeof(OneDriveUploadSession))]
[JsonSerializable(typeof(OAuth2Token))]
[JsonSerializable(typeof(OAuth2Info))]
internal partial class OneDriveContext : JsonSerializerContext;

public sealed class OneDrive : FileUploader, IOAuth2
{
    private const string? AuthorizationEndpoint = "https://login.microsoftonline.com/common/oauth2/v2.0/authorize";
    private const string? TokenEndpoint = "https://login.microsoftonline.com/common/oauth2/v2.0/token";
    private const int MaxSegmentSize = 64 * 1024 * 1024; // 64 MiB
    public OAuth2Info AuthInfo { get; set; }
    public string? FolderID { get; set; }
    public bool AutoCreateShareableLink { get; set; }
    public bool UseDirectLink { get; set; }
    private JsonSerializerOptions Options { get; set; } = new()
    {
        TypeInfoResolver = OneDriveContext.Default
    };


    public static OneDriveFileInfo RootFolder = new()
    {
        id = "", // empty defaults to root
        name = "Root folder"
    };

    public OneDrive(OAuth2Info authInfo)
    {
        AuthInfo = authInfo;
    }

    public string? GetAuthorizationURL()
    {
        Dictionary<string, string?> args = new Dictionary<string, string?>
        {
            { "client_id", AuthInfo.Client_ID },
            { "scope", "offline_access files.readwrite" },
            { "response_type", "code" },
            { "redirect_uri", Links.Callback }
        };
        if (AuthInfo.Proof != null)
        {
            args.Add("code_challenge", AuthInfo.Proof.CodeChallenge);
            args.Add("code_challenge_method", AuthInfo.Proof.ChallengeMethod);
        }

        return URLHelpers.CreateQueryString(AuthorizationEndpoint, args);
    }

    [RequiresDynamicCode("Uploader")]
    [RequiresUnreferencedCode("Uploader")]
    public bool GetAccessToken(string? code)
    {
        var args = new Dictionary<string, string?>
        {
            { "client_id", AuthInfo.Client_ID },
            { "redirect_uri", Links.Callback },
            { "client_secret", AuthInfo.Client_Secret },
            { "code", code },
            { "grant_type", "authorization_code" }
        };

        if (AuthInfo.Proof != null)
            args.Add("code_verifier", AuthInfo.Proof.CodeVerifier);

        var response = SendRequestURLEncoded(HttpMethod.Post, TokenEndpoint, args);
        if (string.IsNullOrEmpty(response)) return false;

        var token = JsonSerializer.Deserialize<OAuth2Token>(response, Options);
        if (token?.access_token == null) return false;

        token.UpdateExpireDate();
        AuthInfo.Token = token;
        return true;
    }

    [RequiresDynamicCode("Uploader")]
    [RequiresUnreferencedCode("Uploader")]
    public bool RefreshAccessToken()
    {
        if (!OAuth2Info.CheckOAuth(AuthInfo) || string.IsNullOrEmpty(AuthInfo.Token.refresh_token)) return false;

        var args = new Dictionary<string, string?>
        {
            { "client_id", AuthInfo.Client_ID },
            { "client_secret", AuthInfo.Client_Secret },
            { "refresh_token", AuthInfo.Token.refresh_token },
            { "grant_type", "refresh_token" }
        };

        var response = SendRequestURLEncoded(HttpMethod.Post, TokenEndpoint, args);
        if (string.IsNullOrEmpty(response)) return false;

        var token = JsonSerializer.Deserialize<OAuth2Token>(response, Options);
        if (token?.access_token == null) return false;

        token.UpdateExpireDate();
        var refresh_token = AuthInfo.Token.refresh_token;
        AuthInfo.Token = token;
        AuthInfo.Token.refresh_token = refresh_token;
        return true;
    }

    [UnconditionalSuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code", Justification = "<Pending>")]
    public bool CheckAuthorization()
    {
        if (!OAuth2Info.CheckOAuth(AuthInfo))
        {
            Errors.Add("Login is required.");
            return false;
        }

        if (AuthInfo.Token.IsExpired && !RefreshAccessToken())
        {
            Errors.Add("Refresh access token failed.");
            return false;
        }

        return true;
    }

    private NameValueCollection GetAuthHeaders() =>
        new() { { "Authorization", $"Bearer {AuthInfo.Token.access_token}" } };


    private string? GetFolderUrl(string? folderID) =>
        string.IsNullOrEmpty(folderID) ? "me/drive/root" : URLHelpers.CombineURL("me/drive/items", folderID);

    [RequiresUnreferencedCode("Calls System.Text.Json.JsonSerializer.Serialize<TValue>(TValue, JsonSerializerOptions)")]
    private string? CreateSession(string? fileName)
    {
        var json = JsonSerializer.Serialize(new
        {
            item = new Dictionary<string, string>
            {
                { "@microsoft.graph.conflictBehavior", "replace" }
            }
        });

        var folderPath = GetFolderUrl(FolderID);
        var url = URLHelpers.BuildUri("https://graph.microsoft.com", $"/v1.0/{folderPath}:/{fileName}:/createUploadSession");

        AllowReportProgress = false;
        var response = SendRequest(HttpMethod.Post, url, json, RequestHelpers.ContentTypeJSON, headers: GetAuthHeaders());
        AllowReportProgress = true;

        return JsonSerializer.Deserialize<OneDriveUploadSession>(response, Options)?.uploadUrl;
    }

    [RequiresUnreferencedCode("Uploader")]
    public override UploadResult Upload(Stream stream, string? fileName)
    {
        if (!CheckAuthorization()) return null;

        var sessionUrl = CreateSession(fileName);
        var result = default(UploadResult);
        long position = 0;

        while (position < stream.Length)
        {
            result = SendRequestFileRange(sessionUrl, stream, fileName, position, MaxSegmentSize);
            if (!result.IsSuccess)
            {
                SendRequest(HttpMethod.Delete, sessionUrl);
                return result;
            }

            position += MaxSegmentSize;
        }

        if (!result.IsSuccess) return result;

        var uploadInfo = JsonSerializer.Deserialize<OneDriveFileInfo>(result.Response, Options);

        if (AutoCreateShareableLink)
        {
            AllowReportProgress = false;
            result.URL = CreateShareableLink(uploadInfo.id, UseDirectLink ? OneDriveLinkType.Embed : OneDriveLinkType.Read);
        }
        else
        {
            result.URL = uploadInfo.webUrl;
        }

        return result;
    }

    [RequiresUnreferencedCode("Uploader")]
    public string? CreateShareableLink(string id, OneDriveLinkType linkType = OneDriveLinkType.Read)
    {
        var linkTypeValue = linkType switch
        {
            OneDriveLinkType.Embed => "embed",
            OneDriveLinkType.Edit => "edit",
            _ => "view" // Default is "view" for OneDriveLinkType.Read
        };

        var json = JsonSerializer.Serialize(new { type = linkTypeValue, scope = "anonymous" });
        var response = SendRequest(HttpMethod.Post, $"https://graph.microsoft.com/v1.0/me/drive/items/{id}/createLink", json, RequestHelpers.ContentTypeJSON, headers: GetAuthHeaders());

        var permissionInfo = JsonSerializer.Deserialize<OneDrivePermission>(response, Options);
        return permissionInfo?.link?.webUrl;
    }

    [RequiresUnreferencedCode("Uploader")]
    public OneDriveFileList GetPathInfo(string? id)
    {
        if (!CheckAuthorization()) return null;

        var folderPath = GetFolderUrl(id);
        var args = new Dictionary<string, string?>
        {
            { "select", "id,name" },
            { "filter", "folder ne null" }
        };

        var response = SendRequest(HttpMethod.Get, $"https://graph.microsoft.com/v1.0/{folderPath}/children", args, GetAuthHeaders());
        return response == null ? null : JsonSerializer.Deserialize<OneDriveFileList>(response, Options);
    }

}

public class OneDriveFileInfo
{
    public string id { get; set; }
    public string name { get; set; }
    public string? webUrl { get; set; }
}

public class OneDrivePermission
{
    public OneDriveShareableLink link { get; set; }
}

public class OneDriveShareableLink
{
    public string? webUrl { get; set; }
    public string webHtml { get; set; }
}

public class OneDriveFileList
{
    public OneDriveFileInfo[] value { get; set; }
}

public class OneDriveUploadSession
{
    public string? uploadUrl { get; set; }
    public string[] nextExpectedRanges { get; set; }
}

public enum OneDriveLinkType
{
    [Description("An embedded link, which is an HTML code snippet that you can insert into a webpage to provide an interactive view of the corresponding file.")]
    Embed,
    [Description("A read-only link, which is a link to a read-only version of the folder or file.")]
    Read,
    [Description("A read-write link, which is a link to a read-write version of the folder or file.")]
    Edit
}

