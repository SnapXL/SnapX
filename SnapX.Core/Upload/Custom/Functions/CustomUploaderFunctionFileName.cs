// SPDX-License-Identifier: GPL-3.0-or-later


using SnapX.Core.Utils;

namespace SnapX.Core.Upload.Custom.Functions;

// Example: {filename}
internal class CustomUploaderFunctionFileName : CustomUploaderFunction
{
    public override string Name { get; } = "filename";

    public override string? Call(ShareXCustomUploaderSyntaxParser parser, string?[] parameters)
    {
        if (parser.URLEncode)
        {
            return URLHelpers.URLEncode(parser.FileName);
        }

        return parser.FileName;
    }
}
