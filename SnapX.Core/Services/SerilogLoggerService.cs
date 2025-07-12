using Microsoft.Extensions.Configuration;
using Serilog;
using Serilog.Context;
using Serilog.Sinks.SystemConsole.Themes;
using SnapX.Core.Interfaces;

namespace SnapX.Core.Services;
public class SerilogLogService : ILoggerService
{
    private readonly ILogger _logger;

    public SerilogLogService(
        string? logFilePath,
        bool logToConsole,
        IConfiguration? config = null)
    {
        var loggerConfig = new LoggerConfiguration();

#if DEBUG
        loggerConfig.MinimumLevel.Debug();
#else
        loggerConfig.MinimumLevel.Information();
#endif

        if (!string.IsNullOrWhiteSpace(logFilePath))
        {
            loggerConfig = loggerConfig
                .WriteTo.Async(a => a.File(
                    logFilePath,
                    rollingInterval: RollingInterval.Day,
                    buffered: true,
                    outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}]: {Message:lj}{NewLine}{Exception}"
                ));
        }

        if (logToConsole)
        {
            loggerConfig = loggerConfig
                .WriteTo.Console(theme: AnsiConsoleTheme.Sixteen);
        }

        if (config is not null)
        {
            loggerConfig.ReadFrom.Configuration(config);
        }

        _logger = loggerConfig.CreateLogger();
    }

    public SerilogLogService(ILogger Logger)
    {
        _logger = Logger;
    }
    public ILogScope BeginScope(string name)
    {
        return new SerilogLogScope(LogContext.PushProperty("Scope", name));
    }
    public void Debug(string messageTemplate, params object[] args) =>
        _logger.Debug(messageTemplate, args);

    public void Information(string messageTemplate, params object[] args) =>
        _logger.Information(messageTemplate, args);

    public void Warning(string messageTemplate, params object[] args) =>
        _logger.Warning(messageTemplate, args);

    public void Error(Exception exception, string messageTemplate, params object[] args) =>
        _logger.Error(exception, messageTemplate, args);

    public void Error(string messageTemplate, params object[] args) =>
        _logger.Error(messageTemplate, args);
    private class SerilogLogScope(IDisposable InnerScope) : ILogScope
    {
        public void Dispose()
        {
            InnerScope.Dispose();
        }
    }
}
