// SPDX-License-Identifier: GPL-3.0-or-later


using System.Xml.Linq;
using SnapX.Core.Upload.BaseServices;
using SnapX.Core.Upload.BaseUploaders;
using SnapX.Core.Upload.OAuth;
using SnapX.Core.Upload.Utils;
using SnapX.Core.Utils.Extensions;

namespace SnapX.Core.Upload.Img;

public class PhotobucketImageUploaderService : ImageUploaderService
{
    public override ImageDestination EnumValue => ImageDestination.Photobucket;

    public override bool CheckConfig(UploadersConfig config)
    {
        return config.PhotobucketAccountInfo != null && OAuthInfo.CheckOAuth(config.PhotobucketOAuthInfo);
    }

    public override GenericUploader CreateUploader(UploadersConfig config, TaskReferenceHelper taskInfo)
    {
        return new Photobucket(config.PhotobucketOAuthInfo, config.PhotobucketAccountInfo);
    }
}

public sealed class Photobucket : ImageUploader, IOAuth
{
    private const string? URLRequestToken = "https://api.photobucket.com/login/request";
    private const string URLAuthorize = "https://photobucket.com/apilogin/login";
    private const string? URLAccessToken = "https://api.photobucket.com/login/access";

    public OAuthInfo AuthInfo { get; set; }
    public PhotobucketAccountInfo AccountInfo { get; set; }

    public Photobucket(OAuthInfo oauth)
    {
        AuthInfo = oauth;
        AccountInfo = new PhotobucketAccountInfo();
    }

    public Photobucket(OAuthInfo oauth, PhotobucketAccountInfo accountInfo)
    {
        AuthInfo = oauth;
        AccountInfo = accountInfo;
    }

    public string? GetAuthorizationURL()
    {
        return GetAuthorizationURL(URLRequestToken, URLAuthorize, AuthInfo, null, HttpMethod.Post);
    }

    public bool GetAccessToken(string? verificationCode)
    {
        AuthInfo.AuthVerifier = verificationCode;

        var nv = GetAccessTokenEx(URLAccessToken, AuthInfo, HttpMethod.Post);

        if (nv != null)
        {
            AccountInfo.Subdomain = nv["subdomain"];
            AccountInfo.AlbumID = nv["username"];
            return !string.IsNullOrEmpty(AccountInfo.Subdomain);
        }

        return false;
    }

    public PhotobucketAccountInfo GetAccountInfo()
    {
        return AccountInfo;
    }

    public override UploadResult Upload(Stream stream, string? fileName)
    {
        return UploadMedia(stream, fileName, AccountInfo.ActiveAlbumPath);
    }

    public UploadResult UploadMedia(Stream stream, string? fileName, string albumID)
    {
        var args = new Dictionary<string, string?>
        {
            { "id", albumID }, // Album identifier.
            { "type", "image" } // Media type. Options are image, video, or base64.
        };

        var url = "https://api.photobucket.com/album/!/upload";
        var query = OAuthManager.GenerateQuery(url, args, HttpMethod.Post, AuthInfo);
        query = FixURL(query);

        var result = SendRequestFile(query, stream, fileName, "uploadfile");

        if (!result.IsSuccess) return result;

        var xd = XDocument.Parse(result.Response);
        var xe = xd.GetNode("response/content");

        if (xe == null) return result;

        result.URL = xe.GetElementValue("url");
        result.ThumbnailURL = xe.GetElementValue("thumb");

        return result;
    }

    public bool CreateAlbum(string albumID, string? albumName)
    {
        var args = new Dictionary<string, string?>
        {
            { "id", albumID }, // Album identifier.
            { "name", albumName } // Name of result. Must be between 2 and 50 characters.
        };

        var url = "https://api.photobucket.com/album/!";
        var query = OAuthManager.GenerateQuery(url, args, HttpMethod.Post, AuthInfo);
        query = FixURL(query);

        var response = SendRequestMultiPart(query, args);

        if (string.IsNullOrEmpty(response)) return false;

        var xd = XDocument.Parse(response);
        var xe = xd.GetNode("response");

        if (xe == null) return false;

        string? status = xe.GetElementValue("status");

        return status == "OK";
    }

    private string? FixURL(string? url) => url.Replace("api.photobucket.com", AccountInfo.Subdomain);

}

public class PhotobucketAccountInfo
{
    public string Subdomain { get; set; }

    public string AlbumID { get; set; }

    public List<string> AlbumList = [];
    public int ActiveAlbumID = 0;

    public string ActiveAlbumPath
    {
        get
        {
            return AlbumList[ActiveAlbumID];
        }
    }
}

