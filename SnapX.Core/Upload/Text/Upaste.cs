
// SPDX-License-Identifier: GPL-3.0-or-later


using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization;
using SnapX.Core.Upload.BaseServices;
using SnapX.Core.Upload.BaseUploaders;
using SnapX.Core.Upload.Utils;

namespace SnapX.Core.Upload.Text;

public class UpasteTextUploaderService : TextUploaderService
{
    public override TextDestination EnumValue => TextDestination.Upaste;
    public override bool CheckConfig(UploadersConfig config) => true;

    public override GenericUploader CreateUploader(UploadersConfig config, TaskReferenceHelper taskInfo)
    {
        return new Upaste(config.UpasteUserKey)
        {
            IsPublic = config.UpasteIsPublic
        };
    }
}
[JsonSerializable(typeof(Upaste.UpasteResponse))]
internal partial class UpasteContext : JsonSerializerContext;
public sealed class Upaste : TextUploader
{
    private const string? APIURL = "https://upaste.me/api";

    public string? UserKey { get; private set; }
    public bool IsPublic { get; set; }

    public Upaste(string? userKey)
    {
        UserKey = userKey;
    }

    [RequiresDynamicCode("Uploader")]
    [RequiresUnreferencedCode("Uploader")]
    public override UploadResult UploadText(string? text, string? fileName)
    {
        var ur = new UploadResult();

        if (string.IsNullOrEmpty(text))
            return ur;

        var arguments = new Dictionary<string, string?>
        {
            { "paste", text },
            { "privacy", IsPublic ? "0" : "1" },
            { "expire", "0" },
            { "json", "true" }
        };

        if (!string.IsNullOrEmpty(UserKey))
        {
            arguments.Add("api_key", UserKey);
        }

        ur.Response = SendRequestMultiPart(APIURL, arguments);

        if (string.IsNullOrEmpty(ur.Response))
            return ur;

        var options = new JsonSerializerOptions()
        {
            TypeInfoResolver = UpasteContext.Default
        };
        var response = JsonSerializer.Deserialize<UpasteResponse>(ur.Response, options);

        if (response?.status?.Equals("success", StringComparison.OrdinalIgnoreCase) == true)
        {
            ur.URL = response.paste.link;
        }
        else
        {
            Errors.Add(response?.error);
        }

        return ur;
    }


    public class UpastePaste
    {
        public string id { get; set; }
        public string? link { get; set; }
        public string raw { get; set; }
        public string download { get; set; }
    }

    public class UpasteResponse
    {
        public UpastePaste paste { get; set; }
        public int errorcode { get; set; }
        public string? error { get; set; }
        public string status { get; set; }
    }
}

