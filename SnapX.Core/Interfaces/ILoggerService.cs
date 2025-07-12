using Serilog.Core;

namespace SnapX.Core.Interfaces;

public interface ILoggerService
{
    ILogScope BeginScope(string name);
    [MessageTemplateFormatMethod("messageTemplate")]
    void Debug(string messageTemplate, params object[] propertyValues);
    [MessageTemplateFormatMethod("messageTemplate")]
    void Information(string messageTemplate, params object[] propertyValues);
    [MessageTemplateFormatMethod("messageTemplate")]
    void Warning(string messageTemplate, params object[] propertyValues);
    [MessageTemplateFormatMethod("messageTemplate")]
    void Error(string messageTemplate, params object[] propertyValues);
    [MessageTemplateFormatMethod("messageTemplate")]
    void Error(Exception exception, string messageTemplate, params object[] propertyValues);
}

public interface ILogScope : IDisposable;
