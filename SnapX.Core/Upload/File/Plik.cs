
// SPDX-License-Identifier: GPL-3.0-or-later


using System.Collections.Specialized;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization;
using SnapX.Core.Upload.BaseServices;
using SnapX.Core.Upload.BaseUploaders;
using SnapX.Core.Upload.Utils;
using SnapX.Core.Utils;

namespace SnapX.Core.Upload.File;

public class PlikFileUploaderService : FileUploaderService
{
    public override FileDestination EnumValue { get; } = FileDestination.Plik;

    public override GenericUploader CreateUploader(UploadersConfig config, TaskReferenceHelper taskInfo)
    {
        return new Plik(config.PlikSettings);
    }

    public override bool CheckConfig(UploadersConfig config)
    {
        return !string.IsNullOrEmpty(config.PlikSettings.URL) && !string.IsNullOrEmpty(config.PlikSettings.APIKey);
    }
}
[JsonSerializable(typeof(UploadMetadataResponse))]
internal partial class PlikContext : JsonSerializerContext;
public sealed class Plik : FileUploader
{
    public PlikSettings Settings { get; private set; }

    public Plik(PlikSettings settings)
    {
        Settings = settings;
    }

    [RequiresDynamicCode("Uploader")]
    [RequiresUnreferencedCode("Uploader")]
    public override UploadResult Upload(Stream stream, string? fileName)
    {
        if (string.IsNullOrEmpty(Settings.URL))
        {
            throw new Exception("Plik Host is empty.");
        }
        var requestHeaders = new NameValueCollection
        {
            ["X-PlikToken"] = Settings.APIKey
        };
        var metaDataReq = new UploadMetadataRequest();
        metaDataReq.Files = new UploadMetadataRequestFile0();
        metaDataReq.Files.File0 = new UploadMetadataRequestFile();
        metaDataReq.Files.File0.FileName = fileName;
        metaDataReq.Files.File0.FileType = MimeTypes.GetMimeType(fileName);
        metaDataReq.Files.File0.FileSize = Convert.ToInt32(stream.Length);
        metaDataReq.Removable = Settings.Removable;
        metaDataReq.OneShot = Settings.OneShot;
        if (Settings.TTLUnit != 3) // everything except the expire time -1
        {
            metaDataReq.Ttl = Convert.ToInt32(GetMultiplyIndex(2, Settings.TTLUnit) * Settings.TTL * 60);
        }
        else
        {
            metaDataReq.Ttl = -1;
        }
        if (Settings.HasComment)
        {
            metaDataReq.Comment = Settings.Comment;
        }
        if (Settings.IsSecured)
        {
            metaDataReq.Login = Settings.Login;
            metaDataReq.Password = Settings.Password;
        }
        var metaDataResp = SendRequest(HttpMethod.Post, Settings.URL + "/upload", JsonSerializer.Serialize(metaDataReq), headers: requestHeaders);
        var metaData = JsonSerializer.Deserialize<UploadMetadataResponse>(metaDataResp, new JsonSerializerOptions
        {
            TypeInfoResolver = PlikContext.Default
        });
        requestHeaders["x-uploadtoken"] = metaData.uploadToken;
        var url = $"{Settings.URL}/file/{metaData.id}/{metaData.files.First().Value.id}/{fileName}";
        var FileDatReq = SendRequestFile(url, stream, fileName, "file", headers: requestHeaders);

        return ConvertResult(metaData, FileDatReq);
    }

    private UploadResult ConvertResult(UploadMetadataResponse metaData, UploadResult fileDataReq)
    {
        var result = new UploadResult(fileDataReq.Response);
        //UploadMetadataResponse fileData = JsonConvert.DeserializeObject<UploadMetadataResponse>(fileDataReq.Response);
        var actFile = metaData.files.First().Value;
        result.URL = $"{Settings.URL}/file/{metaData.id}/{actFile.id}/{URLHelpers.URLEncode(actFile.fileName)}";
        return result;
    }

    internal static decimal GetMultiplyIndex(int newUnit, int oldUnit)
    {
        decimal multiplyValue = 1m;
        switch (newUnit)
        {
            case 0: // days
                switch (oldUnit)
                {
                    case 1: // hours
                        multiplyValue = 1m / 24m;
                        break;
                    case 2: // minutes
                        multiplyValue = 1m / 24m / 60m;
                        break;
                }
                break;
            case 1: // hours
                switch (oldUnit)
                {
                    case 0: // days
                        multiplyValue = 24m;
                        break;
                    case 2: // minutes
                        multiplyValue = 1m / 60m;
                        break;
                }
                break;
            case 2: // minutes
                switch (oldUnit)
                {
                    case 0: // days
                        multiplyValue = 60m * 24m;
                        break;
                    case 1: // hours
                        multiplyValue = 60m;
                        break;
                }
                break;
        }
        return multiplyValue;
    }
}

public class UploadMetadataRequestFile
{
    [JsonPropertyName("fileName")]
    public string? FileName { get; set; }
    [JsonPropertyName("fileType")]
    public string FileType { get; set; }
    [JsonPropertyName("fileSize")]
    public int FileSize { get; set; }
}

public class UploadMetadataRequestFile0
{
    [JsonPropertyName("0")]
    public UploadMetadataRequestFile File0 { get; set; }
}

public class UploadMetadataRequest
{
    [JsonPropertyName("ttl")]
    public int Ttl { get; set; }
    [JsonPropertyName("removable")]
    public bool Removable { get; set; }
    [JsonPropertyName("oneShot")]
    public bool OneShot { get; set; }
    [JsonPropertyName("comments")]
    public string Comment { get; set; }
    [JsonPropertyName("login")]
    public string Login { get; set; }
    [JsonPropertyName("password")]
    public string Password { get; set; }
    [JsonPropertyName("files")]
    public UploadMetadataRequestFile0 Files { get; set; }
}

public class UploadMetadataResponseFile
{
    public string id { get; set; }
    public string? fileName { get; set; }
    public string fileMd5 { get; set; }
    public string status { get; set; }
    public string fileType { get; set; }
    public int fileUploadDate { get; set; }
    public int fileSize { get; set; }
    public string reference { get; set; }
}

public class UploadMetadataResponse
{
    public string id { get; set; }
    public int uploadDate { get; set; }
    public int ttl { get; set; }
    public string shortUrl { get; set; }
    public string downloadDomain { get; set; }
    public string comments { get; set; }
    public Dictionary<string, UploadMetadataResponseFile> files { get; set; }
    public string uploadToken { get; set; }
    public bool admin { get; set; }
    public bool stream { get; set; }
    public bool oneShot { get; set; }
    public bool removable { get; set; }
    public bool protectedByPassword { get; set; }
    public bool protectedByYubikey { get; set; }
}
