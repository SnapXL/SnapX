
// SPDX-License-Identifier: GPL-3.0-or-later


using SnapX.Core.Upload.BaseServices;
using SnapX.Core.Upload.BaseUploaders;
using SnapX.Core.Upload.Utils;

namespace SnapX.Core.Upload.Text;

internal class PastieTextUploaderService : TextUploaderService
{
    public override TextDestination EnumValue => TextDestination.Pastie;

    public override bool CheckConfig(UploadersConfig config) => true;

    public override GenericUploader CreateUploader(UploadersConfig config, TaskReferenceHelper taskInfo)
    {
        return new Pastie()
        {
            IsPublic = config.PastieIsPublic
        };
    }
}

public sealed class Pastie : TextUploader
{
    public bool IsPublic { get; set; }

    public override UploadResult UploadText(string? text, string? fileName)
    {
        var ur = new UploadResult();

        if (string.IsNullOrEmpty(text))
            return ur;

        var arguments = new Dictionary<string, string?>
        {
            { "paste[body]", text },
            { "paste[restricted]", IsPublic ? "0" : "1" },
            { "paste[authorization]", "burger" }
        };

        SendRequestURLEncoded(HttpMethod.Post, "http://pastie.org/pastes", arguments);

        ur.URL = LastResponseInfo?.ResponseURL;

        return ur;
    }
}

