
// SPDX-License-Identifier: GPL-3.0-or-later


using System.Net;
using System.Text;
using SnapX.Core.Utils;

namespace SnapX.Core.Upload.Utils;

public class ResponseInfo
{
    public HttpStatusCode StatusCode { get; set; }
    public string StatusDescription { get; set; }
    public bool IsSuccess => WebHelpers.IsSuccessStatusCode(StatusCode);
    public string? ResponseURL { get; set; }
    public Dictionary<string, string> Headers { get; set; }
    public string? ResponseText { get; set; }

    public string ToReadableString(bool includeResponseText)
    {
        var sbResponseInfo = new StringBuilder();

        sbResponseInfo.AppendLine($"Status code: ({(int)StatusCode}) {StatusDescription}");

        if (!string.IsNullOrEmpty(ResponseURL))
        {
            sbResponseInfo.AppendLine().AppendLine($"Response URL: {ResponseURL}");
        }

        if (Headers?.Count > 0)
        {
            var headerString = string.Join(Environment.NewLine, Headers.Select(h => $"{h.Key}: {h.Value}"));
            sbResponseInfo.AppendLine().AppendLine("Headers:").Append(headerString).Append(Environment.NewLine);
        }

        if (includeResponseText && !string.IsNullOrEmpty(ResponseText))
        {
            sbResponseInfo.AppendLine().AppendLine("Response text:").Append(ResponseText);
        }

        return sbResponseInfo.ToString();
    }
}

