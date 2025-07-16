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

namespace SnapX.Core.Upload.File;

public class GoogleDriveFileUploaderService : FileUploaderService
{
    public override FileDestination EnumValue { get; } = FileDestination.GoogleDrive;

    public override bool CheckConfig(UploadersConfig config)
    {
        return OAuth2Info.CheckOAuth(config.GoogleDriveOAuth2Info);
    }

    public override GenericUploader CreateUploader(UploadersConfig config, TaskReferenceHelper taskInfo)
    {
        return new GoogleDrive(config.GoogleDriveOAuth2Info)
        {
            IsPublic = config.GoogleDriveIsPublic,
            DirectLink = config.GoogleDriveDirectLink,
            FolderID = config.GoogleDriveUseFolder ? config.GoogleDriveFolderID : null,
            DriveID = config.GoogleDriveSelectedDrive?.id
        };
    }
}

public enum GoogleDrivePermissionRole
{
    owner, reader, writer, organizer, commenter
}

public enum GoogleDrivePermissionType
{
    user, group, domain, anyone
}
[JsonSerializable(typeof(GoogleDriveFile))]
[JsonSerializable(typeof(GoogleDriveFileList))]
[JsonSerializable(typeof(GoogleDriveSharedDriveList))]
internal partial class GoogleDriveContext : JsonSerializerContext;

public sealed class GoogleDrive : FileUploader, IOAuth2
{
    public GoogleOAuth2 OAuth2 { get; private set; }
    public OAuth2Info AuthInfo => OAuth2.AuthInfo;
    public bool IsPublic { get; set; }
    public bool DirectLink { get; set; }
    public string FolderID { get; set; }
    public string DriveID { get; set; }

    public JsonSerializerOptions Options { get; set; } = new()
    {
        TypeInfoResolver = GoogleDriveContext.Default
    };

    public static GoogleDriveSharedDrive MyDrive = new()
    {
        id = "", // empty defaults to user drive
        name = "My drive"
    };

    public GoogleDrive(OAuth2Info oauth)
    {
        OAuth2 = new GoogleOAuth2(oauth, this)
        {
            Scope = "https://www.googleapis.com/auth/drive.file https://www.googleapis.com/auth/userinfo.profile"
        };
    }

    [UnconditionalSuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code", Justification = "<Pending>")]
    public bool RefreshAccessToken()
    {
        return OAuth2.RefreshAccessToken();
    }

    public bool CheckAuthorization()
    {
        return OAuth2.CheckAuthorization();
    }

    public string? GetAuthorizationURL()
    {
        return OAuth2.GetAuthorizationURL();
    }

    [UnconditionalSuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code", Justification = "<Pending>")]
    public bool GetAccessToken(string? code)
    {
        return OAuth2.GetAccessToken(code);
    }

    [RequiresDynamicCode("Calls System.Text.Json.JsonSerializer.Serialize<TValue>(TValue, JsonSerializerOptions)")]
    [RequiresUnreferencedCode("Calls System.Text.Json.JsonSerializer.Serialize<TValue>(TValue, JsonSerializerOptions)")]
    private string GetMetadata(string? name, string parentID, string driveID = "")
    {
        object metadata;

        // If there's no parent folder, the drive behaves as parent
        if (string.IsNullOrEmpty(parentID))
        {
            parentID = driveID;
        }

        if (!string.IsNullOrEmpty(parentID))
        {
            metadata = new
            {
                name = name,
                driveId = driveID,
                parents = new[]
                {
                    parentID
                }
            };
        }
        else
        {
            metadata = new
            {
                name = name
            };
        }

        return JsonSerializer.Serialize(metadata);
    }

    [RequiresDynamicCode("Calls System.Text.Json.JsonSerializer.Serialize<TValue>(TValue, JsonSerializerOptions)")]
    [RequiresUnreferencedCode("Calls System.Text.Json.JsonSerializer.Serialize<TValue>(TValue, JsonSerializerOptions)")]
    private void SetPermissions(string fileID, GoogleDrivePermissionRole role, GoogleDrivePermissionType type, bool allowFileDiscovery)
    {
        if (!CheckAuthorization()) return;

        string? url = string.Format("https://www.googleapis.com/drive/v3/files/{0}/permissions?supportsAllDrives=true", fileID);

        string? json = JsonSerializer.Serialize(new
        {
            role = role.ToString(),
            type = type.ToString(),
            allowFileDiscovery = allowFileDiscovery.ToString()
        });

        SendRequest(HttpMethod.Post, url, json, RequestHelpers.ContentTypeJSON, null, OAuth2.GetAuthHeaders());
    }

    [RequiresUnreferencedCode("Uploader")]
    public List<GoogleDriveFile> GetFolders(string? driveID = "", bool trashed = false, bool writer = true)
    {
        if (!CheckAuthorization()) return null;

        string query = "mimeType = 'application/vnd.google-apps.folder'";

        if (!trashed)
        {
            query += " and trashed = false";
        }

        if (writer && string.IsNullOrEmpty(driveID))
        {
            query += " and 'me' in writers";
        }

        Dictionary<string, string?> args = new Dictionary<string, string?>
        {
            { "q", query },
            { "fields", "nextPageToken,files(id,name,description)" }
        };
        if (!string.IsNullOrEmpty(driveID))
        {
            args.Add("driveId", driveID);
            args.Add("corpora", "drive");
            args.Add("supportsAllDrives", "true");
            args.Add("includeItemsFromAllDrives", "true");
        }

        List<GoogleDriveFile> folders = [];
        string? pageToken = "";

        // Make sure we get all the pages of results
        do
        {
            args["pageToken"] = pageToken;
            string? response = SendRequest(HttpMethod.Get, "https://www.googleapis.com/drive/v3/files", args, OAuth2.GetAuthHeaders());
            pageToken = "";

            if (!string.IsNullOrEmpty(response))
            {
                GoogleDriveFileList fileList = JsonSerializer.Deserialize<GoogleDriveFileList>(response, Options);

                if (fileList != null)
                {
                    folders.AddRange(fileList.files);
                    pageToken = fileList.nextPageToken;
                }
            }
        }
        while (!string.IsNullOrEmpty(pageToken));

        return folders;
    }

    [RequiresUnreferencedCode("Uploader")]
    public List<GoogleDriveSharedDrive> GetDrives()
    {
        if (!CheckAuthorization()) return null;

        Dictionary<string, string?> args = [];
        List<GoogleDriveSharedDrive> drives = [];
        string? pageToken = "";

        // Make sure we get all the pages of results
        do
        {
            args["pageToken"] = pageToken;
            string? response = SendRequest(HttpMethod.Get, "https://www.googleapis.com/drive/v3/drives", args, OAuth2.GetAuthHeaders());
            pageToken = "";

            if (!string.IsNullOrEmpty(response))
            {
                GoogleDriveSharedDriveList driveList = JsonSerializer.Deserialize<GoogleDriveSharedDriveList>(response, Options);

                if (driveList != null)
                {
                    drives.AddRange(driveList.drives);
                    pageToken = driveList.nextPageToken;
                }
            }
        }
        while (!string.IsNullOrEmpty(pageToken));

        return drives;
    }

    [RequiresUnreferencedCode("Uploader")]
    public override UploadResult Upload(Stream stream, string? fileName)
    {
        if (!CheckAuthorization()) return null;

        string metadata = GetMetadata(fileName, FolderID, DriveID);

        UploadResult result = SendRequestFile("https://www.googleapis.com/upload/drive/v3/files?uploadType=multipart&fields=id,webViewLink,webContentLink&supportsAllDrives=true",
            stream, fileName, "file", headers: OAuth2.GetAuthHeaders(), contentType: "multipart/related", relatedData: metadata);

        if (!string.IsNullOrEmpty(result.Response))
        {
            GoogleDriveFile upload = JsonSerializer.Deserialize<GoogleDriveFile>(result.Response, Options);

            if (upload != null)
            {
                AllowReportProgress = false;

                if (IsPublic)
                {
                    SetPermissions(upload.id, GoogleDrivePermissionRole.reader, GoogleDrivePermissionType.anyone, false);
                }

                if (DirectLink)
                {
                    Uri webContentLink = new Uri(upload.webContentLink);

                    string leftPart = webContentLink.GetLeftPart(UriPartial.Path);

                    NameValueCollection queryString = HttpUtility.ParseQueryString(webContentLink.Query);
                    queryString.Remove("export");

                    result.URL = $"{leftPart}?{queryString}";
                }
                else
                {
                    result.URL = upload.webViewLink;
                }
            }
        }

        return result;
    }
}

public class GoogleDriveFile
{
    public string id { get; set; }
    public string? webViewLink { get; set; }
    public string webContentLink { get; set; }
    public string name { get; set; }
    public string description { get; set; }
}

public class GoogleDriveFileList
{
    public List<GoogleDriveFile> files { get; set; }
    public string? nextPageToken { get; set; }
}

public class GoogleDriveSharedDrive
{
    public string id { get; set; }
    public string name { get; set; }

    public override string ToString()
    {
        return name;
    }
}

public class GoogleDriveSharedDriveList
{
    public List<GoogleDriveSharedDrive> drives { get; set; }
    public string? nextPageToken { get; set; }
}
