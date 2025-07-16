// SPDX-License-Identifier: GPL-3.0-or-later


namespace SnapX.Core.CLI;
public record CLICommand(string? Command = null, string? Parameter = null)
{
    public string? Command { get; set; } = Command;
    public string? Parameter { get; set; } = Parameter;
    public bool IsCommand { get; set; } // Starts with hyphen?

    public bool CheckCommand(string command, StringComparison comparisonType = StringComparison.OrdinalIgnoreCase)
    {
        return !string.IsNullOrEmpty(Command) && Command.Equals(command, comparisonType);
    }

    public override string? ToString()
    {
        var text = "";

        if (IsCommand)
        {
            text += "-";
        }

        text += Command;

        if (!string.IsNullOrEmpty(Parameter))
        {
            text += " \"" + Parameter + "\"";
        }

        return text;
    }
}

