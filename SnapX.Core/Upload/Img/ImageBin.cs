// SPDX-License-Identifier: GPL-3.0-or-later


using System.Text.RegularExpressions;
using SnapX.Core.Upload.BaseUploaders;

namespace SnapX.Core.Upload.Img;

public sealed class ImageBin : ImageUploader
{

    public override UploadResult Upload(Stream stream, string? fileName)
    {
        var arguments = new Dictionary<string, string?>
        {
            { "t", "file" },
            { "name", "SnapX" },
            { "tags", "SnapX" },
            { "description", "test" },
            { "adult", "t" },
            { "sfile", "Upload" },
            { "url", "" }
        };

        var result = SendRequestFile("https://imagebin.ca/upload.php", stream, fileName, "f", arguments);

        if (!result.IsSuccess)
        {
            return result;
        }

        var match = Regex.Match(result.Response, @"(?<=ca/view/).+(?=\.html'>)", RegexOptions.Compiled);
        if (!match.Success) return result;

        var imageUrl = $"https://imagebin.ca/img/{match.Value}{Path.GetExtension(fileName)}";
        result.URL = imageUrl;

        return result;
    }
}

