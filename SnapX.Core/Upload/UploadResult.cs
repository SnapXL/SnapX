
// SPDX-License-Identifier: GPL-3.0-or-later


using System.Text;
using SnapX.Core.Upload.Utils;
using SnapX.Core.Utils;

namespace SnapX.Core.Upload;

public class UploadResult
{
    public string? URL { get; set; }
    public string? ThumbnailURL { get; set; }
    public string? DeletionURL { get; set; }
    public string? ShortenedURL { get; set; }

    public bool IsSuccess
    {
        get
        {
            string caller = GetCallerName();
            bool hasResponse = !string.IsNullOrEmpty(Response);

            bool finalResult = field && hasResponse;

            if (field && !hasResponse)
            {
                DebugHelper.Logger?.Debug($"[UploadResult] IsSuccess GET by [{caller}]: FALSE. (Reason: field is true, but Response is empty)");
            }
            else if (!finalResult && !field)
            {
                DebugHelper.Logger?.Debug($"[UploadResult] IsSuccess GET by [{caller}]: FALSE. (Reason: field is false)");
            }

            return finalResult;
        }
        set
        {
            string caller = GetCallerName();
            if (field != value)
            {
                DebugHelper.Logger?.Debug($"[UploadResult] IsSuccess SET by [{caller}]: {field} -> {value}");
                field = value;
            }
        }
    }

    private string GetCallerName()
    {
        try
        {
            // Frame 0 is GetCallerName, Frame 1 is the getter/setter, Frame 2 is the actual caller
            return new System.Diagnostics.StackTrace().GetFrame(2)?.GetMethod()?.Name ?? "Unknown";
        }
        catch
        {
            return "Unknown";
        }
    }

    public string? Response { get; set; }
    public UploaderErrorManager Errors { get; set; }
    public bool IsURLExpected { get; set; }

    public bool IsError
    {
        get
        {
            return Errors != null && Errors.Count > 0 && (!IsURLExpected || string.IsNullOrEmpty(URL));
        }
    }

    public ResponseInfo ResponseInfo { get; set; }

    public UploadResult()
    {
        Errors = new UploaderErrorManager();
        IsURLExpected = true;
    }

    public UploadResult(string? source, string? url = null) : this()
    {
        Response = source;
        URL = url;
    }

    public void ForceHTTPS()
    {
        URL = URLHelpers.ForcePrefix(URL);
        ThumbnailURL = URLHelpers.ForcePrefix(ThumbnailURL);
        DeletionURL = URLHelpers.ForcePrefix(DeletionURL);
        ShortenedURL = URLHelpers.ForcePrefix(ShortenedURL);
    }

    public override string? ToString()
    {
        if (!string.IsNullOrEmpty(ShortenedURL))
        {
            return ShortenedURL;
        }

        if (!string.IsNullOrEmpty(URL))
        {
            return URL;
        }

        return "";
    }

    public string ErrorsToString()
    {
        if (IsError)
        {
            return Errors.ToString();
        }

        return null;
    }

    public string ToSummaryString()
    {
        var sb = new StringBuilder()
            .AppendLine("URL: " + URL)
            .AppendLine("Thumbnail URL: " + ThumbnailURL)
            .AppendLine("Shortened URL: " + ShortenedURL)
            .AppendLine("Deletion URL: " + DeletionURL);

        return sb.ToString();
    }
}

