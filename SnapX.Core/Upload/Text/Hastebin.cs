// SPDX-License-Identifier: GPL-3.0-or-later



using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization;
using SnapX.Core.Upload.BaseServices;
using SnapX.Core.Upload.BaseUploaders;
using SnapX.Core.Upload.Utils;
using SnapX.Core.Utils;

namespace SnapX.Core.Upload.Text;

public class HastebinTextUploaderService : TextUploaderService
{
    public override TextDestination EnumValue => TextDestination.Hastebin;
    public override bool CheckConfig(UploadersConfig config) => true;

    public override GenericUploader CreateUploader(UploadersConfig config, TaskReferenceHelper taskInfo)
    {
        return new Hastebin()
        {
            CustomDomain = config.HastebinCustomDomain,
            SyntaxHighlighting = config.HastebinSyntaxHighlighting,
            UseFileExtension = config.HastebinUseFileExtension
        };
    }
}
[JsonSerializable(typeof(Hastebin.HastebinResponse))]
internal partial class HastebinContext : JsonSerializerContext;
public sealed class Hastebin : TextUploader
{
    public string? CustomDomain { get; set; }
    public string SyntaxHighlighting { get; set; }
    public bool UseFileExtension { get; set; }

    [RequiresDynamicCode("Uploader")]
    [RequiresUnreferencedCode("Uploader")]
    public override UploadResult UploadText(string? text, string? fileName)
    {
        var ur = new UploadResult();
        if (string.IsNullOrEmpty(text)) return ur;
        var domain = !string.IsNullOrEmpty(CustomDomain) ? CustomDomain : "https://hastebin.com";

        ur.Response = SendRequest(HttpMethod.Post, URLHelpers.CombineURL(domain, "documents"), text);
        if (string.IsNullOrEmpty(ur.Response)) return ur;
        var options = new JsonSerializerOptions()
        {
            TypeInfoResolver = HastebinContext.Default
        };
        var response = JsonSerializer.Deserialize<HastebinResponse>(ur.Response, options);
        if (response == null || string.IsNullOrEmpty(response.Key)) return ur;

        var url = URLHelpers.CombineURL(domain, response.Key);

        var syntaxHighlighting = SyntaxHighlighting;

        if (UseFileExtension)
        {
            var ext = FileHelpers.GetFileNameExtension(fileName);

            if (!string.IsNullOrEmpty(ext) && !ext.Equals("txt", StringComparison.OrdinalIgnoreCase))
            {
                syntaxHighlighting = ext.ToLowerInvariant();
            }
        }

        if (!string.IsNullOrEmpty(syntaxHighlighting))
        {
            url += "." + syntaxHighlighting;
        }

        ur.URL = url;


        return ur;
    }

    public class HastebinResponse
    {
        public string? Key { get; set; }
    }
}

