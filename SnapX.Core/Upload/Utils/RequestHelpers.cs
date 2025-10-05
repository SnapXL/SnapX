
// SPDX-License-Identifier: GPL-3.0-or-later


using System.Collections.Specialized;
using SnapX.Core.Utils.Cryptographic;

namespace SnapX.Core.Upload.Utils;

internal static class RequestHelpers
{
    public const string ContentTypeMultipartFormData = "multipart/form-data";
    public const string ContentTypeJSON = "application/json";
    public const string ContentTypeXML = "application/xml";
    public const string ContentTypeURLEncoded = "application/x-www-form-urlencoded";
    public const string ContentTypeOctetStream = "application/octet-stream";

    public static string ResponseToString(HttpResponseMessage response)
    {
        if (response?.Content == null)
            return null;

        return response.Content.ReadAsStringAsync().ConfigureAwait(false).GetAwaiter().GetResult();
    }



    public static NameValueCollection CreateAuthenticationHeader(string username, string password)
    {
        var authorization = TranslatorHelper.TextToBase64($"{username}:{password}");
        return new NameValueCollection
        {
            ["Authorization"] = $"Basic {authorization}"
        };
    }
}

