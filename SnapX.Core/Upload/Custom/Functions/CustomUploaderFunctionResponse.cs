// SPDX-License-Identifier: GPL-3.0-or-later


namespace SnapX.Core.Upload.Custom.Functions;

// Example: {response}
internal class CustomUploaderFunctionResponse : CustomUploaderFunction
{
    public override string Name { get; } = "response";

    public override string? Call(ShareXCustomUploaderSyntaxParser parser, string?[] parameters)
    {
        return parser.ResponseInfo.ResponseText;
    }
}
