// SPDX-License-Identifier: GPL-3.0-or-later


namespace SnapX.Core.Upload.Custom.Functions;

internal abstract class CustomUploaderFunction
{
    public abstract string Name { get; }

    public virtual string[]? Aliases { get; }

    public virtual int MinParameterCount { get; } = 0;

    public abstract string? Call(ShareXCustomUploaderSyntaxParser parser, string?[] parameters);
}
