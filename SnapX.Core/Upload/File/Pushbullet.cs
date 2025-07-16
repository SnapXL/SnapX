// SPDX-License-Identifier: GPL-3.0-or-later


using System.Collections.Specialized;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization;
using SnapX.Core.Upload.BaseServices;
using SnapX.Core.Upload.BaseUploaders;
using SnapX.Core.Upload.Utils;
using SnapX.Core.Utils.Extensions;

namespace SnapX.Core.Upload.File;

public class PushbulletFileUploaderService : FileUploaderService
{
    public override FileDestination EnumValue => FileDestination.Pushbullet;

    public override bool CheckConfig(UploadersConfig config)
    {
        return config.PushbulletSettings != null && !string.IsNullOrEmpty(config.PushbulletSettings.UserAPIKey) &&
            config.PushbulletSettings.DeviceList != null && config.PushbulletSettings.DeviceList.IsValidIndex(config.PushbulletSettings.SelectedDevice);
    }

    public override GenericUploader CreateUploader(UploadersConfig config, TaskReferenceHelper taskInfo)
    {
        return new Pushbullet(config.PushbulletSettings);
    }
}

[JsonSerializable(typeof(Pushbullet.PushbulletResponseFileUpload))]
[JsonSerializable(typeof(Pushbullet.PushbulletResponseDevice))]
[JsonSerializable(typeof(Pushbullet.PushbulletResponseDevices))]
[JsonSerializable(typeof(Pushbullet.PushbulletResponseFileUpload))]
[JsonSerializable(typeof(Pushbullet.PushbulletResponsePush))]
[JsonSerializable(typeof(Pushbullet.PushbulletResponseFileUploadData))]
internal partial class PushbulletContext : JsonSerializerContext;
public sealed class Pushbullet(PushbulletSettings Config) : FileUploader
{
    public PushbulletSettings Config { get; private set; } = Config;

    private JsonSerializerOptions Options = new()
    {
        TypeInfoResolver = PushbulletContext.Default
    };

    private const string
        wwwPushesURL = "https://www.pushbullet.com/pushes",
        apiURL = "https://api.pushbullet.com/v2";

    private const string?
        apiGetDevicesURL = apiURL + "/devices";

    private const string?
        apiSendPushURL = apiURL + "/pushes";

    private const string?
        apiRequestFileUploadURL = apiURL + "/upload-request";

    [RequiresDynamicCode("Uploader")]
    [RequiresUnreferencedCode("Uploader")]
    public UploadResult PushFile(Stream stream, string? fileName)
    {
        var headers = RequestHelpers.CreateAuthenticationHeader(Config.UserAPIKey, "");

        Dictionary<string, string?> upArgs = new Dictionary<string, string?>
        {
            { "file_name", fileName }
        };

        var uploadRequest = SendRequestMultiPart(apiRequestFileUploadURL, upArgs, headers);

        if (uploadRequest == null) return null;

        var fileInfo = JsonSerializer.Deserialize<PushbulletResponseFileUpload>(uploadRequest, Options);

        if (fileInfo == null) return null;

        var pushArgs = upArgs;

        upArgs = new Dictionary<string, string?>
        {
            { "awsaccesskeyid", fileInfo.data.awsaccesskeyid },
            { "acl", fileInfo.data.acl },
            { "key", fileInfo.data.key },
            { "signature", fileInfo.data.signature },
            { "policy", fileInfo.data.policy },
            { "content-type", fileInfo.data.content_type }
        };

        var uploadResult = SendRequestFile(fileInfo.upload_url, stream, fileName, "file", upArgs);

        if (uploadResult == null) return null;

        pushArgs.Add("device_iden", Config.CurrentDevice.Key);
        pushArgs.Add("type", "file");
        pushArgs.Add("file_url", fileInfo.file_url);
        pushArgs.Add("body", "Sent via SnapX");
        pushArgs.Add("file_type", fileInfo.file_type);

        var pushResult = SendRequestMultiPart(apiSendPushURL, pushArgs, headers);

        if (pushResult == null) return null;

        var push = JsonSerializer.Deserialize<PushbulletResponsePush>(pushResult, Options);

        if (push != null)
            uploadResult.URL = wwwPushesURL + "?push_iden=" + push.iden;

        return uploadResult;
    }

    [RequiresUnreferencedCode("Calls System.Text.Json.JsonSerializer.Deserialize<TValue>(String, JsonSerializerOptions)")]
    private string Push(string? pushType, string valueType, string? value, string? title)
    {
        NameValueCollection headers = RequestHelpers.CreateAuthenticationHeader(Config.UserAPIKey, "");

        Dictionary<string, string?> args = new Dictionary<string, string?>
        {
            { "device_iden", Config.CurrentDevice.Key },
            { "type", pushType },
            { "title", title },
            { valueType, value }
        };

        if (valueType != "body")
        {
            args.Add("body", pushType == "link" ? value : "Sent via SnapX");
        }

        var response = SendRequestMultiPart(apiSendPushURL, args, headers);

        if (response == null) return null;

        var push = JsonSerializer.Deserialize<PushbulletResponsePush>(response, Options);

        if (push != null)
            return wwwPushesURL + "?push_iden=" + push.iden;

        return null;
    }

    [UnconditionalSuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code", Justification = "<Pending>")]
    public string PushNote(string? note, string? title)
    {
        return Push("note", "body", note, title);
    }

    [UnconditionalSuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code", Justification = "<Pending>")]
    public string PushLink(string? link, string? title)
    {
        return Push("link", "url", link, title);
    }

    [UnconditionalSuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code", Justification = "<Pending>")]
    public override UploadResult Upload(Stream stream, string? fileName)
    {
        if (string.IsNullOrEmpty(Config.UserAPIKey)) throw new Exception("Missing API key.");
        if (Config.CurrentDevice == null) throw new Exception("No device set to push to.");
        if (string.IsNullOrEmpty(Config.CurrentDevice.Key)) throw new Exception("Missing device key.");

        return PushFile(stream, fileName);
    }

    [RequiresUnreferencedCode("Uploader")]
    public List<PushbulletDevice> GetDeviceList()
    {
        var headers = RequestHelpers.CreateAuthenticationHeader(Config.UserAPIKey, "");

        var response = SendRequest(HttpMethod.Get, apiGetDevicesURL, headers: headers);

        var devicesResponse = JsonSerializer.Deserialize<PushbulletResponseDevices>(response, Options);

        return devicesResponse is { devices: not null } ? devicesResponse.devices.Where(x => !string.IsNullOrEmpty(x.nickname)).Select(x1 => new PushbulletDevice { Key = x1.iden, Name = x1.nickname }).ToList() : [];
    }

    public class PushbulletResponseDevices
    {
        public List<PushbulletResponseDevice>? devices { get; set; }
    }

    public class PushbulletResponseDevice
    {
        public string iden { get; set; }
        public string nickname { get; set; }
    }

    public class PushbulletResponsePush
    {
        public string iden { get; set; }
        public string device_iden { get; set; }
        public PushbulletResponsePushData data { get; set; }
        public long created { get; set; }
    }

    public class PushbulletResponsePushData
    {
        public string type { get; set; }
        public string title { get; set; }
        public string body { get; set; }
    }

    public class PushbulletResponseFileUpload
    {
        public string? file_type { get; set; }
        public string file_name { get; set; }
        public string? file_url { get; set; }
        public string? upload_url { get; set; }
        public PushbulletResponseFileUploadData data { get; set; }
    }

    public class PushbulletResponseFileUploadData
    {
        public string awsaccesskeyid { get; set; }
        public string? acl { get; set; }
        public string? key { get; set; }
        public string? signature { get; set; }
        public string? policy { get; set; }
        [JsonPropertyName("content-type")]
        public string? content_type { get; set; }
    }
}

public class PushbulletDevice
{
    public string? Key { get; set; }
    public string Name { get; set; }
}

public class PushbulletSettings
{
    public string UserAPIKey { get; set; } = "";
    public List<PushbulletDevice?> DeviceList { get; set; } = [];
    public int SelectedDevice { get; set; } = 0;

    public PushbulletDevice? CurrentDevice => DeviceList.IsValidIndex(SelectedDevice) ? DeviceList[SelectedDevice] : null;
}
