// SPDX-License-Identifier: GPL-3.0-or-later


using SnapX.Core.Upload.BaseUploaders;

namespace SnapX.Core.Upload.Img;

public sealed class Img1Uploader : ImageUploader
{
    private const string? uploadURL = "https://img1.us/?app";

    public override UploadResult Upload(Stream stream, string? fileName)
    {
        var result = SendRequestFile(uploadURL, stream, fileName, "fileup");

        if (!result.IsSuccess || string.IsNullOrEmpty(result.Response))
            return result;

        var lastLine = result.Response.Split('\n').LastOrDefault()?.Trim();

        if (!string.IsNullOrEmpty(lastLine))
            result.URL = lastLine;

        return result;
    }
}

