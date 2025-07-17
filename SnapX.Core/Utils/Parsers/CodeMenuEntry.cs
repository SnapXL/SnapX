// SPDX-License-Identifier: GPL-3.0-or-later


namespace SnapX.Core.Utils.Parsers;
public abstract class CodeMenuEntry(string Value, string Description, string? Category = null)
{
    protected abstract string Prefix { get; }

    public string ToPrefixString()
    {
        return ToPrefixString(Prefix);
    }

    public string ToPrefixString(string prefix)
    {
        return prefix + Value;
    }
}

