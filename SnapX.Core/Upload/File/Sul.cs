// SPDX-License-Identifier: GPL-3.0-or-later


using System.Text.Json.Nodes;
using FluentFTP.Helpers;
using SnapX.Core.Upload.BaseServices;
using SnapX.Core.Upload.BaseUploaders;
using SnapX.Core.Upload.Utils;
using SnapX.Core.Utils;
using SnapX.Core.Utils.Extensions;

namespace SnapX.Core.Upload.File;
public class SulFileUploaderService : FileUploaderService
{
    public override FileDestination EnumValue { get; } = FileDestination.Sul;


    public override bool CheckConfig(UploadersConfig config)
    {
        return !string.IsNullOrEmpty(config.SulAPIKey);
    }

    public override GenericUploader CreateUploader(UploadersConfig config, TaskReferenceHelper taskInfo)
    {
        return new SulUploader(config.SulAPIKey);
    }
}

public sealed class SulUploader : FileUploader
{
    private string APIKey { get; set; }

    public SulUploader(string apiKey)
    {
        APIKey = apiKey;
    }

    public override UploadResult Upload(Stream stream, string? fileName)
    {
        var args = new Dictionary<string, string?>
        {
            { "wizard", "true" },
            { "key", APIKey },
            { "client", "sharex-native" }
        };

        string? url = "https://s-ul.eu";
        string? upload_url = URLHelpers.CombineURL(url, "api/v1/upload");

        UploadResult result = SendRequestFile(upload_url, stream, fileName, "file", args);

        if (result.IsSuccess)
        {
            var jsonResponse = JsonNode.Parse(result.Response);

            string protocol = "";
            string domain = "";
            string file = "";
            string extension = "";
            string? error = "";

            if (jsonResponse != null)
            {
                protocol = jsonResponse.SelectToken("protocol").ObjectToString();
                domain = jsonResponse.SelectToken("domain").ObjectToString();
                file = jsonResponse.SelectToken("filename").ObjectToString();
                extension = jsonResponse.SelectToken("extension").ObjectToString();
                error = jsonResponse.SelectToken("error").ObjectToString();
            }

            if (!string.IsNullOrEmpty(error) || string.IsNullOrEmpty(protocol))
            {
                if (string.IsNullOrEmpty(error))
                {
                    Errors.Add("Generic error occurred, please contact support@s-ul.eu");
                }
                else
                {
                    Errors.Add(error);
                }
            }
            else
            {
                result.URL = protocol + domain + "/" + file + extension;
                result.DeletionURL = URLHelpers.CombineURL(url, "delete.php?key=" + APIKey + "&file=" + file);
            }
        }

        return result;
    }
}

