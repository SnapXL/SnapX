
// SPDX-License-Identifier: GPL-3.0-or-later


using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using SnapX.Core.Upload.BaseServices;
using SnapX.Core.Upload.BaseUploaders;
using SnapX.Core.Upload.Utils;
using SnapX.Core.Utils;
using SnapX.Core.Utils.Parsers;

namespace SnapX.Core.Upload.File;
public class MediaFireFileUploaderService : FileUploaderService
{
    public override FileDestination EnumValue => FileDestination.MediaFire;

    public override bool CheckConfig(UploadersConfig config)
    {
        return !string.IsNullOrEmpty(config.MediaFireUsername) && !string.IsNullOrEmpty(config.MediaFirePassword);
    }

    public override GenericUploader CreateUploader(UploadersConfig config, TaskReferenceHelper taskInfo)
    {
        return new MediaFire(APIKeys.MediaFireAppId, APIKeys.MediaFireApiKey, config.MediaFireUsername, config.MediaFirePassword)
        {
            UploadPath = NameParser.Parse(NameParserType.URL, config.MediaFirePath),
            UseLongLink = config.MediaFireUseLongLink
        };
    }
}
[JsonSerializable(typeof(MediaFire.SimpleUploadResponse))]
[JsonSerializable(typeof(MediaFire.GetSessionTokenResponse))]
[JsonSerializable(typeof(MediaFire.PollUploadResponse))]
internal partial class MediaFireContext : JsonSerializerContext;
public sealed class MediaFire : FileUploader
{
    public string? UploadPath { get; set; }
    public bool UseLongLink { get; set; }

    private static readonly string apiUrl = "https://www.mediafire.com/api/";
    private static readonly int pollInterval = 1000;
    private readonly string? appId;
    private readonly string apiKey, user;
    private readonly string? pasw;
    private string sessionToken, signatureTime;
    private int signatureKey;

    public MediaFire(string? appId, string apiKey, string user, string? pasw)
    {
        this.appId = appId;
        this.apiKey = apiKey;
        this.user = user;
        this.pasw = pasw;
    }

    public override UploadResult Upload(Stream stream, string? fileName)
    {
        AllowReportProgress = false;
        GetSessionToken();
        AllowReportProgress = true;
        var key = SimpleUpload(stream, fileName);
        AllowReportProgress = false;
        string? url;
        while ((url = PollUpload(key, fileName)) == null)
        {
            Thread.Sleep(pollInterval);
        }
        return new UploadResult() { IsSuccess = true, URL = url };
    }

    [UnconditionalSuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code", Justification = "<Pending>")]
    private void GetSessionToken()
    {
        var args = new Dictionary<string, string?>
        {
            { "email", user },
            { "password", pasw },
            { "application_id", appId },
            { "token_version", "2" },
            { "response_format", "json" },
            { "signature", GetInitSignature() }
        };

        var respStr = SendRequestMultiPart(apiUrl + "user/get_session_token.php", args);
        var resp = DeserializeResponse<GetSessionTokenResponse>(respStr);
        EnsureSuccess(resp);

        if (resp.session_token == null || resp.time == null || resp.secret_key == null)
            throw new IOException("Invalid response");

        sessionToken = resp.session_token;
        signatureTime = resp.time;
        signatureKey = (int)resp.secret_key;
    }

    [UnconditionalSuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code", Justification = "<Pending>")]
    private string? SimpleUpload(Stream stream, string? fileName)
    {
        var args = new Dictionary<string, string?>
        {
            { "session_token", sessionToken },
            { "path", UploadPath },
            { "response_format", "json" },
        };
        args.Add("signature", GetSignature("upload/simple.php", args));

        var url = URLHelpers.CreateQueryString(apiUrl + "upload/simple.php", args);
        var res = SendRequestFile(url, stream, fileName, "Filedata");

        if (!res.IsSuccess) throw new IOException(res.ErrorsToString());

        var resp = DeserializeResponse<SimpleUploadResponse>(res.Response);
        EnsureSuccess(resp);

        if (resp.doupload.result != 0 || resp.doupload.key == null)
            throw new IOException("Invalid response");

        return resp.doupload.key;
    }

    [UnconditionalSuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code", Justification = "<Pending>")]
    private string? PollUpload(string? uploadKey, string? fileName)
    {
        var args = new Dictionary<string, string?>
        {
            { "session_token", sessionToken },
            { "key", uploadKey },
            { "filename", fileName },
            { "response_format", "json" },
        };
        args.Add("signature", GetSignature("upload/poll_upload.php", args));

        var respStr = SendRequestMultiPart(apiUrl + "upload/poll_upload.php", args);
        var resp = DeserializeResponse<PollUploadResponse>(respStr);
        EnsureSuccess(resp);

        if (resp.doupload.result == null || resp.doupload.status == null)
            throw new IOException("Invalid response");

        if (resp.doupload.result != 0 || resp.doupload.fileerror != null)
            throw new IOException($"Couldn't upload the file: {resp.doupload.description ?? "Unknown error"}");
        if (resp.doupload.status != 99) return null;

        if (resp.doupload.quickkey == null) throw new IOException("Invalid response");

        var url = URLHelpers.CombineURL("https://www.mediafire.com/view", resp.doupload.quickkey);
        if (UseLongLink) url = URLHelpers.CombineURL(url, URLHelpers.URLEncode(resp.doupload.filename));
        return url;
    }

    private void EnsureSuccess(MFResponse resp)
    {
        if (resp.result != "Success")
            throw new IOException($"Couldn't upload the file: {resp.message ?? "Unknown error"}");

        if (resp.new_key == "yes") NextSignatureKey();
    }

    private string? GetInitSignature()
    {
        var signatureStr = user + pasw + appId + apiKey;
        using var sha1Gen = SHA1.Create();
        var sha1Bytes = sha1Gen.ComputeHash(Encoding.ASCII.GetBytes(signatureStr));
        return BytesToString(sha1Bytes);
    }


    private string? GetSignature(string urlSuffix, Dictionary<string, string?> args)
    {
        var keyStr = (signatureKey % 256).ToString(CultureInfo.InvariantCulture);
        var urlStr = CreateNonEscapedQuery("/api/" + urlSuffix, args);
        var signatureStr = keyStr + signatureTime + urlStr;

        using var md5gen = MD5.Create();
        var md5Bytes = md5gen.ComputeHash(Encoding.ASCII.GetBytes(signatureStr));
        return BytesToString(md5Bytes);
    }


    private void NextSignatureKey()
    {
        signatureKey = (int)(((long)signatureKey * 16807) % 2147483647);
    }

    [RequiresDynamicCode("Calls System.Text.Json.JsonSerializer.Deserialize<TValue>(String, JsonSerializerOptions)")]
    [RequiresUnreferencedCode("Calls System.Text.Json.JsonSerializer.Deserialize<TValue>(String, JsonSerializerOptions)")]
    private T DeserializeResponse<T>(string? s) where T : new()
    {
        // Deserialize the string into a JSON object
        var jsonDoc = JsonDocument.Parse(s);

        // Extract the value of the "response" property from the JSON document and deserialize it into T
        var responseElement = jsonDoc.RootElement.GetProperty("response");
        return JsonSerializer.Deserialize<T>(responseElement.GetRawText(), new JsonSerializerOptions
        {
            TypeInfoResolver = MediaFireContext.Default
        });
    }
    private static char IntToChar(int x)
    {
        if (x < 10) return (char)(x + '0');
        return (char)(x - 10 + 'a');
    }

    private static string? BytesToString(byte[] b)
    {
        var res = new char[b.Length * 2];
        for (var i = 0; i < b.Length; ++i)
        {
            res[2 * i] = IntToChar(b[i] >> 4);
            res[(2 * i) + 1] = IntToChar(b[i] & 0xf);
        }
        return new string(res);
    }

    private static string CreateNonEscapedQuery(string url, Dictionary<string, string?> args)
    {
        if (args != null && args.Count > 0)
            return url + "?" + string.Join("&", args.Select(x => x.Key + "=" + x.Value).ToArray());
        return url;
    }

    public class MFResponse
    {
        public string result { get; set; }
        public int? error { get; set; }
        public string message { get; set; }
        public string new_key { get; set; }
    }

    public class GetSessionTokenResponse : MFResponse
    {
        public string session_token { get; set; }
        public int? secret_key { get; set; }
        public string time { get; set; }
    }

    public class SimpleUploadResponse : MFResponse
    {
        public DoUpload doupload { get; set; }

        public class DoUpload
        {
            public int? result { get; set; }
            public string? key { get; set; }
        }
    }

    public class PollUploadResponse : MFResponse
    {
        public DoUpload doupload { get; set; }

        public class DoUpload
        {
            public int? result { get; set; }
            public int? status { get; set; }
            public string description { get; set; }
            public int? fileerror { get; set; }
            public string? quickkey { get; set; }
            public string? filename { get; set; }
        }
    }
}

