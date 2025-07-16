// SPDX-License-Identifier: GPL-3.0-or-later


using System.Collections.Specialized;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using SnapX.Core.Upload.BaseServices;
using SnapX.Core.Upload.BaseUploaders;
using SnapX.Core.Upload.OAuth;
using SnapX.Core.Upload.Utils;
using SnapX.Core.Utils;

namespace SnapX.Core.Upload.File;
public class BoxFileUploaderService : FileUploaderService
{
    public override FileDestination EnumValue => FileDestination.Box;

    public override bool CheckConfig(UploadersConfig config)
    {
        return OAuth2Info.CheckOAuth(config.BoxOAuth2Info);
    }

    public override GenericUploader CreateUploader(UploadersConfig config, TaskReferenceHelper taskInfo)
    {
        return new Box(config.BoxOAuth2Info)
        {
            FolderID = config.BoxSelectedFolder.id,
            Share = config.BoxShare,
            ShareAccessLevel = config.BoxShareAccessLevel
        };
    }
}

public sealed class Box : FileUploader, IOAuth2
{
    public static BoxFileEntry RootFolder = new BoxFileEntry
    {
        type = "folder",
        id = "0",
        name = "Root folder"
    };

    public OAuth2Info AuthInfo { get; set; }
    public string FolderID { get; set; }
    public bool Share { get; set; }
    public BoxShareAccessLevel ShareAccessLevel { get; set; }

    public Box(OAuth2Info oauth)
    {
        AuthInfo = oauth;
        FolderID = "0";
        Share = true;
        ShareAccessLevel = BoxShareAccessLevel.Open;
    }

    public string? GetAuthorizationURL() =>
        URLHelpers.CreateQueryString("https://www.box.com/api/oauth2/authorize", new Dictionary<string, string?>
        {
            { "response_type", "code" },
            { "client_id", AuthInfo.Client_ID }
        });

    [RequiresDynamicCode("Uploader")]
    [RequiresUnreferencedCode("Uploader")]
    public bool GetAccessToken(string? pin)
    {
        var args = new Dictionary<string, string?>
        {
            { "grant_type", "authorization_code" },
            { "code", pin },
            { "client_id", AuthInfo.Client_ID },
            { "client_secret", AuthInfo.Client_Secret }
        };

        var response = SendRequestMultiPart("https://www.box.com/api/oauth2/token", args);
        var token = string.IsNullOrEmpty(response) ? null : JsonSerializer.Deserialize<OAuth2Token>(response);
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
            { "grant_type", "refresh_token" },
            { "refresh_token", AuthInfo.Token.refresh_token },
            { "client_id", AuthInfo.Client_ID },
            { "client_secret", AuthInfo.Client_Secret }
        };

        var response = SendRequestMultiPart("https://www.box.com/api/oauth2/token", args);
        var token = string.IsNullOrEmpty(response) ? null : JsonSerializer.Deserialize<OAuth2Token>(response);
        if (token?.access_token == null) return false;

        token.UpdateExpireDate();
        AuthInfo.Token = token;
        return true;
    }

    private NameValueCollection GetAuthHeaders() =>
        new NameValueCollection { { "Authorization", $"Bearer {AuthInfo.Token.access_token}" } };

    [UnconditionalSuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code", Justification = "<Pending>")]
    public bool CheckAuthorization()
    {
        if (OAuth2Info.CheckOAuth(AuthInfo))
        {
            if (AuthInfo.Token.IsExpired && !RefreshAccessToken())
            {
                Errors.Add("Refresh access token failed.");
                return false;
            }
        }
        else
        {
            Errors.Add("Box login is required.");
            return false;
        }

        return true;
    }

    [UnconditionalSuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code", Justification = "<Pending>")]
    public BoxFileInfo GetFiles(BoxFileEntry folder)
    {
        return GetFiles(folder.id);
    }

    [RequiresUnreferencedCode("Uploader")]
    public BoxFileInfo GetFiles(string id)
    {
        if (!CheckAuthorization()) return null;

        var url = $"https://api.box.com/2.0/folders/{id}/items";
        var response = SendRequest(HttpMethod.Get, url, headers: GetAuthHeaders());

        return string.IsNullOrEmpty(response) ? null : JsonSerializer.Deserialize<BoxFileInfo>(response);
    }

    [RequiresUnreferencedCode("Uploader")]
    public string? CreateSharedLink(string id, BoxShareAccessLevel accessLevel)
    {
        var response = SendRequest(HttpMethod.Put, $"https://api.box.com/2.0/files/{id}",
            $"{{\"shared_link\": {{\"access\": \"{accessLevel.ToString().ToLower()}\"}}}}", headers: GetAuthHeaders());

        if (string.IsNullOrEmpty(response)) return null;

        var fileEntry = JsonSerializer.Deserialize<BoxFileEntry>(response);
        return fileEntry?.shared_link?.url;
    }

    [RequiresUnreferencedCode("Uploader")]
    public override UploadResult Upload(Stream stream, string? fileName)
    {
        if (!CheckAuthorization()) return null;

        FolderID ??= "0"; // Use FolderID if set, otherwise default to "0"

        var args = new Dictionary<string, string?>
        {
            { "parent_id", FolderID }
        };

        var result = SendRequestFile("https://upload.box.com/api/2.0/files/content", stream, fileName, "filename", args, GetAuthHeaders());

        if (!result.IsSuccess) return result;

        var fileInfo = JsonSerializer.Deserialize<BoxFileInfo>(result.Response);
        if (fileInfo?.entries?.Length < 0) return result;

        var fileEntry = fileInfo.entries[0];

        result.URL = Share
            ? CreateSharedLink(fileEntry.id, ShareAccessLevel)
            : $"https://app.box.com/files/0/f/{fileEntry.parent.id}/1/f_{fileEntry.id}";

        return result;
    }

}

public class BoxFileInfo
{
    public BoxFileEntry[] entries { get; set; }
}

public class BoxFileEntry
{
    public string type { get; set; }
    public string id { get; set; }
    public string sequence_id { get; set; }
    public string etag { get; set; }
    public string name { get; set; }
    public BoxFileSharedLink shared_link { get; set; }
    public BoxFileEntry parent { get; set; }
}

public class BoxFileSharedLink
{
    public string? url { get; set; }
}

public class BoxFolder
{
    public string ID;
    public string Name;
    public string User_id;
    public string Description;
    public string Shared;
    public string Shared_link;
    public string Permissions;

    //public List<BoxTag> Tags;
    //public List<BoxFile> Files;
    public List<BoxFolder> Folders = [];
}

