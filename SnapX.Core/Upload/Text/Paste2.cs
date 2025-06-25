
// SPDX-License-Identifier: GPL-3.0-or-later


using SnapX.Core.Upload.BaseServices;
using SnapX.Core.Upload.BaseUploaders;
using SnapX.Core.Upload.Utils;

namespace SnapX.Core.Upload.Text;

public class Paste2TextUploaderService : TextUploaderService
{
    public override TextDestination EnumValue => TextDestination.Paste2;

    public override bool CheckConfig(UploadersConfig config) => true;

    public override GenericUploader CreateUploader(UploadersConfig config, TaskReferenceHelper taskInfo)
    {
        var settings = new Paste2Settings()
        {
            TextFormat = taskInfo.TextFormat
        };

        return new Paste2(settings);
    }
}

public sealed class Paste2 : TextUploader
{
    private Paste2Settings settings;

    public Paste2()
    {
        settings = new Paste2Settings();
    }

    public Paste2(Paste2Settings settings)
    {
        this.settings = settings;
    }

    public override UploadResult UploadText(string? text, string? fileName)
    {
        var ur = new UploadResult();

        if (string.IsNullOrEmpty(text)) return ur;

        var arguments = new Dictionary<string, string?>
        {
            { "code", text },
            { "lang", settings.TextFormat },
            { "description", settings.Description },
            { "parent", string.Empty }
        };

        SendRequestMultiPart("https://paste2.org/", arguments);
        ur.URL = LastResponseInfo.ResponseURL;

        return ur;
    }
}

public class Paste2Settings
{
    public string? TextFormat { get; set; }

    public string? Description { get; set; }

    public Paste2Settings()
    {
        TextFormat = "text";
        Description = "";
    }
}

