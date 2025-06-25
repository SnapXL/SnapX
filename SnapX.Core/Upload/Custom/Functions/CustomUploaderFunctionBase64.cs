
// SPDX-License-Identifier: GPL-3.0-or-later


using SnapX.Core.Utils.Cryptographic;

namespace SnapX.Core.Upload.Custom.Functions;

// Example: Basic {base64:username:password}
internal class CustomUploaderFunctionBase64 : CustomUploaderFunction
{
    public override string Name { get; } = "base64";

    public override int MinParameterCount { get; } = 1;

    public override string? Call(ShareXCustomUploaderSyntaxParser parser, string?[] parameters)
    {
        string? text = parameters[0];

        if (!string.IsNullOrEmpty(text))
        {
            return TranslatorHelper.TextToBase64(text);
        }

        return null;
    }
}
