// SPDX-License-Identifier: GPL-3.0-or-later


using System.Xml.Linq;
using SnapX.Core.Upload.BaseUploaders;
using SnapX.Core.Upload.OAuth;
using SnapX.Core.Utils.Extensions;

namespace SnapX.Core.Upload.Img;

public sealed class TwitSnapsUploader : ImageUploader
{
    public OAuthInfo AuthInfo { get; set; }

    private const string? APIURL = "https://twitsnaps.com/dev/image/upload.xml";

    private string APIKey;

    public TwitSnapsUploader(string apiKey, OAuthInfo oauth)
    {
        APIKey = apiKey;
        AuthInfo = oauth;
    }

    public override UploadResult Upload(Stream stream, string? fileName)
    {
        throw new NotImplementedException("Twitsnaps upload not implemented");
        // using (TwitterTweetForm msgBox = new TwitterTweetForm())
        // {
        //     msgBox.ShowDialog();
        //     return Upload(stream, fileName, msgBox.Message);
        // }
    }

    private UploadResult Upload(Stream stream, string? fileName, string? msg)
    {
        var args = new Dictionary<string, string?>
        {
            { "appKey", APIKey },
            { "consumerKey", AuthInfo.ConsumerKey },
            { "consumerSecret", AuthInfo.ConsumerSecret },
            { "oauthToken", AuthInfo.UserToken },
            { "oauthSecret", AuthInfo.UserSecret }
        };

        if (!string.IsNullOrEmpty(msg))
        {
            args.Add("message", msg);
        }

        var result = SendRequestFile(APIURL, stream, fileName, "media", args);

        return ParseResult(result);
    }

    private UploadResult ParseResult(UploadResult result)
    {
        if (result.IsSuccess)
        {
            var xd = XDocument.Parse(result.Response);

            var xe = xd.Element("image");

            if (xe != null)
            {
                string? id = xe.GetElementValue("id");
                result.URL = "https://twitsnaps.com/snap/" + id;
                result.ThumbnailURL = "https://twitsnaps.com/thumb/" + id;
            }
            else
            {
                xe = xd.Element("error");

                if (xe != null)
                {
                    Errors.Add("Error: " + xe.GetElementValue("description"));
                }
            }
        }

        return result;
    }
}
