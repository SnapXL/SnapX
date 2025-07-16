// SPDX-License-Identifier: GPL-3.0-or-later


namespace SnapX.Core.Upload.Custom.Functions;

// Example: {responseurl}
internal class CustomUploaderFunctionResponseURL : CustomUploaderFunction
{
    public override string Name { get; } = "responseurl";

    public override string? Call(ShareXCustomUploaderSyntaxParser parser, string?[] parameters)
    {
        return parser.ResponseInfo.ResponseURL;
    }
}
