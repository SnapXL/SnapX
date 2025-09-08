using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization;
using Aptabase.Core;
using Dapper;
using Microsoft.Data.Sqlite;
using SnapX.Core.Utils;

namespace SnapX.Core;

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

public sealed class Telemetry(SqliteConnection Connection, AptabaseClient AptabaseClient)
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
                // Do not use DebugHelper.WriteException, that will call this function again!
                DebugHelper.WriteLine($"Telemetry Logger Error: {ex.Message}");
                DebugHelper.WriteLine(ex.ToString());
            }
        });
    }

    [RequiresUnreferencedCode("Uses reflection to access properties that may be removed by the trimmer.")]
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
