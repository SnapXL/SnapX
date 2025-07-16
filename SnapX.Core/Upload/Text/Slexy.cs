// SPDX-License-Identifier: GPL-3.0-or-later


using System.ComponentModel;
using SnapX.Core.Upload.BaseServices;
using SnapX.Core.Upload.BaseUploaders;
using SnapX.Core.Upload.Utils;

namespace SnapX.Core.Upload.Text;

public class SlexyTextUploaderService : TextUploaderService
{
    public override TextDestination EnumValue => TextDestination.Slexy;

    public override bool CheckConfig(UploadersConfig config) => true;

    public override GenericUploader CreateUploader(UploadersConfig config, TaskReferenceHelper taskInfo)
    {
        SlexySettings settings = new SlexySettings()
        {
            TextFormat = taskInfo.TextFormat
        };

        return new Slexy(settings);
    }
}

public sealed class Slexy : TextUploader
{
    private const string? APIURL = "https://slexy.org/index.php/submit";

    private SlexySettings settings;

    public Slexy()
    {
        settings = new SlexySettings();
    }

    public Slexy(SlexySettings settings)
    {
        this.settings = settings;
    }

    public override UploadResult UploadText(string? text, string? fileName)
    {
        var ur = new UploadResult();

        if (string.IsNullOrEmpty(text))
            return ur;

        var arguments = new Dictionary<string, string?>
        {
            { "raw_paste", text },
            { "author", settings.Author },
            { "comment", "" },
            { "desc", settings.Description },
            { "expire", settings.Expiration },
            { "language", settings.TextFormat },
            { "linenumbers", settings.LineNumbers ? "1" : "0" },
            { "permissions", settings.Visibility == Privacy.Private ? "1" : "0" },
            { "submit", "Submit Paste" },
            { "tabbing", "true" },
            { "tabtype", "real" }
        };

        SendRequestMultiPart(APIURL, arguments);

        ur.URL = LastResponseInfo?.ResponseURL;

        return ur;
    }
}

public class SlexySettings
{
    /// <summary>language</summary>
    public string? TextFormat { get; set; }

    /// <summary>author</summary>
    public string? Author { get; set; }

    /// <summary>permissions</summary>
    public Privacy Visibility { get; set; }

    /// <summary>desc</summary>
    public string? Description { get; set; }

    /// <summary>linenumbers</summary>
    public bool LineNumbers { get; set; }

    /// <summary>expire</summary>
    [Description("Expiration time with seconds. Example: 0 = Forever, 60 = 1 minutes, 3600 = 1 hour, 2592000 = 1 month")]
    public string? Expiration { get; set; }

    public SlexySettings()
    {
        TextFormat = "text";
        Author = "";
        Visibility = Privacy.Private;
        Description = "";
        LineNumbers = true;
        Expiration = "2592000";
    }
}

