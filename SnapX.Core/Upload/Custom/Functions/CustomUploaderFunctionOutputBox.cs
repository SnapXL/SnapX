
// SPDX-License-Identifier: GPL-3.0-or-later


namespace SnapX.Core.Upload.Custom.Functions;

// Example: {outputbox:text}
// Example: {outputbox:title|text}
internal class CustomUploaderFunctionOutputBox : CustomUploaderFunction
{
    public override string Name { get; } = "outputbox";

    public override int MinParameterCount { get; } = 1;

    public override string? Call(ShareXCustomUploaderSyntaxParser parser, string?[] parameters)
    {
        string? text;
        string? title = null;

        if (parameters.Length > 1)
        {
            title = parameters[0];
            text = parameters[1];
        }
        else
        {
            text = parameters[0];
        }

        if (!string.IsNullOrEmpty(text))
        {
            if (string.IsNullOrEmpty(title))
            {
                title = "Output";
            }
            throw new NotImplementedException("Custom uploader function outputbox function parameters are not implemented.");
        }

        return null;
    }
}
