
// SPDX-License-Identifier: GPL-3.0-or-later


using System.Text;

namespace SnapX.Core.CLI;
public class CLIManager
{
    public string?[] Arguments { get; private set; }
    public List<CLICommand> Commands { get; private set; }
    public List<CLICommandAction> Actions { get; private set; }

    public CLIManager()
    {
        Commands = [];
        Actions = [];
    }

    public CLIManager(string?[] arguments) : this()
    {
        Arguments = arguments;
    }

    // public CLIManager(string arguments) : this()
    // {
    //     Arguments = ParseCLI(arguments);
    // }

    public bool ParseCommands()
    {
        try
        {
            CLICommand lastCommand = null;

            foreach (string? argument in Arguments)
            {
                if (lastCommand == null || argument[0] == '-')
                {
                    CLICommand command = new CLICommand();

                    if (argument[0] == '-')
                    {
                        command.IsCommand = true;
                        command.Command = argument.Substring(1);
                        lastCommand = command;
                    }
                    else
                    {
                        command.Command = argument;
                    }

                    Commands.Add(command);
                }
                else
                {
                    lastCommand.Parameter = argument;
                    lastCommand = null;
                }
            }

            return true;
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }

        return false;
    }

    private string[] ParseCLI(string arguments)
    {
        List<string> commands = [];

        bool inDoubleQuotes = false;

        for (int i = 0, start = 0; i < arguments.Length; i++)
        {
            if ((!inDoubleQuotes && char.IsWhiteSpace(arguments[i])) || (inDoubleQuotes && arguments[i] == '"'))
            {
                string command = arguments.Substring(start, i - start);

                if (!string.IsNullOrEmpty(command))
                {
                    commands.Add(command);
                }

                if (inDoubleQuotes) inDoubleQuotes = false;
                start = i + 1;
            }
            else if (arguments[i] == '"')
            {
                inDoubleQuotes = true;
                start = i + 1;
            }
        }

        return commands.ToArray();
    }

    public bool IsCommandExist(params string[] commands)
    {
        if (Commands != null && commands != null)
        {
            foreach (string command in commands.Where(x => !string.IsNullOrEmpty(x)))
            {
                string command1 = command;

                if (command1[0] == '-')
                {
                    command1 = command1.Substring(1);
                }

                foreach (CLICommand command2 in Commands.Where(x => x != null && x.IsCommand))
                {
                    if (command2.CheckCommand(command1))
                    {
                        return true;
                    }
                }
            }
        }

        return false;
    }

    public CLICommand GetCommand(string command)
    {
        return Commands.Find(x => x.CheckCommand(command));
    }

    public string? GetParameter(string command)
    {
        CLICommand cliCommand = GetCommand(command);

        if (cliCommand != null)
        {
            return cliCommand.Parameter;
        }

        return null;
    }

    public void ExecuteActions()
    {
        foreach (CLICommandAction action in Actions)
        {
            action.CheckCommands(Commands);
        }
    }

    public override string ToString()
    {
        StringBuilder sb = new StringBuilder();

        foreach (CLICommand command in Commands)
        {
            if (sb.Length > 0)
            {
                sb.AppendLine();
            }

            sb.Append(command);
        }

        return sb.ToString();
    }
}

