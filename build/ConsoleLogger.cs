namespace DefaultNamespace;

public class ConsoleLogger(bool NoColor = false) : IBuildLogger
{
    public void Error(string message)
    {
        if (!NoColor) Console.ForegroundColor = ConsoleColor.Red;
        Console.Error.WriteLine($"ERROR: {message}");
        if (!NoColor) Console.ResetColor();
    }

    public void Warning(string message)
    {
        if (!NoColor) Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine($"WARNING: {message}");
        if (!NoColor) Console.ResetColor();
    }

    public void Information(string message)
    {
        if (!NoColor) Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine(message);
        if (!NoColor) Console.ResetColor();
    }

    public void Debug(string message)
    {
        if (!NoColor) Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.WriteLine($"DEBUG: {message}");
        if (!NoColor) Console.ResetColor();
    }
}
