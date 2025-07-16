// SPDX-License-Identifier: GPL-3.0-or-later


using System.Collections.Specialized;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using SnapX.Core.Upload.BaseServices;
using SnapX.Core.Upload.BaseUploaders;
using SnapX.Core.Upload.Utils;
using SnapX.Core.Utils;

namespace SnapX.Core.Upload.File;

public class StreamableFileUploaderService : FileUploaderService
{
    public override FileDestination EnumValue { get; } = FileDestination.Streamable;

    public override bool CheckConfig(UploadersConfig config)
    {
        return !string.IsNullOrEmpty(config.StreamableUsername) && !string.IsNullOrEmpty(config.StreamablePassword);
    }

    public override GenericUploader CreateUploader(UploadersConfig config, TaskReferenceHelper taskInfo)
    {
        return new Streamable(config.StreamableUsername, config.StreamablePassword)
        {
            UseDirectURL = config.StreamableUseDirectURL
        };
    }
}

public class Streamable : FileUploader
{
    private const string? Host = "https://api.streamable.com";

    public string Email { get; private set; }
    public string Password { get; private set; }
    public bool UseDirectURL { get; set; }

    public Streamable(string email, string password)
    {
        Email = email;
        Password = password;
    }

    [UnconditionalSuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code", Justification = "<Pending>")]
    public override UploadResult Upload(Stream stream, string? fileName)
    {
        NameValueCollection headers = null;

        if (!string.IsNullOrEmpty(Email) && !string.IsNullOrEmpty(Password))
        {
            headers = RequestHelpers.CreateAuthenticationHeader(Email, Password);
        }

        string? url = URLHelpers.CombineURL(Host, "upload");
        UploadResult result = SendRequestFile(url, stream, fileName, "file", headers: headers);

        TranscodeFile(result);

        return result;
    }

    [RequiresDynamicCode("Calls System.Text.Json.JsonSerializer.Deserialize<TValue>(String, JsonSerializerOptions)")]
    [RequiresUnreferencedCode("Calls System.Text.Json.JsonSerializer.Deserialize<TValue>(String, JsonSerializerOptions)")]
    private void TranscodeFile(UploadResult result)
    {
        StreamableTranscodeResponse transcodeResponse = JsonSerializer.Deserialize<StreamableTranscodeResponse>(result.Response);

        if (!string.IsNullOrEmpty(transcodeResponse.Shortcode))
        {
            ProgressManager progress = new ProgressManager(100);
            OnProgressChanged(progress);

            while (!StopUploadRequested)
            {
                string? statusJson = SendRequest(HttpMethod.Get, URLHelpers.CombineURL(Host, "videos", transcodeResponse.Shortcode));
                StreamableStatusResponse response = JsonSerializer.Deserialize<StreamableStatusResponse>(statusJson);

                if (response.status > 2)
                {
                    Errors.Add(response.message);
                    result.IsSuccess = false;
                    break;
                }
                else if (response.status == 2)
                {
                    progress.UpdateProgress(100 - progress.Position);
                    OnProgressChanged(progress);

                    result.IsSuccess = true;

                    if (UseDirectURL && response.files != null && response.files.mp4 != null && !string.IsNullOrEmpty(response.files.mp4.url))
                    {
                        result.URL = URLHelpers.ForcePrefix(response.files.mp4.url);
                    }
                    else
                    {
                        result.URL = URLHelpers.ForcePrefix(response.url);
                    }

                    break;
                }

                progress.UpdateProgress(response.percent - progress.Position);
                OnProgressChanged(progress);

                Thread.Sleep(1000);
            }
        }
        else
        {
            Errors.Add("Could not create video");
            result.IsSuccess = false;
        }
    }
}

public class StreamableTranscodeResponse
{
    public string Shortcode { get; set; }
    public int Status { get; set; }
}

public class StreamableStatusResponse
{
    public int status { get; set; }
    public StreamableStatusResponseFiles files { get; set; }
    //public string url_root { get; set; }
    public string thumbnail_url { get; set; }
    //public string[] formats { get; set; }
    public string? url { get; set; }
    public string? message { get; set; }
    public string title { get; set; }
    public long percent { get; set; }
}

public class StreamableStatusResponseFiles
{
    public StreamableStatusResponseVideo mp4 { get; set; }
}

public class StreamableStatusResponseVideo
{
    public int status { get; set; }
    public string? url { get; set; }
    public int framerate { get; set; }
    public int height { get; set; }
    public int width { get; set; }
    public long bitrate { get; set; }
    public long size { get; set; }
}
