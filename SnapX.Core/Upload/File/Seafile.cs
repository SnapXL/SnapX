
// SPDX-License-Identifier: GPL-3.0-or-later


using System.Collections.Specialized;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Web;
using SnapX.Core.Upload.BaseServices;
using SnapX.Core.Upload.BaseUploaders;
using SnapX.Core.Upload.Utils;
using SnapX.Core.Utils;

namespace SnapX.Core.Upload.File;

public class SeafileFileUploaderService : FileUploaderService
{
    public override FileDestination EnumValue { get; } = FileDestination.Seafile;

    public override bool CheckConfig(UploadersConfig config)
    {
        return !string.IsNullOrEmpty(config.SeafileAPIURL) && !string.IsNullOrEmpty(config.SeafileAuthToken) && !string.IsNullOrEmpty(config.SeafileRepoID);
    }

    public override GenericUploader CreateUploader(UploadersConfig config, TaskReferenceHelper taskInfo)
    {
        return new Seafile(config.SeafileAPIURL, config.SeafileAuthToken, config.SeafileRepoID)
        {
            Path = config.SeafilePath,
            IsLibraryEncrypted = config.SeafileIsLibraryEncrypted,
            EncryptedLibraryPassword = config.SeafileEncryptedLibraryPassword,
            ShareDaysToExpire = config.SeafileShareDaysToExpire,
            SharePassword = config.SeafileSharePassword,
            CreateShareableURL = config.SeafileCreateShareableURL,
            CreateShareableURLRaw = config.SeafileCreateShareableURLRaw,
            IgnoreInvalidCert = config.SeafileIgnoreInvalidCert
        };
    }
}
[JsonSerializable(typeof(SeafileCheckAccInfoResponse))]
[JsonSerializable(typeof(SeafileAuthResponse))]
[JsonSerializable(typeof(SeafileLibraryObj))]
[JsonSerializable(typeof(SeafileDefaultLibraryObj))]
internal partial class SeafileContext : JsonSerializerContext;

public sealed class Seafile : FileUploader
{
    public string? APIURL { get; set; }
    public string AuthToken { get; set; }
    public string RepoID { get; set; }
    public string? Path { get; set; }
    public bool IsLibraryEncrypted { get; set; }
    public string EncryptedLibraryPassword { get; set; }
    public int ShareDaysToExpire { get; set; }
    public string? SharePassword { get; set; }
    public bool CreateShareableURL { get; set; }
    public bool CreateShareableURLRaw { get; set; }
    public bool IgnoreInvalidCert { get; set; }

    private JsonSerializerOptions Options = new()
    {
        TypeInfoResolver = SeafileContext.Default
    };

    public Seafile(string? apiurl, string authtoken, string repoid)
    {
        APIURL = apiurl;
        AuthToken = authtoken;
        RepoID = repoid;
    }

    #region SeafileAuth

    [RequiresDynamicCode("Uploader")]
    [RequiresUnreferencedCode("Uploader")]
    public string GetAuthToken(string username, string? password)
    {
        var url = URLHelpers.FixPrefix(APIURL);
        url = URLHelpers.CombineURL(url, "auth-token/?format=json");

        var args = new Dictionary<string, string?>
        {
            { "username", username },
            { "password", password }
        };

        var response = SendRequestMultiPart(url, args);

        if (!string.IsNullOrEmpty(response))
        {
            var AuthResult = JsonSerializer.Deserialize<SeafileAuthResponse>(response, Options);

            return AuthResult.token;
        }

        return "";
    }

    #endregion SeafileAuth

    #region SeafileChecks

    public bool CheckAPIURL()
    {
        var url = URLHelpers.FixPrefix(APIURL);
        url = URLHelpers.CombineURL(url, "ping/?format=json");

        SSLBypassHelper sslBypassHelper = null;

        try
        {
            if (IgnoreInvalidCert)
            {
                sslBypassHelper = new SSLBypassHelper();
            }

            string? response = SendRequest(HttpMethod.Get, url);

            if (!string.IsNullOrEmpty(response))
            {
                if (response == "\"pong\"")
                {
                    return true;
                }
            }

            return false;
        }
        finally
        {
            if (sslBypassHelper != null)
            {
                sslBypassHelper.Dispose();
            }
        }
    }

    public bool CheckAuthToken()
    {
        string? url = URLHelpers.FixPrefix(APIURL);
        url = URLHelpers.CombineURL(url, "auth/ping/?format=json");

        NameValueCollection headers = new NameValueCollection
        {
            { "Authorization", "Token " + AuthToken }
        };

        SSLBypassHelper sslBypassHelper = null;

        try
        {
            if (IgnoreInvalidCert)
            {
                sslBypassHelper = new SSLBypassHelper();
            }

            var response = SendRequest(HttpMethod.Get, url, null, headers);

            if (!string.IsNullOrEmpty(response))
            {
                if (response == "\"pong\"")
                {
                    return true;
                }
            }

            return false;
        }
        finally
        {
            if (sslBypassHelper != null)
            {
                sslBypassHelper.Dispose();
            }
        }
    }

    #endregion SeafileChecks

    #region SeafileAccountInformation

    [RequiresDynamicCode("Uploader")]
    [RequiresUnreferencedCode("Uploader")]
    public SeafileCheckAccInfoResponse GetAccountInfo()
    {
        var url = URLHelpers.FixPrefix(APIURL);
        url = URLHelpers.CombineURL(url, "account/info/?format=json");

        var headers = new NameValueCollection
        {
            { "Authorization", "Token " + AuthToken }
        };

        SSLBypassHelper sslBypassHelper = null;

        try
        {
            if (IgnoreInvalidCert)
            {
                sslBypassHelper = new SSLBypassHelper();
            }

            var response = SendRequest(HttpMethod.Get, url, null, headers);

            if (!string.IsNullOrEmpty(response))
            {
                var AccInfoResponse = JsonSerializer.Deserialize<SeafileCheckAccInfoResponse>(response, Options);

                return AccInfoResponse;
            }

            return null;
        }
        finally
        {
            if (sslBypassHelper != null)
            {
                sslBypassHelper.Dispose();
            }
        }
    }

    #endregion SeafileAccountInformation

    #region SeafileLibraries

    [RequiresUnreferencedCode("Uploader")]
    public string GetOrMakeDefaultLibrary(string authtoken = null)
    {
        string? url = URLHelpers.FixPrefix(APIURL);
        url = URLHelpers.CombineURL(url, "default-repo/?format=json");

        var headers = new NameValueCollection
        {
            { "Authorization", "Token " + (authtoken ?? AuthToken) }
        };

        SSLBypassHelper sslBypassHelper = null;

        try
        {
            if (IgnoreInvalidCert)
            {
                sslBypassHelper = new SSLBypassHelper();
            }

            string? response = SendRequest(HttpMethod.Get, url, null, headers);

            if (!string.IsNullOrEmpty(response))
            {
                var JsonResponse = JsonSerializer.Deserialize<SeafileDefaultLibraryObj>(response, Options);

                return JsonResponse.repo_id;
            }

            return null;
        }
        finally
        {
            if (sslBypassHelper != null)
            {
                sslBypassHelper.Dispose();
            }
        }
    }

    [RequiresUnreferencedCode("Uploader")]
    public List<SeafileLibraryObj> GetLibraries()
    {
        var url = URLHelpers.FixPrefix(APIURL);
        url = URLHelpers.CombineURL(url, "repos/?format=json");

        var headers = new NameValueCollection
        {
            { "Authorization", "Token " + AuthToken }
        };

        SSLBypassHelper sslBypassHelper = null;

        try
        {
            if (IgnoreInvalidCert)
            {
                sslBypassHelper = new SSLBypassHelper();
            }

            var response = SendRequest(HttpMethod.Get, url, null, headers);

            if (!string.IsNullOrEmpty(response))
            {
                var JsonResponse = JsonSerializer.Deserialize<List<SeafileLibraryObj>>(response, Options);

                return JsonResponse;
            }

            return null;
        }
        finally
        {
            if (sslBypassHelper != null)
            {
                sslBypassHelper.Dispose();
            }
        }
    }

    public bool ValidatePath(string path)
    {
        var url = URLHelpers.FixPrefix(APIURL);
        url = URLHelpers.CombineURL(url, "repos/" + RepoID + "/dir/?p=" + path + "&format=json");

        var headers = new NameValueCollection
        {
            { "Authorization", "Token " + AuthToken }
        };

        SSLBypassHelper sslBypassHelper = null;

        try
        {
            if (IgnoreInvalidCert)
            {
                sslBypassHelper = new SSLBypassHelper();
            }

            var response = SendRequest(HttpMethod.Get, url, null, headers);

            if (!string.IsNullOrEmpty(response))
            {
                return true;
            }

            return false;
        }
        finally
        {
            if (sslBypassHelper != null)
            {
                sslBypassHelper.Dispose();
            }
        }
    }

    #endregion SeafileLibraries

    #region SeafileEncryptedLibrary

    public bool DecryptLibrary(string libraryPassword)
    {
        var url = URLHelpers.FixPrefix(APIURL);
        url = URLHelpers.CombineURL(url, "repos/" + RepoID + "/?format=json");

        var headers = new NameValueCollection
        {
            { "Authorization", "Token " + AuthToken }
        };

        var args = new Dictionary<string, string?>
        {
            { "password", libraryPassword }
        };

        SSLBypassHelper sslBypassHelper = null;

        try
        {
            if (IgnoreInvalidCert)
            {
                sslBypassHelper = new SSLBypassHelper();
            }

            var response = SendRequestMultiPart(url, args, headers);

            if (!string.IsNullOrEmpty(response))
            {
                if (response == "\"success\"")
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }

            return false;
        }
        finally
        {
            if (sslBypassHelper != null)
            {
                sslBypassHelper.Dispose();
            }
        }
    }

    #endregion SeafileEncryptedLibrary

    #region SeafileUpload

    public override UploadResult Upload(Stream stream, string? fileName)
    {
        if (string.IsNullOrEmpty(APIURL))
        {
            throw new Exception("Seafile API URL is empty.");
        }

        if (string.IsNullOrEmpty(AuthToken))
        {
            throw new Exception("Seafile Authentication Token is empty.");
        }

        if (string.IsNullOrEmpty(Path))
        {
            Path = "/";
        }
        else
        {
            char pathLast = Path[Path.Length - 1];
            if (pathLast != '/')
            {
                Path += "/";
            }
        }

        var url = URLHelpers.FixPrefix(APIURL);
        url = URLHelpers.CombineURL(url, "repos/" + RepoID + "/upload-link/?format=json");

        var headers = new NameValueCollection
        {
            { "Authorization", "Token " + AuthToken }
        };

        SSLBypassHelper sslBypassHelper = null;

        try
        {
            if (IgnoreInvalidCert)
            {
                sslBypassHelper = new SSLBypassHelper();
            }

            var response = SendRequest(HttpMethod.Get, url, null, headers);

            var responseURL = response.Trim('"');

            var args = new Dictionary<string, string?>
            {
                { "filename", fileName },
                { "parent_dir", Path }
            };

            var result = SendRequestFile(responseURL, stream, fileName, "file", args, headers);

            if (!IsError)
            {
                if (CreateShareableURL && !IsLibraryEncrypted)
                {
                    AllowReportProgress = false;
                    result.URL = ShareFile(Path + fileName);

                    if (CreateShareableURLRaw)
                    {
                        var uriBuilder = new UriBuilder(result.URL);
                        var query = HttpUtility.ParseQueryString(uriBuilder.Query);
                        query["raw"] = "1";
                        uriBuilder.Query = query.ToString();
                        result.URL = $"{uriBuilder.Scheme}://{uriBuilder.Host}{uriBuilder.Path}{uriBuilder.Query}";
                    }
                }
                else
                {
                    result.IsURLExpected = false;
                }
            }

            return result;
        }
        finally
        {
            if (sslBypassHelper != null)
            {
                sslBypassHelper.Dispose();
            }
        }
    }

    public string? ShareFile(string path)
    {
        var url = URLHelpers.FixPrefix(APIURL);
        url = URLHelpers.CombineURL(url, "repos", RepoID, "file/shared-link/");

        var args = new Dictionary<string, string?>
        {
            { "p", path },
            { "share_type", "download" }
        };
        if (!string.IsNullOrEmpty(SharePassword)) args.Add("password", SharePassword);
        if (ShareDaysToExpire > 0) args.Add("expire", ShareDaysToExpire.ToString());

        var headers = new NameValueCollection
        {
            { "Authorization", "Token " + AuthToken }
        };

        SSLBypassHelper sslBypassHelper = null;

        try
        {
            if (IgnoreInvalidCert)
            {
                sslBypassHelper = new SSLBypassHelper();
            }

            SendRequestURLEncoded(HttpMethod.Put, url, args, headers);
            return LastResponseInfo.Headers["Location"].FirstOrDefault()!;
        }
        finally
        {
            if (sslBypassHelper != null)
            {
                sslBypassHelper.Dispose();
            }
        }
    }

    #endregion SeafileUpload
}

public class SeafileAuthResponse
{
    public string token { get; set; }
}

public class SeafileCheckAccInfoResponse
{
    public long usage { get; set; }
    public long total { get; set; }
    public string email { get; set; }
}

public class SeafileLibraryObj
{
    public string permission { get; set; }
    public bool encrypted { get; set; }
    public long mtime { get; set; }
    public string owner { get; set; }
    public string id { get; set; }
    public long size { get; set; }
    public string name { get; set; }
    public string type { get; set; }
    [JsonPropertyName("virtual")]
    public string _virtual { get; set; }
    public string desc { get; set; }
    public string root { get; set; }
}

public class SeafileDefaultLibraryObj
{
    public string repo_id { get; set; }
    public bool exists { get; set; }
}
