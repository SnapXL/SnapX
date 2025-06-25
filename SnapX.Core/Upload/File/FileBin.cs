
// SPDX-License-Identifier: GPL-3.0-or-later


using SnapX.Core.Upload.BaseUploaders;

namespace SnapX.Core.Upload.File;

public sealed class FileBin : FileUploader
{
    public override UploadResult Upload(Stream stream, string? fileName)
    {
        var args = new Dictionary<string, string?>
        {
            { "MAX_FILE_SIZE", "82428800" }
        };

        var result = SendRequestFile("https://filebin.ca/upload.php", stream, fileName, "file", args);

        if (result.IsSuccess)
        {
            result.URL = result.Response.Substring(result.Response.LastIndexOf(' ') + 1).Trim();
        }

        return result;
    }
}
