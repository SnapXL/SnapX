
// SPDX-License-Identifier: GPL-3.0-or-later


namespace SnapX.Core.CLI;
public class CLICommand
{
    public string? Command { get; set; }
    public string? Parameter { get; set; }
    public bool IsCommand { get; set; } // Starts with hyphen?

    public CLICommand(string? command = null, string? parameter = null)
    {
        Command = command;
        Parameter = parameter;
    }

    public bool CheckCommand(string command, StringComparison comparisonType = StringComparison.OrdinalIgnoreCase)
    {
        return !string.IsNullOrEmpty(Command) && Command.Equals(command, comparisonType);
    }

    public override string? ToString()
    {
        string? text = "";

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

