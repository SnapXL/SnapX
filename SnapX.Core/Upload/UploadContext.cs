using System.Text.Json.Serialization;
using SnapX.Core.Upload.Img;

namespace SnapX.Core.Upload;

[JsonSourceGenerationOptions(WriteIndented = true)]
[JsonSerializable(typeof(ImgurResponse))]
[JsonSerializable(typeof(ImgurError))]

internal partial class UploadContext : JsonSerializerContext
{
}
