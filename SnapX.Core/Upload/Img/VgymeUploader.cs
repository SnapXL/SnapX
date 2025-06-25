
// SPDX-License-Identifier: GPL-3.0-or-later


using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization;
using SnapX.Core.Upload.BaseServices;
using SnapX.Core.Upload.BaseUploaders;
using SnapX.Core.Upload.Utils;

namespace SnapX.Core.Upload.Img;

public class VgymeImageUploaderService : ImageUploaderService
{
    public override ImageDestination EnumValue => ImageDestination.Vgyme;

    public override bool CheckConfig(UploadersConfig config) => true;

    public override GenericUploader CreateUploader(UploadersConfig config, TaskReferenceHelper taskInfo)
    {
        return new VgymeUploader()
        {
            UserKey = config.VgymeUserKey
        };
    }
}
[JsonSerializable(typeof(VgymeUploader.VgymeResponse))]
internal partial class VgymeContext : JsonSerializerContext;
public sealed class VgymeUploader : ImageUploader
{
    public string? UserKey { get; set; }

    [RequiresDynamicCode("Uploader")]
    [RequiresUnreferencedCode("Uploader")]
    public override UploadResult Upload(Stream stream, string? fileName)
    {
        Dictionary<string, string?> args = [];
        if (!string.IsNullOrEmpty(UserKey)) args.Add("userkey", UserKey);

        var result = SendRequestFile("https://vgy.me/upload", stream, fileName, "file", args);

        if (result.IsSuccess)
        {
            var options = new JsonSerializerOptions
            {
                TypeInfoResolver = VgymeContext.Default
            };
            var response = JsonSerializer.Deserialize<VgymeResponse>(result.Response, options);

            if (response != null && !response.Error)
            {
                result.URL = response.Image;
                result.DeletionURL = response.Delete;
            }
        }

        return result;
    }

    public class VgymeResponse
    {
        public bool Error { get; set; }
        public string URL { get; set; }
        public string? Image { get; set; }
        public long Size { get; set; }
        public string Filename { get; set; }
        public string Ext { get; set; }
        public string? Delete { get; set; }
    }
}
