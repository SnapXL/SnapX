// SPDX-License-Identifier: GPL-3.0-or-later


using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization;
using SnapX.Core.Upload.BaseServices;
using SnapX.Core.Upload.BaseUploaders;
using SnapX.Core.Upload.OAuth;
using SnapX.Core.Upload.Utils;
using SnapX.Core.Utils.Extensions;

namespace SnapX.Core.Upload.Img;

public class TwitterImageUploaderService : ImageUploaderService
{
    public override ImageDestination EnumValue => ImageDestination.Twitter;

    public override bool CheckConfig(UploadersConfig config)
    {
        return config.TwitterOAuthInfoList != null && config.TwitterOAuthInfoList.IsValidIndex(config.TwitterSelectedAccount) &&
            OAuthInfo.CheckOAuth(config.TwitterOAuthInfoList[config.TwitterSelectedAccount]);
    }

    public override GenericUploader CreateUploader(UploadersConfig config, TaskReferenceHelper taskInfo)
    {
        OAuthInfo twitterOAuth = config.TwitterOAuthInfoList.ReturnIfValidIndex(config.TwitterSelectedAccount);

        return new Twitter(twitterOAuth)
        {
            SkipMessageBox = config.TwitterSkipMessageBox,
            DefaultMessage = config.TwitterDefaultMessage ?? ""
        };
    }
}
[JsonSerializable(typeof(TwitterStatusResponse))]
internal partial class TwitterContext : JsonSerializerContext;
public class Twitter : ImageUploader, IOAuth
{
    private const string APIVersion = "1.1";
    private const int characters_reserved_per_media = 23;

    public const int MessageLimit = 280;
    public const int MessageMediaLimit = MessageLimit - characters_reserved_per_media;

    public OAuthInfo AuthInfo { get; set; }
    public bool SkipMessageBox { get; set; }
    public string DefaultMessage { get; set; }

    public Twitter(OAuthInfo oauth)
    {
        AuthInfo = oauth;
    }

    public string? GetAuthorizationURL()
    {
        return GetAuthorizationURL("https://api.twitter.com/oauth/request_token", "https://api.twitter.com/oauth/authorize", AuthInfo);
    }

    public bool GetAccessToken(string? verificationCode)
    {
        AuthInfo.AuthVerifier = verificationCode;
        return GetAccessToken("https://api.twitter.com/oauth/access_token", AuthInfo);
    }

    [UnconditionalSuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code", Justification = "<Pending>")]
    public override UploadResult Upload(Stream stream, string? fileName)
    {
        string message = DefaultMessage;

        if (!SkipMessageBox)
        {
            throw new NotImplementedException("Twitter Upload is not implemented yet.");
            // using (TwitterTweetForm twitterMsg = new TwitterTweetForm())
            // {
            //     twitterMsg.MediaMode = true;
            //     twitterMsg.Message = DefaultMessage;
            //
            //     if (twitterMsg.ShowDialog() != DialogResult.OK)
            //     {
            //         return new UploadResult() { IsURLExpected = false };
            //     }
            //
            //     message = twitterMsg.Message;
            // }
        }

        return TweetMessageWithMedia(message, stream, fileName);
    }

    [RequiresDynamicCode("Uploader")]
    [RequiresUnreferencedCode("Uploader")]
    public TwitterStatusResponse TweetMessage(string? message)
    {
        if (message.Length > MessageLimit)
            message = message.Remove(MessageLimit);

        var url = $"https://api.twitter.com/{APIVersion}/statuses/update.json";
        var query = OAuthManager.GenerateQuery(url, null, HttpMethod.Post, AuthInfo);

        var args = new Dictionary<string, string?>
        {
            { "status", message }
        };

        var response = SendRequestMultiPart(query, args);
        var options = new JsonSerializerOptions
        {
            TypeInfoResolver = TwitterContext.Default
        };
        if (string.IsNullOrEmpty(response))
            return null;

        return JsonSerializer.Deserialize<TwitterStatusResponse>(response, options);
    }


    [RequiresDynamicCode("Uploader")]
    [RequiresUnreferencedCode("Uploader")]
    public UploadResult TweetMessageWithMedia(string message, Stream stream, string? fileName)
    {
        if (message.Length > MessageMediaLimit)
            message = message.Remove(MessageMediaLimit);

        var url = $"https://api.twitter.com/{APIVersion}/statuses/update_with_media.json";
        var query = OAuthManager.GenerateQuery(url, null, HttpMethod.Post, AuthInfo);

        var args = new Dictionary<string, string?>
        {
            { "status", message }
        };

        var result = SendRequestFile(query, stream, fileName, "media[]", args);
        var options = new JsonSerializerOptions
        {
            TypeInfoResolver = TwitterContext.Default
        };
        if (string.IsNullOrEmpty(result.Response))
            return result;

        var status = JsonSerializer.Deserialize<TwitterStatusResponse>(result.Response, options);

        if (status?.user != null)
            result.URL = status.GetTweetURL();

        return result;
    }

    private string? GetConfiguration()
    {
        string? url = string.Format("https://api.twitter.com/{0}/help/configuration.json", APIVersion);
        string? query = OAuthManager.GenerateQuery(url, null, HttpMethod.Get, AuthInfo);
        string? response = SendRequest(HttpMethod.Get, query);
        return response;
    }
}

public class TwitterStatusResponse
{
    public long id { get; set; }
    public string text { get; set; }
    public string in_reply_to_screen_name { get; set; }
    public TwitterStatusUser user { get; set; }

    public string? GetTweetURL()
    {
        return string.Format("https://twitter.com/{0}/status/{1}", user.screen_name, id);
    }
}

public class TwitterStatusUser
{
    public long id { get; set; }
    public string name { get; set; }
    public string screen_name { get; set; }
}
