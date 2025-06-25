
// SPDX-License-Identifier: GPL-3.0-or-later


using System.Xml.Linq;
using SnapX.Core.Upload.BaseUploaders;
using SnapX.Core.Utils;
using SnapX.Core.Utils.Extensions;

namespace SnapX.Core.Upload.File;

public sealed class DropIO : FileUploader
{
    public string? DropName { get; set; }
    public string? DropDescription { get; set; }

    public class Asset
    {
        public string? Name { get; set; }
        public string? OriginalFilename { get; set; }
    }

    public class Drop
    {
        public string? Name { get; set; }
        public string? AdminToken { get; set; }
    }

    private string APIKey;

    public DropIO(string apiKey)
    {
        APIKey = apiKey;
    }

    public override UploadResult Upload(Stream stream, string? fileName)
    {
        DropName = "ShareX_" + Helpers.GetRandomAlphanumeric(10);
        DropDescription = "";
        var drop = CreateDrop(DropName, DropDescription, false, false, false);

        var args = new Dictionary<string, string?>
        {
            { "version", "2.0" },
            { "api_key", APIKey },
            { "format", "xml" },
            { "token", drop.AdminToken },
            { "drop_name", drop.Name }
        };

        var result = SendRequestFile("https://assets.drop.io/upload", stream, fileName, "file", args);

        if (result.IsSuccess)
        {
            var asset = ParseAsset(result.Response);
            result.URL = string.Format("https://drop.io/{0}/asset/{1}", drop.Name, asset.Name);
        }

        return result;
    }

    public Asset ParseAsset(string? response)
    {
        var doc = XDocument.Parse(response);
        var root = doc.Element("asset");
        if (root != null)
        {
            var asset = new Asset();
            asset.Name = root.GetElementValue("name");
            asset.OriginalFilename = root.GetElementValue("original-filename");
            return asset;
        }

        return null;
    }

    private Drop CreateDrop(string? name, string? description, bool guests_can_comment, bool guests_can_add, bool guests_can_delete)
    {
        var args = new Dictionary<string, string?>
        {
            { "version", "2.0" },
            { "api_key", APIKey },
            { "format", "xml" },
            // this is the name of the drop and will become part of the URL of the drop
            { "name", name },
            // a plain text description of a drop
            { "description", description },
            // determines whether guests can comment on assets
            { "guests_can_comment", guests_can_comment.ToString() },
            // determines whether guests can add assets
            { "guests_can_add", guests_can_add.ToString() },
            // determines whether guests can delete assets
            { "guests_can_delete", guests_can_delete.ToString() }
        };

        var response = SendRequestMultiPart("https://api.drop.io/drops", args);

        var doc = XDocument.Parse(response);
        var root = doc.Element("drop");
        if (root != null)
        {
            var drop = new Drop();
            drop.Name = root.GetElementValue("name");
            drop.AdminToken = root.GetElementValue("admin_token");
            return drop;
        }

        return null;
    }
}
