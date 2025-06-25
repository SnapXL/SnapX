
// SPDX-License-Identifier: GPL-3.0-or-later


using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using SnapX.Core.Upload.BaseUploaders;
using SnapX.Core.Upload.OAuth;

namespace SnapX.Core.Upload.Img;

public enum TwitPicUploadType
{
    [Description("Upload Image")]
    UPLOAD_IMAGE_ONLY,
    [Description("Upload Image and update Twitter Status")]
    UPLOAD_IMAGE_AND_TWITTER
}

public enum TwitPicThumbnailType
{
    [Description("Mini Thumbnail")]
    Mini,
    [Description("Normal Thumbnail")]
    Thumb
}

public sealed class TwitPicUploader : ImageUploader
{
    public string APIKey { get; private set; }
    public OAuthInfo AuthInfo { get; private set; }

    public TwitPicUploadType TwitPicUploadType { get; set; }
    public bool ShowFull { get; set; }
    public TwitPicThumbnailType TwitPicThumbnailMode { get; set; }

    private const string? UploadLink = "https://api.twitpic.com/1/upload.json";
    private const string UploadAndPostLink = "https://api.twitpic.com/1/uploadAndPost.json";

    public TwitPicUploader(string key, OAuthInfo oauth)
    {
        APIKey = key;
        AuthInfo = oauth;
        TwitPicUploadType = TwitPicUploadType.UPLOAD_IMAGE_ONLY;
        ShowFull = false;
        TwitPicThumbnailMode = TwitPicThumbnailType.Thumb;
    }

    [UnconditionalSuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code", Justification = "<Pending>")]
    public override UploadResult Upload(Stream stream, string? fileName)
    {
        return TwitPicUploadType switch
        {
            TwitPicUploadType.UPLOAD_IMAGE_ONLY => Upload(stream, fileName, UploadLink),
            TwitPicUploadType.UPLOAD_IMAGE_AND_TWITTER => throw new NotImplementedException("TwitPicUploadType.UPLOAD_IMAGE_AND_TWITTER is not implemented yet."),
        };
    }

    [RequiresDynamicCode("Calls System.Text.Json.JsonSerializer.Deserialize<TValue>(String, JsonSerializerOptions)")]
    [RequiresUnreferencedCode("Calls System.Text.Json.JsonSerializer.Deserialize<TValue>(String, JsonSerializerOptions)")]
    private UploadResult Upload(Stream stream, string? fileName, string? url, string? msg = "")
    {
        if (AuthInfo == null || string.IsNullOrEmpty(AuthInfo.UserToken) || string.IsNullOrEmpty(AuthInfo.UserSecret))
        {
            Errors.Add("Login is required.");
            return null;
        }

        Dictionary<string, string?> args = new Dictionary<string, string?>
        {
            { "key", APIKey },
            { "consumer_token", AuthInfo.ConsumerKey },
            { "consumer_secret", AuthInfo.ConsumerSecret },
            { "oauth_token", AuthInfo.UserToken },
            { "oauth_secret", AuthInfo.UserSecret },
            { "message", msg }
        };

        UploadResult result = SendRequestFile(url, stream, fileName, "media", args);

        TwitPicResponse response = JsonSerializer.Deserialize<TwitPicResponse>(result.Response);

        if (response != null)
        {
            result.URL = response.URL;
            if (ShowFull) result.URL += "/full";
            result.ThumbnailURL = string.Format("https://twitpic.com/show/{0}/{1}.{2}", TwitPicThumbnailMode.ToString().ToLowerInvariant(), response.ID, response.Type);
        }

        return result;
    }

    public class TwitPicResponse
    {
        public string ID { get; set; }
        public string Text { get; set; }
        public string? URL { get; set; }
        public string Width { get; set; }
        public string Height { get; set; }
        public string Size { get; set; }
        public string Type { get; set; }
        public string Timestamp { get; set; }

        public class User
        {
            public string ID { get; set; }
            public string Screen_Name { get; set; }
        }
    }
}
