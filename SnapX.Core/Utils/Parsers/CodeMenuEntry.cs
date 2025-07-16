// SPDX-License-Identifier: GPL-3.0-or-later


namespace SnapX.Core.Utils.Parsers;
public abstract class CodeMenuEntry
{
    protected abstract string Prefix { get; }

    public string Value { get; private set; }
    public string Description { get; private set; }
    public string Category { get; private set; }

    public CodeMenuEntry(string value, string description, string category = null)
    {
        Value = value;
        Description = description;
        Category = category;
    }

    public string ToPrefixString()
    {
        return ToPrefixString(Prefix);
    }

    public string ToPrefixString(string prefix)
    {
        return prefix + Value;
    }
}

