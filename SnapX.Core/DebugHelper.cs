// SPDX-License-Identifier: GPL-3.0-or-later


using Serilog;
using Serilog.Events;
using Serilog.Sinks.InMemory;
using Serilog.Sinks.SystemConsole.Themes;

namespace SnapX.Core;

public static class DebugHelper
{
    public static ILogger? Logger { get; private set; }
    private static List<string?> messageBuffer = new();
    private static InMemorySink inMemorySink = new();
    public static IEnumerable<LogEvent> LogEvents => inMemorySink.LogEvents;
    public static void Init(string logFilePath)
    {
        if (string.IsNullOrEmpty(logFilePath)) return;
        var ConsoleLogLevel = LogEventLevel.Information;
#if DEBUG
        ConsoleLogLevel = LogEventLevel.Debug;
#endif
        if (SnapX.IsCLI)
        {
            #if !DEBUG
            ConsoleLogLevel = LogEventLevel.Warning;
            #endif
        }
        var loggerConfig = new LoggerConfiguration()
#if DEBUG
            .MinimumLevel.Debug()
#endif
            .WriteTo.Sink(inMemorySink)
            // If you run multiple SnapX instances, this will be the first to break. :)
            .WriteTo.Async(a => a.File(logFilePath, outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}]: {Message:lj}{NewLine}{Exception}", rollingInterval: RollingInterval.Day, buffered: !SnapX.IsCLI))
            .WriteTo.Console(theme: AnsiConsoleTheme.Sixteen, restrictedToMinimumLevel: ConsoleLogLevel);

        if (SnapX.Configuration != null)
        {
            loggerConfig.ReadFrom.Configuration(SnapX.Configuration);
        }
        Logger = loggerConfig.CreateLogger();
        Log.Logger = Logger;
    }

    public static void WriteLine(string? message = "")
    {
        if (Logger != null)
        {
            foreach (var bufferedMessage in messageBuffer)
            {
                Logger.Information(bufferedMessage);
            }
            Logger.Information(message);

            messageBuffer.Clear();
        }
        else
        {
            messageBuffer.Add(message);
        }
    }
    /// <summary>
    /// Ensures a message is output regardless of the configured log level.
    /// Routes to the logger as Information in UI mode, or directly to the Standard Output in CLI mode.
    /// </summary>
    /// <param name="message">The text to display or log.</param>
    public static void WriteAlways(string? message = "")
    {
        if (Logger != null && !SnapX.IsCLI)
        {
            Logger.Information(message);
        }
        else
        {
            Console.WriteLine(message);
        }
    }
    public static void FlushBufferedMessages()
    {
        foreach (var bufferedMessage in messageBuffer)
        {
            if (Logger is null && SnapX.IsCLI)
            {
                Console.WriteLine(bufferedMessage);
            }
            else
            {
                Logger.Information(bufferedMessage);
            }
        }

        messageBuffer.Clear();
    }
    public static void WriteLine(string format, params object[] args)
    {
        WriteLine(string.Format(format, args));
    }
    public static void WriteException(string exception, string message = "Exception")
    {
        if (Logger != null)
        {
            Logger.Error(exception);
        }
        else
        {
            Console.Error.WriteLine($"{message} - {exception}");
        }
    }

    public static void WriteException(Exception exception, string message = "Exception")
    {
        if (!FeatureFlags.DisableTelemetry) SentrySdk.CaptureException(exception);
        WriteException(exception.ToString(), message);
    }
}

