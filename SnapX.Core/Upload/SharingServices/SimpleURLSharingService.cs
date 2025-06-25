
// SPDX-License-Identifier: GPL-3.0-or-later


using SnapX.Core.Upload.BaseServices;
using SnapX.Core.Upload.BaseUploaders;
using SnapX.Core.Upload.Utils;
using SnapX.Core.Utils;

namespace SnapX.Core.Upload.SharingServices;

public abstract class SimpleURLSharingService : URLSharingService
{
    protected abstract string URLFormatString { get; }

    public override URLSharer CreateSharer(UploadersConfig config, TaskReferenceHelper taskInfo)
    {
        return new SimpleURLSharer(URLFormatString);
    }

    public override bool CheckConfig(UploadersConfig config) => true;
}

public sealed class SimpleURLSharer : URLSharer
{
    public string URLFormatString { get; private set; }

    public SimpleURLSharer(string urlFormatString)
    {
        URLFormatString = urlFormatString;
    }

    public override UploadResult ShareURL(string? url)
    {
        var result = new UploadResult { URL = url, IsURLExpected = false };

        var encodedURL = URLHelpers.URLEncode(url);
        var resultURL = string.Format(URLFormatString, encodedURL);
        URLHelpers.OpenURL(resultURL);

        return result;
    }
}

