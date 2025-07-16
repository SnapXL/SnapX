// SPDX-License-Identifier: GPL-3.0-or-later


using System.Collections.Specialized;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization;
using SnapX.Core.Upload.BaseServices;
using SnapX.Core.Upload.BaseUploaders;
using SnapX.Core.Upload.Utils;
using SnapX.Core.Utils;

namespace SnapX.Core.Upload.Text;

public class OneTimeSecretTextUploaderService : TextUploaderService
{
    public override TextDestination EnumValue => TextDestination.OneTimeSecret;

    public override bool CheckConfig(UploadersConfig config) => true;

    public override GenericUploader CreateUploader(UploadersConfig config, TaskReferenceHelper taskInfo)
    {
        return new OneTimeSecret()
        {
            API_KEY = config.OneTimeSecretAPIKey,
            API_USERNAME = config.OneTimeSecretAPIUsername
        };
    }
}
[JsonSerializable(typeof(OneTimeSecret.OneTimeSecretResponse))]
internal partial class OneTimeContext : JsonSerializerContext;
public sealed class OneTimeSecret : TextUploader
{
    private const string? API_ENDPOINT = "https://onetimesecret.com/api/v1/share";

    public string API_KEY { get; set; }
    public string API_USERNAME { get; set; }

    [RequiresDynamicCode("Uploader")]
    [RequiresUnreferencedCode("Uploader")]
    public override UploadResult UploadText(string? text, string? fileName)
    {
        var ur = new UploadResult();
        if (string.IsNullOrWhiteSpace(text) || string.IsNullOrWhiteSpace(fileName)) return ur;

        var args = new Dictionary<string, string?>() { { "text", text } };

        NameValueCollection headers = null;

        if (!string.IsNullOrEmpty(API_USERNAME) && !string.IsNullOrEmpty(API_KEY))
        {
            headers = RequestHelpers.CreateAuthenticationHeader(API_USERNAME, API_KEY);
        }

        ur.Response = SendRequestMultiPart(API_ENDPOINT, args, headers);
        if (string.IsNullOrEmpty(ur.Response)) return ur;
        var options = new JsonSerializerOptions()
        {
            TypeInfoResolver = OneTimeContext.Default
        };
        var jsonResponse = JsonSerializer.Deserialize<OneTimeSecretResponse>(ur.Response, options);

        if (jsonResponse != null)
        {
            ur.URL = URLHelpers.CombineURL("https://onetimesecret.com/secret/", jsonResponse.secret_key);
        }

        return ur;
    }

    public class OneTimeSecretResponse
    {
        public string custid { get; set; }
        public string metadata_key { get; set; }
        public string? secret_key { get; set; }
        public string ttl { get; set; }
        public string updated { get; set; }
        public string created { get; set; }
    }
}

