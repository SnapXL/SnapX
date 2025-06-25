
// SPDX-License-Identifier: GPL-3.0-or-later


using System.Xml.Linq;
using SnapX.Core.Upload.BaseUploaders;
using SnapX.Core.Utils.Extensions;

namespace SnapX.Core.Upload.File;

public class FileSonic : FileUploader
{
    public string Username { get; set; }

    public string? Password { get; set; }

    private const string? APIURL = "https://api.filesonic.com/upload";

    public FileSonic(string username, string? password)
    {
        Username = username;
        Password = password;
    }

    public override UploadResult Upload(Stream stream, string? fileName)
    {
        UploadResult result = null;

        var url = GetUploadURL();

        if (!string.IsNullOrEmpty(url))
        {
            result = SendRequestFile(url, stream, fileName, "file");

            if (!string.IsNullOrEmpty(result.Response))
            {
                result.URL = result.Response;
            }
        }
        else
        {
            Errors.Add("GetUploadURL failed.");
        }

        return result;
    }

    public string? GetUploadURL()
    {
        var args = new Dictionary<string, string?>
        {
            { "method", "getUploadUrl" },
            { "format", "xml" },
            { "u", Username },
            { "p", Password }
        };

        var response = SendRequest(HttpMethod.Get, APIURL, args);

        var xd = XDocument.Parse(response);
        return xd.GetValue("FSApi_Upload/getUploadUrl/response/url");
    }
}
