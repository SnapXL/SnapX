// SPDX-License-Identifier: GPL-3.0-or-later


using System.ComponentModel;
using SnapX.Core.Upload.BaseUploaders;

namespace SnapX.Core.Upload.Text;

public sealed class Pastebin_ca : TextUploader
{
    private const string? APIURL = "https://pastebin.ca/quiet-paste.php";

    private string APIKey;

    private PastebinCaSettings settings;

    public Pastebin_ca(string apiKey)
    {
        APIKey = apiKey;
        settings = new PastebinCaSettings();
    }

    public Pastebin_ca(string apiKey, PastebinCaSettings settings)
    {
        APIKey = apiKey;
        this.settings = settings;
    }

    public override UploadResult UploadText(string? text, string? fileName)
    {
        var ur = new UploadResult();

        if (string.IsNullOrEmpty(text))
            return ur;

        var arguments = new Dictionary<string, string?>
        {
            { "api", APIKey },
            { "content", text },
            { "description", settings.Description },
            { "encryptpw", settings.EncryptPassword },
            { "expiry", settings.ExpireTime },
            { "name", settings.Author },
            { "s", "Submit Post" },
            { "tags", settings.Tags },
            { "type", settings.TextFormat }
        };

        if (settings.Encrypt)
        {
            arguments.Add("encrypt", "true");
        }

        ur.Response = SendRequestMultiPart(APIURL, arguments);

        if (string.IsNullOrEmpty(ur.Response))
            return ur;

        if (ur.Response.StartsWith("SUCCESS:"))
        {
            ur.URL = string.Concat("https://pastebin.ca/", ur.Response.AsSpan(8));
        }
        else if (ur.Response.StartsWith("FAIL:"))
        {
            Errors.Add(ur.Response.Substring(5));
        }

        return ur;
    }
}

public class PastebinCaSettings
{
    /// <summary>name</summary>
    [Description("Name / Title")]
    public string? Author { get; set; }

    /// <summary>description</summary>
    [Description("Description / Question")]
    public string? Description { get; set; }

    /// <summary>tags</summary>
    [Description("Tags (space separated, optional)")]
    public string? Tags { get; set; }

    /// <summary>type</summary>
    [Description("Content Type"), DefaultValue("1")]
    public string? TextFormat { get; set; }

    /// <summary>expiry</summary>
    [Description("Expire this post in ..."), DefaultValue("1 month")]
    public string? ExpireTime { get; set; }

    /// <summary>encrypt</summary>
    [Description("Encrypt this paste")]
    public bool Encrypt { get; set; }

    /// <summary>encryptpw</summary>
    public string? EncryptPassword { get; set; }

    public PastebinCaSettings()
    {
        Author = "";
        Description = "";
        Tags = "";
        TextFormat = "1";
        ExpireTime = "1 month";
        Encrypt = false;
        EncryptPassword = "";
    }
}
