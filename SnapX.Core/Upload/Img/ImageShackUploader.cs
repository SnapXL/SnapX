
// SPDX-License-Identifier: GPL-3.0-or-later


using System.Text.Json;
using System.Text.Json.Serialization;
using SnapX.Core.Upload.BaseServices;
using SnapX.Core.Upload.BaseUploaders;
using SnapX.Core.Upload.Utils;

namespace SnapX.Core.Upload.Img;

public class ImageShackImageUploaderService : ImageUploaderService
{
    public override ImageDestination EnumValue => ImageDestination.ImageShack;
    public override bool CheckConfig(UploadersConfig config)
    {
        return config.ImageShackSettings != null && !string.IsNullOrEmpty(config.ImageShackSettings.Auth_token);
    }

    public override GenericUploader CreateUploader(UploadersConfig config, TaskReferenceHelper taskInfo)
    {
        return new ImageShackUploader(APIKeys.ImageShackKey, config.ImageShackSettings);
    }
}
[JsonSerializable(typeof(ImageShackUploader.ImageShackLoginResponse))]
[JsonSerializable(typeof(ImageShackUploader.ImageShackErrorInfo))]
[JsonSerializable(typeof(ImageShackUploader.ImageShackUploadResponse))]
internal partial class ImageShackContext : JsonSerializerContext
{ }
public sealed class ImageShackUploader(string DeveloperKey, ImageShackOptions? Config) : ImageUploader
{
    private const string URLAPI = "https://api.imageshack.com/v2/";
    private const string? URLAccessToken = URLAPI + "user/login";
    private const string? URLUpload = URLAPI + "images";
    private JsonSerializerOptions options = new()
    {
        TypeInfoResolver = ImageShackContext.Default,
    };

    public ImageShackOptions? Config { get; set; } = Config;

    public bool GetAccessToken()
    {
        if (string.IsNullOrEmpty(Config.Username) || string.IsNullOrEmpty(Config.Password))
        {
            return false;
        }

        var args = new Dictionary<string, string?>
        {
            { "user", Config.Username },
            { "password", Config.Password }
        };

        var response = SendRequestMultiPart(URLAccessToken, args);

        if (string.IsNullOrEmpty(response))
        {
            return false;
        }

        var resp = JsonSerializer.Deserialize<ImageShackLoginResponse>(response, options);
        if (resp?.result?.auth_token == null) return false;

        Config.Auth_token = resp.result.auth_token;
        return true;
    }

    public override UploadResult Upload(Stream stream, string? fileName)
    {
        var arguments = new Dictionary<string, string?>
        {
            { "api_key", DeveloperKey },
            { "auth_token", Config?.Auth_token },
            { "public", Config is { IsPublic: true } ? "y" : "n" }
        };

        var result = SendRequestFile(URLUpload, stream, fileName, "file", arguments);

        if (string.IsNullOrEmpty(result.Response))
            return result;

        using var jsonDoc = JsonDocument.Parse(result.Response);
        var root = jsonDoc.RootElement;

        if (!root.TryGetProperty("success", out var success) || !success.GetBoolean())
            return HandleError(root);

        var uploadResult = root.GetProperty("result");

        if (!uploadResult.TryGetProperty("images", out var images) || images.GetArrayLength() <= 0) return result;
        var image = images[0];
        result.URL = $"https://imagizer.imageshack.com/a/img{image.GetProperty("server").GetString()}/{image.GetProperty("bucket").GetString()}/{image.GetProperty("filename").GetString()}";
        result.ThumbnailURL = $"https://imagizer.imageshack.us/v2/{Config.ThumbnailWidth}x{Config.ThumbnailHeight}q90/{image.GetProperty("server").GetString()}/{image.GetProperty("filename").GetString()}";

        return result;
    }

    private UploadResult HandleError(JsonElement root)
    {
        if (!root.TryGetProperty("error", out var error)) return new UploadResult();
        var errorInfo = JsonSerializer.Deserialize<ImageShackErrorInfo>(error.GetRawText(), options);
        Errors.Add(errorInfo?.ToString());

        return new UploadResult();
    }


    public class ImageShackErrorInfo
    {
        public int error_code { get; set; }
        public string? error_message { get; set; }

        public override string ToString()
        {
            return $"Error message: {error_message}\r\nError code: {error_code}";
        }
    }

    public class ImageShackLoginResponse
    {
        public bool success { get; set; }
        public int process_time { get; set; }
        public ImageShackLogin result { get; set; }
    }

    public class ImageShackLogin
    {
        public string? auth_token { get; set; }
        public int? user_id { get; set; }
        public string? email { get; set; }
        public string? username { get; set; }
        public ImageShackeUserAvatar? avatar { get; set; }
        public string? membership { get; set; }
        public string? membership_item_number { get; set; }
        public string? membership_cookie { get; set; }
    }

    public class ImageShackUser
    {
        public bool is_owner { get; set; }
        public int cache_version { get; set; }
        public string? username { get; set; }
        public string? description { get; set; }
        public int creation_date { get; set; }
        public string? location { get; set; }
        public string? first_name { get; set; }
        public string? last_name { get; set; }
        public ImageShackeUserAvatar? Avatar { get; set; }
    }

    public class ImageShackeUserAvatar
    {
        public int image_id { get; set; }
        public int server { get; set; }
        public string? filename { get; set; }
    }

    public class ImageShackUploadResponse
    {
        public bool success { get; set; }
        public int process_time { get; set; }
        public ImageShackUploadResult result { get; set; }
    }

    public class ImageShackUploadResult
    {
        public int max_filesize { get; set; }
        public int space_limit { get; set; }
        public int space_used { get; set; }
        public int space_left { get; set; }
        public int passed { get; set; }
        public int failed { get; set; }
        public int total { get; set; }
        public List<ImageShackImage> images { get; set; }
    }

    public class ImageShackImage
    {
        public string? id { get; set; }
        public int server { get; set; }
        public int bucket { get; set; }
        public string? lp_hash { get; set; }
        public string? filename { get; set; }
        public string? original_filename { get; set; }
        public string? direct_link { get; set; }
        public object? title { get; set; }
        public object? description { get; set; }
        public List<string>? tags { get; set; }
        public int likes { get; set; }
        public bool liked { get; set; }
        public int views { get; set; }
        public int comments_count { get; set; }
        public bool comments_disabled { get; set; }
        public int filter { get; set; }
        public int filesize { get; set; }
        public int creation_date { get; set; }
        public int width { get; set; }
        public int height { get; set; }
        public bool @public { get; set; }
        public bool is_owner { get; set; }
        public ImageShackUser? owner { get; set; }
        public List<ImageShackImage>? next_images { get; set; }
        public List<ImageShackImage>? prev_images { get; set; }
        public object? related_images { get; set; }
    }
}

public class ImageShackOptions
{
    public string? Username { get; set; }
    public string? Password { get; set; }
    public bool IsPublic { get; set; }
    public string? Auth_token { get; set; }
    public int ThumbnailWidth { get; set; } = 256;
    public int ThumbnailHeight { get; set; }
}

