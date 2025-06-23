namespace DefaultNamespace;

public class ConsoleLogger : IBuildLogger
{
    private bool noColor;
    public void Error(string message)
    {
        if (!noColor) Console.ForegroundColor = ConsoleColor.Red;
        Console.Error.WriteLine($"ERROR: {message}");
        if (!noColor) Console.ResetColor();
    }

    public void Warning(string message)
    {
        if (!noColor) Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine($"WARNING: {message}");
        if (!noColor) Console.ResetColor();
    }

    public void Information(string message)
    {
        if (!noColor) Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine(message);
        if (!noColor) Console.ResetColor();
    }

    public void Debug(string message)
    {
        if (!noColor) Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.WriteLine($"DEBUG: {message}");
        if (!noColor) Console.ResetColor();
    }
}
