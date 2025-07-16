// SPDX-License-Identifier: GPL-3.0-or-later


namespace SnapX.Core.Upload.Utils;

public class Argument
{
    public string Name { get; set; }
    public string Value { get; set; }

    public Argument()
    {
    }

    public Argument(string name, string value)
    {
        Name = name;
        Value = value;
    }
}

