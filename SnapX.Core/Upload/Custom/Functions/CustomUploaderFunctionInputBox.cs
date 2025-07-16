// SPDX-License-Identifier: GPL-3.0-or-later


namespace SnapX.Core.Upload.Custom.Functions;

// Example: {inputbox}
// Example: {inputbox:title}
// Example: {inputbox:title|default text}
internal class CustomUploaderFunctionInputBox : CustomUploaderFunction
{
    public override string Name { get; } = "inputbox";

    public override string[] Aliases { get; } = ["prompt"];

    public override string? Call(ShareXCustomUploaderSyntaxParser parser, string?[] parameters)
    {
        throw new System.NotImplementedException();
    }
}
