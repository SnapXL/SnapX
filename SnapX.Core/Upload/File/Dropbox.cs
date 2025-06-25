
// SPDX-License-Identifier: GPL-3.0-or-later


using System.Collections.Specialized;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization;
using SnapX.Core.Upload.BaseServices;
using SnapX.Core.Upload.BaseUploaders;
using SnapX.Core.Upload.OAuth;
using SnapX.Core.Upload.Utils;
using SnapX.Core.Utils;
using SnapX.Core.Utils.Parsers;

namespace SnapX.Core.Upload.File;

public class DropboxFileUploaderService : FileUploaderService
{
    public override FileDestination EnumValue { get; } = FileDestination.Dropbox;
    public override bool CheckConfig(UploadersConfig config)
    {
        return OAuth2Info.CheckOAuth(config.DropboxOAuth2Info);
    }

    public override GenericUploader CreateUploader(UploadersConfig config, TaskReferenceHelper taskInfo)
    {
        return new Dropbox(config.DropboxOAuth2Info)
        {
            UploadPath = NameParser.Parse(NameParserType.Default, Dropbox.VerifyPath(config.DropboxUploadPath)),
            AutoCreateShareableLink = config.DropboxAutoCreateShareableLink,
            UseDirectLink = config.DropboxUseDirectLink
        };
    }
}
[JsonSerializable(typeof(OAuth2Token))]
[JsonSerializable(typeof(DropboxAccount))]
[JsonSerializable(typeof(DropboxMetadata))]
[JsonSerializable(typeof(DropboxLinkMetadata))]
[JsonSerializable(typeof(DropboxListSharedLinksResult))]
internal partial class DropboxContext : JsonSerializerContext;
public sealed class Dropbox : FileUploader, IOAuth2Basic
{
    private const string APIVersion = "2";

    private const string URLWEB = "https://www.dropbox.com";
    private const string URLAPIBase = "https://api.dropboxapi.com";
    private const string URLAPI = URLAPIBase + "/" + APIVersion;
    private const string URLContent = "https://content.dropboxapi.com/" + APIVersion;
    private const string URLNotify = "https://notify.dropboxapi.com/" + APIVersion;

    private const string? URLOAuth2Authorize = URLWEB + "/oauth2/authorize";
    private const string? URLOAuth2Token = URLAPIBase + "/oauth2/token";

    private const string? URLGetCurrentAccount = URLAPI + "/users/get_current_account";
    private const string? URLDownload = URLContent + "/files/download";
    private const string? URLUpload = URLContent + "/files/upload";
    private const string? URLGetMetadata = URLAPI + "/files/get_metadata";
    private const string? URLCreateSharedLink = URLAPI + "/sharing/create_shared_link_with_settings";
    private const string? URLListSharedLinks = URLAPI + "/sharing/list_shared_links";
    private const string? URLCopy = URLAPI + "/files/copy";
    private const string? URLCreateFolder = URLAPI + "/files/create_folder";
    private const string? URLDelete = URLAPI + "/files/delete";
    private const string? URLMove = URLAPI + "/files/move";

    private const string? URLShareDirect = "https://dl.dropboxusercontent.com/scl/fi/";

    public OAuth2Info AuthInfo { get; set; }
    public string? UploadPath { get; set; }
    public bool AutoCreateShareableLink { get; set; }
    public bool UseDirectLink { get; set; }

    public Dropbox(OAuth2Info oauth)
    {
        AuthInfo = oauth;
    }

    [UnconditionalSuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code", Justification = "<Pending>")]
    public override UploadResult Upload(Stream stream, string? fileName)
    {
        return UploadFile(stream, UploadPath, fileName, AutoCreateShareableLink, UseDirectLink);
    }

    public string? GetAuthorizationURL()
    {
        var args = new Dictionary<string, string?>
        {
            { "response_type", "code" },
            { "client_id", AuthInfo.Client_ID }
        };

        return URLHelpers.CreateQueryString(URLOAuth2Authorize, args);
    }

    [RequiresDynamicCode("Uploader")]
    [RequiresUnreferencedCode("Uploader")]
    public bool GetAccessToken(string? code)
    {
        var args = new Dictionary<string, string?>
        {
            { "client_id", AuthInfo.Client_ID },
            { "client_secret", AuthInfo.Client_Secret },
            { "grant_type", "authorization_code" },
            { "code", code }
        };

        var response = SendRequestMultiPart(URLOAuth2Token, args);
        if (string.IsNullOrEmpty(response)) return false;
        var options = new JsonSerializerOptions()
        {
            TypeInfoResolver = DropboxContext.Default
        };
        var token = JsonSerializer.Deserialize<OAuth2Token>(response, options);

        if (token != null && !string.IsNullOrEmpty(token.access_token))
        {
            AuthInfo.Token = token;
            return true;
        }

        return false;
    }

    private NameValueCollection GetAuthHeaders()
    {
        var headers = new NameValueCollection
        {
            { "Authorization", "Bearer " + AuthInfo.Token.access_token }
        };
        return headers;
    }

    public static string? VerifyPath(string? path, string? fileName = null)
    {
        if (!string.IsNullOrEmpty(path))
        {
            path = path.Trim().Replace('\\', '/').Trim('/');
            path = URLHelpers.AddSlash(path, SlashType.Prefix);

            if (!string.IsNullOrEmpty(fileName))
            {
                path = URLHelpers.CombineURL(path, fileName);
            }

            return path;
        }

        if (!string.IsNullOrEmpty(fileName))
        {
            return fileName;
        }

        return "";
    }

    [RequiresDynamicCode("Uploader")]
    [RequiresUnreferencedCode("Uploader")]
    public DropboxAccount GetCurrentAccount()
    {
        if (OAuth2Info.CheckOAuth(AuthInfo))
        {
            var response = SendRequest(HttpMethod.Post, URLGetCurrentAccount, "null", RequestHelpers.ContentTypeJSON, null, GetAuthHeaders());

            if (!string.IsNullOrEmpty(response))
            {
                var options = new JsonSerializerOptions()
                {
                    TypeInfoResolver = DropboxContext.Default
                };
                return JsonSerializer.Deserialize<DropboxAccount>(response, options);
            }
        }

        return null;
    }

    [RequiresUnreferencedCode("Uploader")]
    public bool DownloadFile(string? path, Stream downloadStream)
    {
        if (!string.IsNullOrEmpty(path) && OAuth2Info.CheckOAuth(AuthInfo))
        {
            var json = JsonSerializer.Serialize(new
            {
                path = VerifyPath(path)
            });

            var args = new Dictionary<string, string?>
            {
                { "arg", json }
            };

            return SendRequestDownload(HttpMethod.Post, URLDownload, downloadStream, args, GetAuthHeaders(), null, RequestHelpers.ContentTypeJSON);
        }

        return false;
    }

    [RequiresUnreferencedCode("Uploader")]
    public UploadResult UploadFile(Stream stream, string? path, string? fileName, bool createShareableLink = false, bool useDirectLink = false)
    {
        if (stream.Length > 150000000)
        {
            Errors.Add("There's a 150MB limit to uploads through the API.");
            return null;
        }

        var json = JsonSerializer.Serialize(new
        {
            path = VerifyPath(path, fileName),
            mode = "overwrite",
            autorename = false,
            mute = true
        });

        var args = new Dictionary<string, string?>
        {
            { "arg", json }
        };

        var response = SendRequest(HttpMethod.Post, URLUpload, stream, RequestHelpers.ContentTypeOctetStream, args, GetAuthHeaders());

        var ur = new UploadResult(response);

        if (!string.IsNullOrEmpty(ur.Response))
        {
            var options = new JsonSerializerOptions()
            {
                TypeInfoResolver = DropboxContext.Default
            };
            var metadata = JsonSerializer.Deserialize<DropboxMetadata>(ur.Response, options);

            if (metadata != null)
            {
                if (createShareableLink)
                {
                    AllowReportProgress = false;

                    ur.URL = CreateShareableLink(metadata.path_display, useDirectLink);
                }
                else
                {
                    ur.IsURLExpected = false;
                }
            }
        }

        return ur;
    }

    [RequiresUnreferencedCode("Uploader")]
    public DropboxMetadata GetMetadata(string? path)
    {
        DropboxMetadata metadata = null;

        if (path != null && OAuth2Info.CheckOAuth(AuthInfo))
        {
            var json = JsonSerializer.Serialize(new
            {
                path = VerifyPath(path),
                include_media_info = false,
                include_deleted = false,
                include_has_explicit_shared_members = false
            });

            var response = SendRequest(HttpMethod.Post, URLGetMetadata, json, RequestHelpers.ContentTypeJSON, null, GetAuthHeaders());

            if (!string.IsNullOrEmpty(response))
            {
                var options = new JsonSerializerOptions()
                {
                    TypeInfoResolver = DropboxContext.Default
                };
                metadata = JsonSerializer.Deserialize<DropboxMetadata>(response, options);
            }
        }

        return metadata;
    }

    [UnconditionalSuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code", Justification = "<Pending>")]
    public bool IsExists(string? path)
    {
        var metadata = GetMetadata(path);

        return metadata != null && !metadata.tag.Equals("deleted", StringComparison.OrdinalIgnoreCase);
    }

    [RequiresUnreferencedCode("Uploader")]
    public string? CreateShareableLink(string? path, bool directLink)
    {
        if (!string.IsNullOrEmpty(path) && OAuth2Info.CheckOAuth(AuthInfo))
        {
            var json = JsonSerializer.Serialize(new
            {
                path = VerifyPath(path),
                settings = new
                {
                    requested_visibility = "public" // Anyone who has received the link can access it. No login required.
                }
            });

            var response = SendRequest(HttpMethod.Post, URLCreateSharedLink, json, RequestHelpers.ContentTypeJSON, null, GetAuthHeaders());

            DropboxLinkMetadata linkMetadata = null;

            if (!string.IsNullOrEmpty(response))
            {
                var options = new JsonSerializerOptions()
                {
                    TypeInfoResolver = DropboxContext.Default
                };
                linkMetadata = JsonSerializer.Deserialize<DropboxLinkMetadata>(response, options);
            }
            else if (IsError && Errors.Errors[Errors.Count - 1].Text.Contains("\"shared_link_already_exists\"")) // Ugly workaround
            {
                var result = ListSharedLinks(path, true);

                if (result != null && result.links != null && result.links.Length > 0)
                {
                    linkMetadata = result.links[0];
                }
            }

            if (linkMetadata != null)
            {
                if (directLink)
                {
                    return GetDirectShareableURL(linkMetadata.url);
                }
                else
                {
                    return linkMetadata.url;
                }
            }
        }

        return null;
    }

    [RequiresUnreferencedCode("Uploader")]
    public DropboxListSharedLinksResult ListSharedLinks(string? path, bool directOnly = false)
    {
        DropboxListSharedLinksResult result = null;

        if (path != null && OAuth2Info.CheckOAuth(AuthInfo))
        {
            var json = JsonSerializer.Serialize(new
            {
                path = VerifyPath(path),
                direct_only = directOnly
            });

            var response = SendRequest(HttpMethod.Post, URLListSharedLinks, json, RequestHelpers.ContentTypeJSON, null, GetAuthHeaders());

            if (!string.IsNullOrEmpty(response))
            {
                var options = new JsonSerializerOptions()
                {
                    TypeInfoResolver = DropboxContext.Default
                };
                result = JsonSerializer.Deserialize<DropboxListSharedLinksResult>(response, options);
            }
        }

        return result;
    }

    [RequiresUnreferencedCode("Uploader")]
    public DropboxMetadata Copy(string? fromPath, string? toPath)
    {
        DropboxMetadata metadata = null;

        if (!string.IsNullOrEmpty(fromPath) && !string.IsNullOrEmpty(toPath) && OAuth2Info.CheckOAuth(AuthInfo))
        {
            string? json = JsonSerializer.Serialize(new
            {
                from_path = VerifyPath(fromPath),
                to_path = VerifyPath(toPath)
            });

            string? response = SendRequest(HttpMethod.Post, URLCopy, json, RequestHelpers.ContentTypeJSON, null, GetAuthHeaders());

            if (!string.IsNullOrEmpty(response))
            {
                var options = new JsonSerializerOptions()
                {
                    TypeInfoResolver = DropboxContext.Default
                };
                metadata = JsonSerializer.Deserialize<DropboxMetadata>(response, options);
            }
        }

        return metadata;
    }

    [RequiresUnreferencedCode("Uploader")]
    public DropboxMetadata CreateFolder(string? path)
    {
        DropboxMetadata metadata = null;

        if (!string.IsNullOrEmpty(path) && OAuth2Info.CheckOAuth(AuthInfo))
        {
            var json = JsonSerializer.Serialize(new
            {
                path = VerifyPath(path)
            });

            var response = SendRequest(HttpMethod.Post, URLCreateFolder, json, RequestHelpers.ContentTypeJSON, null, GetAuthHeaders());

            if (!string.IsNullOrEmpty(response))
            {
                var options = new JsonSerializerOptions()
                {
                    TypeInfoResolver = DropboxContext.Default
                };
                metadata = JsonSerializer.Deserialize<DropboxMetadata>(response, options);
            }
        }

        return metadata;
    }

    [RequiresUnreferencedCode("Uploader")]
    public DropboxMetadata Delete(string? path)
    {
        DropboxMetadata metadata = null;

        if (!string.IsNullOrEmpty(path) && OAuth2Info.CheckOAuth(AuthInfo))
        {
            var json = JsonSerializer.Serialize(new
            {
                path = VerifyPath(path)
            });

            var response = SendRequest(HttpMethod.Post, URLDelete, json, RequestHelpers.ContentTypeJSON, null, GetAuthHeaders());

            if (!string.IsNullOrEmpty(response))
            {
                var options = new JsonSerializerOptions()
                {
                    TypeInfoResolver = DropboxContext.Default
                };
                metadata = JsonSerializer.Deserialize<DropboxMetadata>(response, options);
            }
        }

        return metadata;
    }

    [RequiresUnreferencedCode("Uploader")]
    public DropboxMetadata Move(string? fromPath, string? toPath)
    {
        DropboxMetadata metadata = null;

        if (!string.IsNullOrEmpty(fromPath) && !string.IsNullOrEmpty(toPath) && OAuth2Info.CheckOAuth(AuthInfo))
        {
            var json = JsonSerializer.Serialize(new
            {
                from_path = VerifyPath(fromPath),
                to_path = VerifyPath(toPath)
            });

            var response = SendRequest(HttpMethod.Post, URLMove, json, RequestHelpers.ContentTypeJSON, null, GetAuthHeaders());

            if (!string.IsNullOrEmpty(response))
            {
                var options = new JsonSerializerOptions()
                {
                    TypeInfoResolver = DropboxContext.Default
                };
                metadata = JsonSerializer.Deserialize<DropboxMetadata>(response, options);
            }
        }

        return metadata;
    }

    private static string? GetDirectShareableURL(string? url)
    {
        string find = "dropbox.com/scl/fi/";
        int index = url.IndexOf(find);

        if (index > -1)
        {
            string? path = url.Remove(0, index + find.Length);

            if (!string.IsNullOrEmpty(path))
            {
                return URLHelpers.CombineURL(URLShareDirect, path);
            }
        }

        return null;
    }
}

public class DropboxAccount
{
    public string account_id { get; set; }
    public DropboxAccountName name { get; set; }
    public string email { get; set; }
    public bool email_verified { get; set; }
    public bool disabled { get; set; }
    public string locale { get; set; }
    public string referral_link { get; set; }
    public bool is_paired { get; set; }
    public DropboxAccountType account_type { get; set; }
    public string profile_photo_url { get; set; }
    public string country { get; set; }
}

public class DropboxAccountName
{
    public string given_name { get; set; }
    public string surname { get; set; }
    public string familiar_name { get; set; }
    public string display_name { get; set; }
}

public class DropboxAccountType
{
    [JsonPropertyName(".tag")]
    public string tag { get; set; }
}

public class DropboxMetadata
{
    [JsonPropertyName(".tag")]
    public string tag { get; set; }
    public string name { get; set; }
    public string id { get; set; }
    public string client_modified { get; set; }
    public string server_modified { get; set; }
    public string rev { get; set; }
    public int size { get; set; }
    public string path_lower { get; set; }
    public string? path_display { get; set; }
    public DropboxMetadataSharingInfo sharing_info { get; set; }
    public List<DropboxMetadataPropertyGroup> property_groups { get; set; }
    public bool has_explicit_shared_members { get; set; }
}

public class DropboxMetadataSharingInfo
{
    public bool read_only { get; set; }
    public string parent_shared_folder_id { get; set; }
    public string modified_by { get; set; }
}

public class DropboxMetadataPropertyGroup
{
    public string template_id { get; set; }
    public List<DropboxMetadataPropertyGroupField> fields { get; set; }
}

public class DropboxMetadataPropertyGroupField
{
    public string name { get; set; }
    public string value { get; set; }
}

public class DropboxLinkMetadata
{
    [JsonPropertyName(".tag")]
    public string tag { get; set; }
    public string? url { get; set; }
    public string name { get; set; }
    public DropboxLinkMetadataPermissions link_permissions { get; set; }
    public string client_modified { get; set; }
    public string server_modified { get; set; }
    public string rev { get; set; }
    public int size { get; set; }
    public string id { get; set; }
    public string path_lower { get; set; }
    public DropboxLinkMetadataTeamMemberInfo team_member_info { get; set; }
}

public class DropboxLinkMetadataPermissions
{
    public bool can_revoke { get; set; }
    public DropboxLinkMetadataResolvedVisibility resolved_visibility { get; set; }
    public DropboxLinkMetadataRevokeFailureReason revoke_failure_reason { get; set; }
}

public class DropboxLinkMetadataResolvedVisibility
{
    [JsonPropertyName(".tag")]
    public string tag { get; set; }
}

public class DropboxLinkMetadataRevokeFailureReason
{
    [JsonPropertyName(".tag")]
    public string tag { get; set; }
}

public class DropboxLinkMetadataTeamMemberInfo
{
    public DropboxLinkMetadataTeamInfo team_info { get; set; }
    public string display_name { get; set; }
    public string member_id { get; set; }
}

public class DropboxLinkMetadataTeamInfo
{
    public string id { get; set; }
    public string name { get; set; }
}

public class DropboxListSharedLinksResult
{
    public DropboxLinkMetadata[] links { get; set; }
    public bool has_more { get; set; }
    public string cursor { get; set; }
}
