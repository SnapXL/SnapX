
// SPDX-License-Identifier: GPL-3.0-or-later


using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization;
using SnapX.Core.Upload.BaseServices;
using SnapX.Core.Upload.BaseUploaders;
using SnapX.Core.Upload.Utils;

namespace SnapX.Core.Upload.File;

public class HostrFileUploaderService : FileUploaderService
{
    public override FileDestination EnumValue { get; } = FileDestination.Localhostr;

    public override bool CheckConfig(UploadersConfig config)
    {
        return !string.IsNullOrEmpty(config.LocalhostrEmail) && !string.IsNullOrEmpty(config.LocalhostrPassword);
    }

    public override GenericUploader CreateUploader(UploadersConfig config, TaskReferenceHelper taskInfo)
    {
        return new Hostr(config.LocalhostrEmail, config.LocalhostrPassword)
        {
            DirectURL = config.LocalhostrDirectURL
        };
    }
}
[JsonSerializable(typeof(Hostr.HostrFileUploadResponse))]
internal partial class HostrContext : JsonSerializerContext;
public sealed class Hostr : FileUploader
{
    public string Email { get; set; }
    public string Password { get; set; }
    public bool DirectURL { get; set; }

    public Hostr(string email, string password)
    {
        Email = email;
        Password = password;
    }

    [RequiresDynamicCode("Uploader")]
    [RequiresUnreferencedCode("Uploader")]
    public override UploadResult Upload(Stream stream, string? fileName)
    {
        UploadResult result = null;

        if (!string.IsNullOrEmpty(Email) && !string.IsNullOrEmpty(Password))
        {
            var headers = RequestHelpers.CreateAuthenticationHeader(Email, Password);
            result = SendRequestFile("https://api.hostr.co/file", stream, fileName, "file", headers: headers);

            if (result.IsSuccess)
            {
                var response = JsonSerializer.Deserialize<HostrFileUploadResponse>(result.Response, new JsonSerializerOptions
                {
                    TypeInfoResolver = HostrContext.Default
                });

                if (response != null)
                {
                    if (DirectURL && response.direct != null)
                    {
                        result.URL = string.Format("https://hostr.co/file/{0}/{1}", response.id, response.name);
                        result.ThumbnailURL = response.direct.direct_150x;
                    }
                    else
                    {
                        result.URL = response.href;
                    }
                }
            }
        }

        return result;
    }

    public class HostrFileUploadResponse
    {
        public string added { get; set; }
        public string name { get; set; }
        public string? href { get; set; }
        public int size { get; set; }
        public string type { get; set; }
        public HostrFileUploadResponseDirect direct { get; set; }
        public string id { get; set; }
    }

    public class HostrFileUploadResponseDirect
    {
        [JsonPropertyName("150x")]
        public string? direct_150x { get; set; }

        [JsonPropertyName("150x")]
        public string direct_930x { get; set; }
    }
}
