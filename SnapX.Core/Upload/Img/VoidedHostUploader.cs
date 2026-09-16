
// SPDX-License-Identifier: GPL-3.0-or-later


using System.Collections.Specialized;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using SnapX.Core.Upload.BaseServices;
using SnapX.Core.Upload.BaseUploaders;
using SnapX.Core.Upload.Utils;

namespace SnapX.Core.Upload.Img;

public class VoidedHostImageUploaderService : ImageUploaderService
{
    public override ImageDestination EnumValue => ImageDestination.VoidedHost;

    public override bool CheckConfig(UploadersConfig config) => VoidedHostUploader.IsUploadConfigured(config);

    public override GenericUploader CreateUploader(UploadersConfig config, TaskReferenceHelper taskInfo)
    {
        bool useGuest = VoidedHostUploader.ShouldUseGuestMode(config);
        string key = VoidedHostUploader.GetEffectiveUploadKey(config);

        return new VoidedHostUploader(key, useGuest);
    }
}

internal sealed class VoidedHostMultipartUploader : FileUploader
{
    private readonly string _uploadKey;
    private readonly bool _useGuestMode;
    private readonly string _apiUrl;

    public VoidedHostMultipartUploader(string uploadKey, bool useGuestMode, string apiUrl)
    {
        _uploadKey = uploadKey ?? "";
        _useGuestMode = useGuestMode;
        _apiUrl = apiUrl ?? "";
    }

    [RequiresDynamicCode("Uploader")]
    [RequiresUnreferencedCode("Uploader")]
    public override UploadResult Upload(Stream stream, string? fileName)
    {
        var result = new UploadResult();
        Errors.Errors.Clear();

        DebugHelper.WriteLine($"[voided.host] Upload starting. ApiUrl={_apiUrl}, FileName={fileName}, GuestMode={_useGuestMode}, StreamLength={stream?.Length}, StreamPosition={stream?.Position}");

        if (string.IsNullOrWhiteSpace(_uploadKey))
        {
            DebugHelper.WriteLine("[voided.host] Upload key is empty, aborting.");
            if (_useGuestMode)
            {
                Errors.Add("Guest uploads aren't enabled in this build of SnapX. Turn off guest upload, then paste your own voided.host upload key (or use a build that includes guest uploads).");
            }
            else
            {
                Errors.Add("Paste your voided.host upload key in SnapX destinations, or turn on guest upload if your build supports it.");
            }

            return result;
        }

        var headers = new NameValueCollection
        {
            ["Authorization"] = _uploadKey.Trim()
        };

        var args = new Dictionary<string, string?>
        {
            ["p"] = "snapx",
            ["v"] = "1",
            ["t"] = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString()
        };

        ReturnResponseOnError = true;
        DebugHelper.WriteLine($"[voided.host] Sending request to {_apiUrl}");
        result = SendRequestFile(_apiUrl, stream, fileName, "file", args, headers);

        var statusCode = result.ResponseInfo?.StatusCode;
        DebugHelper.WriteLine($"[voided.host] Response received. IsSuccess={result.IsSuccess}, StatusCode={statusCode}, ResponseLength={result.Response?.Length ?? 0}");

        if (!string.IsNullOrEmpty(result.Response))
        {
            var preview = result.Response.Length > 500 ? result.Response[..500] + "..." : result.Response;
            DebugHelper.WriteLine($"[voided.host] Response body: {preview}");
        }

        if (VoidedHostResponseParser.TryBuildUserFacingError(result.Response, out var structuredError))
        {
            DebugHelper.WriteLine($"[voided.host] API returned structured error: {structuredError}");
            Errors.Errors.Clear();
            Errors.Add(structuredError);
            result.IsSuccess = false;
            return result;
        }

        ApplyUnauthorizedRotationHint(result);

        if (!result.IsSuccess || string.IsNullOrEmpty(result.Response))
        {
            DebugHelper.WriteLine($"[voided.host] Upload failed or empty response. IsSuccess={result.IsSuccess}, HasResponse={!string.IsNullOrEmpty(result.Response)}, ErrorCount={Errors.Count}");
            return result;
        }

        if (!VoidedHostResponseParser.TryParseUploadResponse(result.Response, out var shareLink, out var deletionUrl, out var apiUserError))
        {
            DebugHelper.WriteLine($"[voided.host] Failed to parse upload response. ApiError={apiUserError}");
            if (!string.IsNullOrEmpty(apiUserError))
            {
                Errors.Add(apiUserError);
            }
            else
            {
                Errors.Add("voided.host replied in an unexpected format. Try updating SnapX; if it keeps happening, forward the upload log to voided.host support.");
            }

            result.IsSuccess = false;
            return result;
        }

        DebugHelper.WriteLine($"[voided.host] Upload successful. URL={shareLink}, DeletionURL={deletionUrl}");
        result.URL = shareLink;
        if (!string.IsNullOrWhiteSpace(deletionUrl))
        {
            result.DeletionURL = deletionUrl;
        }

        result.IsSuccess = true;
        return result;
    }

    private void ApplyUnauthorizedRotationHint(UploadResult result)
    {
        var info = LastResponseInfo ?? result.ResponseInfo;
        if (info?.StatusCode != HttpStatusCode.Unauthorized)
        {
            return;
        }

        DebugHelper.WriteLine($"[voided.host] 401 Unauthorized. GuestMode={_useGuestMode}");
        Errors.Errors.Clear();

        if (_useGuestMode)
        {
            Errors.Add("voided.host blocked guest uploads (access expired or changed). Turn off guest upload and paste your personal upload key, or install the latest SnapX. More help: " + VoidedHostUploader.UploadKeySettingsUrl);
        }
        else
        {
            Errors.Add("voided.host didn't accept your upload key—it may have been reset. Open your upload-key page, copy the current key, and paste it in SnapX: " + VoidedHostUploader.UploadKeySettingsUrl);
        }
    }
}

public sealed class VoidedHostUploader : ImageUploader
{
    /// <summary>
    /// Upload key used when the user enables "Upload as guest". Must be provisioned by voided.host
    /// for the shared guest account. Guest mode stays off in the UI and in CheckConfig until this is non-empty.
    /// Omit from public repos — set at release build time only.
    /// </summary>
    public const string GuestUploadApiKey = "MTA3NQ.MTc3ODMzMzE1MDI3NA.wIeJFNAwaYymgvgMIhLCxRamvUXWtZMtSGmjQNZDfGDKGVqx";

    public const string ImageUploadApiUrl = "https://api.voided.host/v2/images";
    public const string PasteUploadApiUrl = "https://api.voided.host/v2/pastes";
    public const string FileUploadApiUrl = "https://api.voided.host/v2/files";
    public const string UploadKeySettingsUrl = "https://voided.host/settings/security";
    public const string SnapXSetupRegisterUrl = "https://voided.host/register?inviter=Lixqa&next=/flows/sharex-uploader-setup/";
    public const string ImageDeletionUrlFormat = "https://voided.host/settings/image/{0}";

    private readonly VoidedHostMultipartUploader _multipart;

    public string UploadKey { get; private set; }

    public bool UseGuestMode { get; private set; }

    public VoidedHostUploader(string uploadKey, bool useGuestMode)
    {
        UploadKey = uploadKey ?? "";
        UseGuestMode = useGuestMode;
        _multipart = new VoidedHostMultipartUploader(uploadKey, useGuestMode, ImageUploadApiUrl);
        _multipart.ProgressChanged += OnProgressChanged;
        _multipart.EarlyURLCopyRequested += OnEarlyURLCopyRequested;
    }

    public static bool IsUploadConfigured(UploadersConfig config)
    {
        if (config == null)
        {
            return false;
        }

        if (ShouldUseGuestMode(config))
        {
            return true;
        }

        return !string.IsNullOrWhiteSpace(config.VoidedHostUploadKey);
    }

    public static bool ShouldUseGuestMode(UploadersConfig config)
    {
        return config != null && config.VoidedHostUseGuest && IsGuestUploadKeyConfigured();
    }

    public static bool IsGuestUploadKeyConfigured()
    {
        return !string.IsNullOrWhiteSpace(GuestUploadApiKey);
    }

    public static string GetEffectiveUploadKey(UploadersConfig config)
    {
        if (ShouldUseGuestMode(config))
        {
            return GuestUploadApiKey.Trim();
        }

        return config?.VoidedHostUploadKey?.Trim() ?? "";
    }

    [RequiresDynamicCode("Uploader")]
    [RequiresUnreferencedCode("Uploader")]
    public override UploadResult Upload(Stream stream, string? fileName)
    {
        Errors.Errors.Clear();
        _multipart.BufferSize = BufferSize;
        UploadResult result = _multipart.Upload(stream, fileName);
        Errors.Add(_multipart.Errors);
        return result;
    }

    public override void StopUpload()
    {
        _multipart.StopUpload();
        base.StopUpload();
    }
}

[JsonSerializable(typeof(VoidedHostApiResponse))]
internal partial class VoidedHostJsonContext : JsonSerializerContext;

internal sealed class VoidedHostApiResponse
{
    [JsonPropertyName("error")]
    public JsonElement? Error { get; set; }

    [JsonPropertyName("message")]
    public string? Message { get; set; }

    [JsonPropertyName("_errors")]
    public List<VoidedHostApiErrorDetail>? ErrorDetails { get; set; }

    [JsonPropertyName("data")]
    public VoidedHostApiData? Data { get; set; }
}

internal sealed class VoidedHostApiErrorDetail
{
    [JsonPropertyName("message")]
    public string? Message { get; set; }
}

internal sealed class VoidedHostApiData
{
    [JsonPropertyName("shareLink")]
    public string? ShareLink { get; set; }

    [JsonPropertyName("id")]
    public JsonElement? Id { get; set; }
}

internal static class VoidedHostResponseParser
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        TypeInfoResolver = VoidedHostJsonContext.Default,
        PropertyNameCaseInsensitive = true
    };

    public static bool TryBuildUserFacingError(string? json, out string? message)
    {
        message = null;

        if (string.IsNullOrWhiteSpace(json))
        {
            return false;
        }

        try
        {
            json = json.TrimStart();
            if (json.Length == 0 || json[0] != '{')
            {
                return false;
            }

            var response = JsonSerializer.Deserialize<VoidedHostApiResponse>(json, JsonOptions);
            if (response == null || !IsTruthyError(response.Error))
            {
                return false;
            }

            var sb = new StringBuilder();

            var main = response.Message?.Trim();
            if (!string.IsNullOrEmpty(main))
            {
                sb.Append("voided.host: ").Append(main);
            }

            if (response.ErrorDetails != null)
            {
                foreach (var item in response.ErrorDetails)
                {
                    var detail = item.Message?.Trim();
                    if (string.IsNullOrEmpty(detail))
                    {
                        continue;
                    }

                    if (string.Equals(detail, main, StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    if (sb.Length > 0)
                    {
                        sb.Append(Environment.NewLine);
                    }

                    sb.Append(detail);
                }
            }

            message = sb.Length > 0 ? sb.ToString() : "voided.host returned an error.";
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static bool IsTruthyError(JsonElement? errorElement)
    {
        if (errorElement == null || !errorElement.HasValue)
        {
            return false;
        }

        var tok = errorElement.Value;

        return tok.ValueKind switch
        {
            JsonValueKind.Null or JsonValueKind.Undefined => false,
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Number => tok.TryGetInt64(out var n) && n != 0,
            JsonValueKind.String => tok.GetString() is { } s &&
                                    !string.IsNullOrEmpty(s.Trim()) &&
                                    (s.Equals("true", StringComparison.OrdinalIgnoreCase) ||
                                     (int.TryParse(s, out var parsed) && parsed != 0)),
            _ => false
        };
    }

    public static bool TryParseUploadResponse(string? json, out string? shareLink, out string? deletionUrl, out string? errorMessage)
    {
        shareLink = null;
        deletionUrl = null;
        errorMessage = null;

        if (string.IsNullOrWhiteSpace(json))
        {
            return false;
        }

        try
        {
            var response = JsonSerializer.Deserialize<VoidedHostApiResponse>(json, JsonOptions);
            if (response == null)
            {
                return false;
            }

            if (IsTruthyError(response.Error))
            {
                if (!TryBuildUserFacingError(json, out errorMessage) || string.IsNullOrEmpty(errorMessage))
                {
                    errorMessage = "voided.host returned an error.";
                }

                return false;
            }

            if (response.Data != null)
            {
                var sl = response.Data.ShareLink?.Trim();
                if (!string.IsNullOrEmpty(sl))
                {
                    shareLink = sl;
                }

                if (response.Data.Id is { } idElement && idElement.ValueKind != JsonValueKind.Null && idElement.ValueKind != JsonValueKind.Undefined)
                {
                    var idPart = idElement.ToString();
                    if (!string.IsNullOrEmpty(idPart))
                    {
                        deletionUrl = string.Format(VoidedHostUploader.ImageDeletionUrlFormat, idPart);
                    }
                }
            }

            return !string.IsNullOrEmpty(shareLink);
        }
        catch
        {
            return false;
        }
    }
}
