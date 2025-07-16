// SPDX-License-Identifier: GPL-3.0-or-later


namespace SnapX.Core.Upload.Custom.Functions;

// Example: {header:Location}
internal class CustomUploaderFunctionHeader : CustomUploaderFunction
{
    public override string Name { get; } = "header";

    public override int MinParameterCount { get; } = 1;

    public override string? Call(ShareXCustomUploaderSyntaxParser parser, string?[] parameters)
    {
        string? header = parameters[0];

        if (parser.ResponseInfo.Headers != null && parser.ResponseInfo.Headers.ContainsKey(header))
        {
            var headerValues = parser.ResponseInfo.Headers[header];
            return headerValues.FirstOrDefault(); // Returns the first value or null if the list is empty
        }
        return null;
    }
}
