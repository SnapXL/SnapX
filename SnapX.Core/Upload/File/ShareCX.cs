// SPDX-License-Identifier: GPL-3.0-or-later


using System.Text.RegularExpressions;
using SnapX.Core.Upload.BaseUploaders;

namespace SnapX.Core.Upload.File;

public sealed class ShareCX : FileUploader
{
    public override UploadResult Upload(Stream stream, string? fileName)
    {
        var result = SendRequestFile("https://file1.share.cx/cgi-bin/upload.cgi", stream, fileName, "file_0");

        if (result.IsSuccess)
        {
            MatchCollection matches = Regex.Matches(result.Response, "(?<=value=\")http:.+?(?=\".*></td>)");

            if (matches.Count == 2)
            {
                result.URL = matches[0].Value;
                result.DeletionURL = matches[1].Value;
            }
        }

        return result;
    }
}
