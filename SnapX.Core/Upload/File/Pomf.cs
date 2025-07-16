// SPDX-License-Identifier: GPL-3.0-or-later


using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization;
using SnapX.Core.Upload.BaseServices;
using SnapX.Core.Upload.BaseUploaders;
using SnapX.Core.Upload.Utils;
using SnapX.Core.Utils;

namespace SnapX.Core.Upload.File;

public class PomfFileUploaderService : FileUploaderService
{
    public override FileDestination EnumValue { get; } = FileDestination.Pomf;

    public override bool CheckConfig(UploadersConfig config)
    {
        return config.PomfUploader != null && !string.IsNullOrEmpty(config.PomfUploader.UploadURL);
    }

    public override GenericUploader CreateUploader(UploadersConfig config, TaskReferenceHelper taskInfo)
    {
        return new Pomf(config.PomfUploader);
    }
}

[JsonSerializable(typeof(Pomf.PomfResponse))]
internal partial class PomfContext : JsonSerializerContext;
public class Pomf : FileUploader
{
    public PomfUploader Uploader { get; private set; }

    public Pomf(PomfUploader uploader)
    {
        Uploader = uploader;
    }

    [RequiresDynamicCode("Uploader")]
    [RequiresUnreferencedCode("Uploader")]
    public override UploadResult Upload(Stream stream, string? fileName)
    {
        var result = SendRequestFile(Uploader.UploadURL, stream, fileName, "files[]");

        if (result.IsSuccess)
        {
            var response = JsonSerializer.Deserialize<PomfResponse>(result.Response, new JsonSerializerOptions
            {
                TypeInfoResolver = PomfContext.Default
            });

            if (response.success && response.files != null && response.files.Count > 0)
            {
                var url = response.files[0].url;

                if (!URLHelpers.HasPrefix(url) && !string.IsNullOrEmpty(Uploader.ResultURL))
                {
                    var resultURL = URLHelpers.FixPrefix(Uploader.ResultURL);
                    url = URLHelpers.CombineURL(resultURL, url);
                }

                result.URL = url;
            }
        }

        return result;
    }

    public class PomfResponse
    {
        public bool success { get; set; }
        public object error { get; set; }
        public List<PomfFile> files { get; set; }
    }

    public class PomfFile
    {
        public string hash { get; set; }
        public string name { get; set; }
        public string? url { get; set; }
        public string size { get; set; }
    }
}
