// SPDX-License-Identifier: GPL-3.0-or-later


using System.Text.Json;
using System.Text.Json.Serialization;
using SnapX.Core.Upload.BaseServices;
using SnapX.Core.Upload.BaseUploaders;
using SnapX.Core.Upload.Utils;
using SnapX.Core.Utils;

namespace SnapX.Core.Upload.Img;

public class CheveretoImageUploaderService : ImageUploaderService
{
    public override ImageDestination EnumValue => ImageDestination.Chevereto;
    public override bool CheckConfig(UploadersConfig config)
    {
        return config.CheveretoUploader != null && !string.IsNullOrEmpty(config.CheveretoUploader.UploadURL) &&
            !string.IsNullOrEmpty(config.CheveretoUploader.APIKey);
    }

    public override GenericUploader CreateUploader(UploadersConfig config, TaskReferenceHelper taskInfo)
    {
        return new Chevereto(config.CheveretoUploader)
        {
            DirectURL = config.CheveretoDirectURL
        };
    }
}
[JsonSerializable(typeof(Chevereto.CheveretoResponse))]
[JsonSerializable(typeof(Chevereto.CheveretoImage))]
[JsonSerializable(typeof(Chevereto.CheveretoThumb))]
internal partial class CheveretoContext : JsonSerializerContext;
public sealed class Chevereto(CheveretoUploader? Uploader) : ImageUploader
{
    public CheveretoUploader? Uploader { get; private set; } = Uploader;

    public bool DirectURL { get; set; }

    public override UploadResult Upload(Stream stream, string? fileName)
    {
        var args = new Dictionary<string, string?>
        {
            { "key", Uploader?.APIKey },
            { "format", "json" }
        };

        var url = URLHelpers.FixPrefix(Uploader?.UploadURL);

        var result = SendRequestFile(url, stream, fileName, "source", args);

        if (!result.IsSuccess)
        {
            return result;
        }

        var options = new JsonSerializerOptions
        {
            TypeInfoResolver = CheveretoContext.Default
        };
        var response = JsonSerializer.Deserialize<CheveretoResponse>(result.Response!, options);

        if (response?.Image == null)
        {
            return result;
        }

        result.URL = DirectURL ? response.Image.URL : response.Image.URL_Viewer;

        if (response.Image.Thumb?.URL != null)
        {
            result.ThumbnailURL = response.Image.Thumb.URL;
        }

        return result;
    }


    public class CheveretoResponse
    {
        public CheveretoImage? Image { get; set; }
    }

    public class CheveretoImage
    {
        public string? URL { get; set; }
        public string? URL_Viewer { get; set; }
        public CheveretoThumb? Thumb { get; set; }
    }

    public class CheveretoThumb
    {
        public string? URL { get; set; }
    }
}

