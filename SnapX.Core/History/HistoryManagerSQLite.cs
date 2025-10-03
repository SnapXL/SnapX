using System.Data;
using Dapper;
using Microsoft.Data.Sqlite;

namespace SnapX.Core.History;


public class HistoryManagerSQLite(SqliteConnection Connection) : HistoryManager(SnapX.DBPath)
{
    [DapperAot]
    protected override List<HistoryItem> Load(string? filePath)
    {
        const string sql = "SELECT * FROM HistoryItems";
        return Connection.Query<HistoryItem>(sql).AsList();
    }
    [DapperAot]
    public override List<HistoryItem> GetHistoryItems(int Items = int.MaxValue)
    {
        if (Connection.State == ConnectionState.Closed) return [];
        const string sql = "SELECT * FROM HistoryItems LIMIT @Items";
        return Connection.Query<HistoryItem>(sql, new { Items }).AsList();
    }

    [DapperAot]
    protected override bool Save(string? filePath, IEnumerable<HistoryItem> historyItems)
    {
        const string updateSql = @"
        UPDATE HistoryItems SET
            FileName     = @FileName,
            FilePath     = @FilePath,
            DateTime     = @DateTime,
            Type         = @Type,
            Host         = @Host,
            URL          = @URL,
            ThumbnailURL = @ThumbnailURL,
            DeletionURL  = @DeletionURL,
            ShortenedURL = @ShortenedURL,
            Hidden       = @Hidden
        WHERE Id = @Id;";
        using var connection = new SqliteConnection(Connection.ConnectionString);
        connection.Open();

        using var transaction = connection.BeginTransaction();
        try
        {
            foreach (var item in historyItems)
            {
                connection.Execute(updateSql, item, transaction);
            }

            transaction.Commit();
            return true;
        }
        catch (Exception ex)
        {
            DebugHelper.WriteLine($"Failed to update history: {ex.Message}");
            DebugHelper.WriteException(ex);
            transaction.Rollback();
            return false;
        }
    }

    [DapperAot]
    public override bool RemoveHistoryItem(HistoryItem historyItem)
    {
        var rowsAffected = Connection.Execute(
            "DELETE FROM HistoryItems WHERE Id = @Id",
            new { historyItem.Id });
        return rowsAffected > 0;
    }

    [DapperAot]
    public override HistoryItem UpdateHistoryItem(HistoryItem historyItem)
    {
        Connection.Execute("""
                            UPDATE HistoryItems
                            SET
                                FileName = @FileName,
                                FilePath = @FilePath,
                                DateTime = @DateTime,
                                Type = @Type,
                                Host = @Host,
                                URL = @URL,
                                ThumbnailURL = @ThumbnailURL,
                                DeletionURL = @DeletionURL,
                                ShortenedURL = @ShortenedURL,
                                Hidden = @Hidden
                            WHERE
                                Id = @Id;
                            """, historyItem);
        return Connection.QuerySingle<HistoryItem>(
            "SELECT * FROM HistoryItems WHERE Id = @Id",
            new { historyItem.Id }
        );
    }

    [DapperAot]
    protected override bool Append(string? filePath, IEnumerable<HistoryItem> historyItems)
    {
        // For each Append operation, we create a new connection for concurrency. SQLite handles the connections.
        using var connection = new SqliteConnection(Connection.ConnectionString);
        connection.Open();
        using var tx = connection.BeginTransaction();
        try
        {
            var processedHistoryItems = new List<HistoryItem>();
            foreach (var historyItem in historyItems)
            {

                try
                {
                    // I swear I'm not paranoid!!!
#pragma warning disable CS8073 // The result of the expression is always the same since a value of this type is never equal to 'null'
                    // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
                    if (historyItem.DateTime == null) historyItem.DateTime = DateTime.Now;
#pragma warning restore CS8073 // The result of the expression is always the same since a value of this type is never equal to 'null'
                    if (historyItem.DateTime > DateTime.Now)
                    {
                        DebugHelper.WriteLine($"SQLite: WARN Date '{historyItem.DateTime:D}' is in the future. System clock misconfigured?? Still saving it.");
                    }

                    if (historyItem.FilePath == null)
                    {
                        DebugHelper.WriteLine($"SQLite: WARN {historyItem.FileName ?? historyItem.FilePath} has a NULL FilePath. Still saving it.");
                    }

                    if (historyItem.FilePath != null && !File.Exists(historyItem.FilePath))
                    {
                        DebugHelper.WriteLine($"SQLite: WARN {historyItem.FileName ?? historyItem.FilePath} does not exist on the filesystem. Still saving it.");
                    }

                    var ShareXCreationDate = new DateTime(2007, 8, 22);
                    // This is an Easter egg
                    if (historyItem.DateTime < ShareXCreationDate)
                    {
                        DebugHelper.WriteLine($"SQLite: WARN Date '{historyItem.DateTime:D}' indicates that this screenshot was taken before ShareX's creation??? Still saving, but what the heck!");
                    }
                    historyItem.FileName ??= Path.GetFileName(historyItem.FilePath);

                    const string insertHistorySql = """
                    INSERT INTO HistoryItems
                    (FileName, FilePath, [DateTime], Type, Hidden, Host, URL, ThumbnailURL, DeletionURL, ShortenedURL)
                    VALUES
                    (@FileName, @FilePath, @DateTime, @Type, @Hidden, @Host, @URL, @ThumbnailURL, @DeletionURL, @ShortenedURL)
                    RETURNING *;
                """;
                    var processedHistoryItem = connection.QuerySingle<HistoryItem>(
                        insertHistorySql,
                        historyItem,
                        tx
                    );
                    processedHistoryItems.Add(processedHistoryItem);

                }
                catch (Exception ex)
                {
                    DebugHelper.WriteException(ex);
                }
            }

            var allTags = processedHistoryItems
                .SelectMany(h => h.Tags ?? Enumerable.Empty<HistoryItem.Tag>(),
                    (h, t) => new
                    {
                        HistoryItemId = h.Id,
                        t.Text,
                        t.WindowTitle,
                        t.ProcessName
                    });

            if (allTags.Any())
            {
                foreach (var tag in allTags)
                {
                    const string insertTagSql = """
                    INSERT INTO Tags
                    (HistoryItemId, Text, WindowTitle, ProcessName)
                    VALUES
                       (@HistoryItemId, @Text, @WindowTitle, @ProcessName);
                """;

                    connection.Execute(
                        insertTagSql,
                        tag, tx);
                }
            }

            tx.Commit();
            return true;
        }
        catch (Exception ex)
        {
            DebugHelper.WriteException(ex);
            tx.Rollback();
            return false;
        }
    }

}
