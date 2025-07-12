using System.Text.Json;
using System.Text.Json.Serialization;
using Aptabase.Core;
using Dapper;
using Microsoft.Data.Sqlite;
using SnapX.Core.Interfaces;
using SnapX.Core.Utils;

namespace SnapX.Core;

[JsonSourceGenerationOptions(WriteIndented = true)]
[JsonSerializable(typeof(SentryEvent))]
[JsonSerializable(typeof(Sentry.Protocol.App))]
[JsonSerializable(typeof(Sentry.Protocol.OperatingSystem))]
[JsonSerializable(typeof(Sentry.Protocol.Device))]
[JsonSerializable(typeof(Sentry.Protocol.DeviceOrientation))]
[JsonSerializable(typeof(Sentry.Protocol.Gpu))]
[JsonSerializable(typeof(Sentry.Protocol.DebugImage))]
[JsonSerializable(typeof(List<Sentry.Protocol.DebugImage>))]
[JsonSerializable(typeof(Sentry.Protocol.SentryException))]
[JsonSerializable(typeof(Sentry.Protocol.Trace))]
[JsonSerializable(typeof(List<Sentry.Protocol.Trace>))]
[JsonSerializable(typeof(Sentry.Protocol.Runtime))]
[JsonSerializable(typeof(Sentry.Protocol.Response))]
[JsonSerializable(typeof(Sentry.Protocol.Measurement))]
[JsonSerializable(typeof(Sentry.Protocol.Mechanism))]
[JsonSerializable(typeof(SentryContexts))]
[JsonSerializable(typeof(SentryFeedback))]
[JsonSerializable(typeof(Breadcrumb))]
[JsonSerializable(typeof(SentryClient))]
[JsonSerializable(typeof(SentryContexts))]
[JsonSerializable(typeof(object[]))]
internal partial class SentryContext : JsonSerializerContext;

[JsonSourceGenerationOptions(WriteIndented = true)]
[JsonSerializable(typeof(Dictionary<string, object>))]
[JsonSerializable(typeof(int))]
[JsonSerializable(typeof(string))]
[JsonSerializable(typeof(bool))]
[JsonSerializable(typeof(long))]
[JsonSerializable(typeof(DateTime))]
[JsonSerializable(typeof(BuildType))]
[JsonSerializable(typeof(OsInfo.GenericGraphicsInfo))]
internal partial class AptabaseContext : JsonSerializerContext;

public sealed class Telemetry(SqliteConnection Connection, AptabaseClient AptabaseClient, ILoggerService Logger)
{
    [DapperAot]
    public void LogTelemetry(string provider, string eventName, string envelope)
    {
        ThreadPool.QueueUserWorkItem(_ =>
        {
            try
            {
                const string sql = @"
                INSERT INTO TelemetryLog (EventName, Provider, Envelope)
                VALUES (@EventName, @Provider, @Envelope);
            ";
                Connection.Execute(sql, new { EventName = eventName, Provider = provider, Envelope = envelope });
            }
            catch (Exception ex)
            {
                Logger.Error("Telemetry Logger Error: {ExMessage}", ex.ToString());
            }
        });
    }

    public void TrackEvent(string EventName, Dictionary<string, object>? Envelope = null)
    {
        if (string.IsNullOrWhiteSpace(EventName)) return;
        LogTelemetry("Aptabase", EventName, Envelope is not null ? JsonSerializer.Serialize(Envelope, new JsonSerializerOptions
        {
            TypeInfoResolver = AptabaseContext.Default
        }) : "{}");
        AptabaseClient.TrackEvent(EventName, Envelope);
    }
}
