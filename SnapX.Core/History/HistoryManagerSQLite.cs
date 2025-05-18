using Dapper;
using Microsoft.Data.Sqlite;

namespace SnapX.Core.History;


public class HistoryManagerSQLite : HistoryManager
{

    public HistoryManagerSQLite(SqliteConnection connection) : base(SnapX.DBPath)
    {
        _connection = connection;
    }
    private SqliteConnection _connection;
    [DapperAot]
    protected override List<HistoryItem> Load(string filePath)
    {
        const string sql = "SELECT * FROM HistoryItems";
        return _connection.Query<HistoryItem>(sql).AsList();
    }
    [DapperAot]
    public override List<HistoryItem> GetHistoryItems()
    {
        const string sql = "SELECT * FROM HistoryItems";
        return _connection.Query<HistoryItem>(sql).AsList();
    }
    [DapperAot]
    public override async Task<List<HistoryItem>> GetHistoryItemsAsync()
    {
        const string sql = "SELECT * FROM HistoryItems";
        return _connection.QueryAsync<HistoryItem>(sql).Result.ToList();
    }
    [DapperAot]
    protected override bool Append(string filePath, IEnumerable<HistoryItem> historyItems)
    {
        using var tx = _connection.BeginTransaction();
        try
        {
            const string insertHistorySql = @"
            INSERT INTO HistoryItems
                (Id, FileName, FilePath, DateTime, Type, Hidden, Host, URL, ThumbnailURL, DeletionURL, ShortenedURL)
            VALUES
                (@Id, @FileName, @FilePath, @DateTime, @Type, @Hidden, @Host, @URL, @ThumbnailURL, @DeletionURL, @ShortenedURL);";

            var insertHistoryItems = _connection.Execute(
                insertHistorySql,
                historyItems,
                transaction: tx
            );
            DebugHelper.WriteLine($"Insert History Items Returned {insertHistoryItems}");

            var allTags = historyItems
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
                const string insertTagSql = @"
                INSERT INTO HistoryItemTags
                    (HistoryItemId, Text, WindowTitle, ProcessName)
                VALUES
                    (@HistoryItemId, @Text, @WindowTitle, @ProcessName);";

                _connection.Execute(
                    insertTagSql,
                    allTags,
                    transaction: tx
                );
            }

            tx.Commit();
            return true;
        }
        catch
        {
            tx.Rollback();
            return false;
        }
    }

}
