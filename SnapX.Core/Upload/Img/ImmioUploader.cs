// SPDX-License-Identifier: GPL-3.0-or-later


using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization;
using SnapX.Core.Upload.BaseUploaders;

namespace SnapX.Core.Upload.Img;
[JsonSerializable(typeof(ImmioUploader.ImmioResponse))]
internal partial class ImmioContext : JsonSerializerContext;
public sealed class ImmioUploader : ImageUploader
{
    [RequiresDynamicCode("Uploader")]
    [RequiresUnreferencedCode("Uploader")]
    public override UploadResult Upload(Stream stream, string? fileName)
    {
        var result = SendRequestFile("https://imm.io/store/", stream, fileName, "image");
        if (!result.IsSuccess) return result;
        var options = new JsonSerializerOptions { TypeInfoResolver = ImmioContext.Default };
        var response = JsonSerializer.Deserialize<ImmioResponse>(result.Response, options);
        if (response != null) result.URL = response.Payload.Uri;
        return result;
    }

    public class ImmioResponse
    {
        public bool Success { get; set; }
        public ImmioPayload Payload { get; set; }
    }

    public class ImmioPayload
    {
        public string Uid { get; set; }
        public string? Uri { get; set; }
        public string Link { get; set; }
        public string Name { get; set; }
        public string Format { get; set; }
        public string Ext { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public string Size { get; set; }
    }
}

