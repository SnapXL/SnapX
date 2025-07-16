// SPDX-License-Identifier: GPL-3.0-or-later


using SnapX.Core.Utils;

namespace SnapX.Core.Upload.File;

public class PomfUploader
{
    public string? UploadURL { get; set; }
    public string? ResultURL { get; set; }

    public PomfUploader()
    {
    }

    public PomfUploader(string? uploadURL, string? resultURL = null)
    {
        UploadURL = uploadURL;
        ResultURL = resultURL;
    }

    public override string? ToString()
    {
        return URLHelpers.GetHostName(UploadURL);
    }
}
