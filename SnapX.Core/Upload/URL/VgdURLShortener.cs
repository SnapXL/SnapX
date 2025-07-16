// SPDX-License-Identifier: GPL-3.0-or-later


using SnapX.Core.Upload.BaseServices;
using SnapX.Core.Upload.BaseUploaders;
using SnapX.Core.Upload.Utils;

namespace SnapX.Core.Upload.URL;

public class VgdURLShortenerService : URLShortenerService
{
    public override UrlShortenerType EnumValue => UrlShortenerType.VGD;

    public override bool CheckConfig(UploadersConfig config) => true;

    public override URLShortener CreateShortener(UploadersConfig config, TaskReferenceHelper taskInfo)
    {
        return new VgdURLShortener();
    }
}

public class VgdURLShortener : IsgdURLShortener
{
    protected override string? APIURL => "https://v.gd/create.php";
}

