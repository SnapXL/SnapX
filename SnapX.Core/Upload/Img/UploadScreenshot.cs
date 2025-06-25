
// SPDX-License-Identifier: GPL-3.0-or-later


using System.Xml.Linq;
using SnapX.Core.Upload.BaseUploaders;
using SnapX.Core.Utils.Extensions;

namespace SnapX.Core.Upload.Img;

public class UploadScreenshot : ImageUploader
{
    private string APIKey { get; set; }

    public UploadScreenshot(string key)
    {
        APIKey = key;
    }

    public override UploadResult Upload(Stream stream, string? fileName)
    {
        var arguments = new Dictionary<string, string?>
        {
            { "apiKey", APIKey },
            { "xmlOutput", "1" }
        };
        //arguments.Add("testMode", "1");

        var result = SendRequestFile("https://img1.uploadscreenshot.com/api-upload.php", stream, fileName, "userfile", arguments);

        return ParseResult(result);
    }

    private UploadResult ParseResult(UploadResult result)
    {
        if (result.IsSuccess)
        {
            XDocument xdoc = XDocument.Parse(result.Response);
            XElement xele = xdoc.Root.Element("upload");

            string? error = xele.GetElementValue("errorCode");
            if (!string.IsNullOrEmpty(error))
            {
                string? errorMessage;

                switch (error)
                {
                    case "1":
                        errorMessage = "The MD5 sum that you provided did not match the MD5 sum that we calculated for the uploaded image file." +
                                       " There may of been a network interruption during upload. Suggest that you try the upload again.";
                        break;
                    case "2":
                        errorMessage = "The apiKey that you provided does not exist or has been banned. Please contact us for more information.";
                        break;
                    case "3":
                        errorMessage = "The file that you provided was not a png or jpg.";
                        break;
                    case "4":
                        errorMessage = "The file that you provided was too large, currently the limit per file is 50MB.";
                        break;
                    case "99":
                    default:
                        errorMessage = "An unkown error occured, please contact the admin and include a copy of the file that you were trying to upload.";
                        break;
                }

                Errors.Add(errorMessage);
            }
            else
            {
                result.URL = xele.GetElementValue("original");
                result.ThumbnailURL = xele.GetElementValue("small");
                result.DeletionURL = xele.GetElementValue("deleteurl");
            }
        }

        return result;
    }
}
