using Serilog.Core;
using Serilog.Events;

namespace SnapX.Core;

public class ObservableSink(IFormatProvider? FormatProvider = null) : ILogEventSink
{
    private readonly List<LogEvent> _buffer = [];
    private readonly Lock _lock = new();

    public event EventHandler<LogEvent>? LogMessageReceived;

    public void Emit(LogEvent logEvent)
    {

        lock (_lock)
        {
            _buffer.Add(logEvent);
        }

        LogMessageReceived?.Invoke(this, logEvent);
    }

    public List<LogEvent> GetBufferedEvents()
    {
        lock (_lock)
        {
            return _buffer;
        }
    }
}
