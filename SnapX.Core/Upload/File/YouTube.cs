
// SPDX-License-Identifier: GPL-3.0-or-later


using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization;
using SnapX.Core.Upload.BaseServices;
using SnapX.Core.Upload.BaseUploaders;
using SnapX.Core.Upload.OAuth;
using SnapX.Core.Upload.Utils;

namespace SnapX.Core.Upload.File;

public class YouTubeFileUploaderService : FileUploaderService
{
    public override FileDestination EnumValue { get; } = FileDestination.YouTube;

    public override bool CheckConfig(UploadersConfig config)
    {
        return OAuth2Info.CheckOAuth(config.YouTubeOAuth2Info);
    }

    public override GenericUploader CreateUploader(UploadersConfig config, TaskReferenceHelper taskInfo)
    {
        return new YouTube(config.YouTubeOAuth2Info)
        {
            PrivacyType = config.YouTubePrivacyType,
            UseShortenedLink = config.YouTubeUseShortenedLink,
            ShowDialog = config.YouTubeShowDialog
        };
    }
}
[JsonSerializable(typeof(YouTubeVideoResponse))]
internal partial class YouTubeContext : JsonSerializerContext;
public sealed class YouTube : FileUploader, IOAuth2
{
    public GoogleOAuth2 OAuth2 { get; private set; }
    public OAuth2Info AuthInfo => OAuth2.AuthInfo;
    public YouTubeVideoPrivacy PrivacyType { get; set; }
    public bool UseShortenedLink { get; set; }
    public bool ShowDialog { get; set; }

    public YouTube(OAuth2Info oauth)
    {
        OAuth2 = new GoogleOAuth2(oauth, this)
        {
            Scope = "https://www.googleapis.com/auth/youtube.upload https://www.googleapis.com/auth/userinfo.profile"
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

    [RequiresDynamicCode("Uploader")]
    [RequiresUnreferencedCode("Uploader")]
    public override UploadResult Upload(Stream stream, string? fileName)
    {
        if (!CheckAuthorization()) return null;

        var title = Path.GetFileNameWithoutExtension(fileName);
        var description = "";
        var visibility = PrivacyType;

        if (ShowDialog)
        {
            // TODO: Reimplement YouTube ShowDialog
            // using (YouTubeVideoOptionsForm form = new YouTubeVideoOptionsForm(title, description, visibility))
            // {
            //     if (form.ShowDialog() == DialogResult.OK)
            //     {
            //         title = form.Title;
            //         description = form.Description;
            //         visibility = form.Visibility;
            //     }
            //     else
            //     {
            //         return null;
            //     }
            // }
        }

        var uploadVideo = new YouTubeVideoUpload()
        {
            snippet = new YouTubeVideoSnippet()
            {
                title = title,
                description = description
            },
            status = new YouTubeVideoStatusUpload()
            {
                privacyStatus = visibility
            }
        };

        var metadata = JsonSerializer.Serialize(uploadVideo);

        var result = SendRequestFile("https://www.googleapis.com/upload/youtube/v3/videos?part=id,snippet,status", stream, fileName, "file",
            headers: OAuth2.GetAuthHeaders(), relatedData: metadata);

        if (!string.IsNullOrEmpty(result.Response))
        {
            var responseVideo = JsonSerializer.Deserialize<YouTubeVideoResponse>(result.Response, new JsonSerializerOptions
            {
                TypeInfoResolver = YouTubeContext.Default
            });

            if (responseVideo != null)
            {
                if (UseShortenedLink)
                {
                    result.URL = $"https://youtu.be/{responseVideo.id}";
                }
                else
                {
                    result.URL = $"https://www.youtube.com/watch?v={responseVideo.id}";
                }

                switch (responseVideo.status.uploadStatus)
                {
                    case YouTubeVideoStatus.UploadFailed:
                        Errors.Add("Upload failed: " + responseVideo.status.failureReason);
                        break;
                    case YouTubeVideoStatus.UploadRejected:
                        Errors.Add("Upload rejected: " + responseVideo.status.rejectionReason);
                        break;
                }
            }
        }

        return result;
    }
}

public class YouTubeVideoUpload
{
    public YouTubeVideoSnippet snippet { get; set; }
    public YouTubeVideoStatusUpload status { get; set; }
}

public class YouTubeVideoResponse
{
    public string id { get; set; }
    public YouTubeVideoSnippet snippet { get; set; }
    public YouTubeVideoStatus status { get; set; }
}

public class YouTubeVideoSnippet
{
    public string title { get; set; }
    public string description { get; set; }
    public string[] tags { get; set; }
}

public class YouTubeVideoStatus
{
    public const string UploadFailed = "failed";
    public const string UploadRejected = "rejected";

    public YouTubeVideoPrivacy privacyStatus { get; set; }
    public string uploadStatus { get; set; }
    public string failureReason { get; set; }
    public string rejectionReason { get; set; }
}

public class YouTubeVideoStatusUpload
{
    public YouTubeVideoPrivacy privacyStatus { get; set; }
}
