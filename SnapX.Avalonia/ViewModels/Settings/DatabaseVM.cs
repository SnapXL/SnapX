
using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using Dapper;
using Microsoft.Data.Sqlite;
using SnapX.Core;
using SnapX.Core.Utils.Extensions;

namespace SnapX.Avalonia.ViewModels;
// Source - https://stackoverflow.com/q/3931716
// Posted by c0D3l0g1c, modified by community. See post 'Timeline' for change history
// Retrieved 2026-01-30, License - CC BY-SA 2.5

public sealed class ObservableConcurrentCollection<T> : ICollection<T>, INotifyCollectionChanged, INotifyPropertyChanged
{
    private readonly ConcurrentQueue<T> _items = new();

    public int Count => _items.Count;

    public bool IsReadOnly => false;

    public void Add(T item)
    {
        _items.Enqueue(item);
        OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, item));
        OnPropertyChanged(nameof(Count));
    }

    public void Clear()
    {
        while (_items.TryDequeue(out _)) { }
        OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        OnPropertyChanged(nameof(Count));
    }

    public bool Contains(T item) => _items.Contains(item);

    public void CopyTo(T[] array, int arrayIndex) => _items.ToArray().CopyTo(array, arrayIndex);

    public bool Remove(T item)
    {
        // ConcurrentQueue has no Remove, fallback: rebuild queue
        bool removed = false;
        var temp = new ConcurrentQueue<T>();
        while (_items.TryDequeue(out var current))
        {
            if (!removed && EqualityComparer<T>.Default.Equals(current, item))
            {
                removed = true;
                continue;
            }
            temp.Enqueue(current);
        }
        while (temp.TryDequeue(out var v))
            _items.Enqueue(v);

        if (removed)
        {
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, item));
            OnPropertyChanged(nameof(Count));
        }

        return removed;
    }

    public IEnumerator<T> GetEnumerator() => _items.GetEnumerator();

    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();

    private void OnCollectionChanged(NotifyCollectionChangedEventArgs e) =>
        CollectionChanged?.Invoke(this, e);

    private void OnPropertyChanged(string name) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

    public event NotifyCollectionChangedEventHandler CollectionChanged;
    public event PropertyChangedEventHandler PropertyChanged;
}

public class DatabaseRow : INotifyPropertyChanged
{
    public IDictionary<string, object?> Columns { get; }

    public DatabaseRow(IDictionary<string, object?> columns)
    {
        Columns = columns;
    }

    public object? this[string columnName]
    {
        get => Columns.TryGetValue(columnName, out var value) ? value : null;
        set
        {
            Columns[columnName] = value;
            OnPropertyChanged($"[{columnName}]");
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged(string name) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}

public partial class DatabaseVM(SqliteConnection Connection) : ViewModelBase
{
    public ObservableConcurrentCollection<DatabaseRow> Items { get; } = new();
    public HashSet<string> AllColumnNames { get; } = new(StringComparer.OrdinalIgnoreCase);
    [ObservableProperty] private DatabaseRow? _selectedItem;

    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty] private ObservableCollection<string> _tables = [];
    [ObservableProperty] private string _selectedTable = "HistoryItems";
    [ObservableProperty] private string _rawSqlQuery = "SELECT * FROM HistoryItems";
    [ObservableProperty] private long _totalCount;
    [RelayCommand]
    private async Task Refresh()
    {
        await LoadData();
    }
    public record PathReplaceResult(string OldBase, string NewBase);
    public class PathReplaceRequestMessage : AsyncRequestMessage<PathReplaceResult?>
    {
        public string Title { get; init; } = "Replace Path Bases";
    }
    public record PathPreviewItem(string OldPath, string NewPath);

    public class PathUpdateConfirmationMessage : AsyncRequestMessage<bool>
    {
        public List<PathPreviewItem> Previews { get; init; } = new();
        public int TotalCount { get; init; }
    }
    [DapperAot]
    [RelayCommand]
    private async Task ChangePath()
    {
        var result = await WeakReferenceMessenger.Default.Send(new PathReplaceRequestMessage());
        if (result == null || string.IsNullOrWhiteSpace(result.OldBase) || string.IsNullOrWhiteSpace(result.NewBase)) return;

        var updates = new List<(DatabaseRow Row, string OldPath, string NewPath, object Id)>();
        var previewList = new List<PathPreviewItem>();

        foreach (var row in Items)
        {
            if (row.Columns.TryGetValue("FilePath", out var currentPathObj) && currentPathObj is string currentPath)
            {
                if (currentPath.StartsWith(result.OldBase, StringComparison.Ordinal))
                {
                    if (row.Columns.TryGetValue("Id", out var id))
                    {
                        string relativePath = Path.GetRelativePath(result.OldBase, currentPath);
                        string newFilePath = Path.Combine(result.NewBase, relativePath);

                        updates.Add((row, currentPath, newFilePath, id));

                        if (previewList.Count < 20)
                            previewList.Add(new PathPreviewItem(currentPath, newFilePath));
                    }
                }
            }
        }

        if (updates.Count == 0)
        {
            DebugHelper.Logger?.Warning("No files found matching the base path: {OldBase}", result.OldBase);
            return;
        }

        bool confirmed = await WeakReferenceMessenger.Default.Send(new PathUpdateConfirmationMessage
        {
            Previews = previewList,
            TotalCount = updates.Count
        });

        if (!confirmed)
        {
            DebugHelper.Logger?.Information("User aborted path update after preview.");
            return;
        }

        DebugHelper.Logger?.Information("Mass update confirmed. Modifying {Count} records.", updates.Count);

        await using var transaction = await Connection.BeginTransactionAsync();
        try
        {
            foreach (var update in updates)
            {
                update.Row["FilePath"] = update.NewPath;
                DebugHelper.Logger?.Debug("Updating Row ID {Id}: Changing path {OldPath} to {NewPath}", update.Id, update.OldPath, update.NewPath);
                await Connection.ExecuteAsync(
                    "UPDATE HistoryItems SET FilePath = @Path WHERE Id = @Id",
                    new { Path = update.NewPath, Id = update.Id },
                    transaction);
            }
            await transaction.CommitAsync();
            DebugHelper.Logger?.Information("Successfully committed {Count} path changes.", updates.Count);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            DebugHelper.Logger?.Error(ex, "Transaction failed. Database rolled back to original state.");
            ex.ShowError();
        }
    }
    [DapperAot]
    [RelayCommand]
    public async Task ExecuteRawSql(string? query)
    {
        if (string.IsNullOrWhiteSpace(query))
            query = $"SELECT * FROM [{SelectedTable}]";

        var parameters = new Dictionary<string, object?>(); // No search params for raw query
        await ExecuteRawSqlInternal(query, parameters);
    }

    private async Task ExecuteRawSqlInternal(string query, IDictionary<string, object?>? parameters = null)
    {
        try
        {
            Items.Clear();
            TotalCount = 0;

            await using var cmd = Connection.CreateCommand();
            cmd.CommandText = query;

            if (parameters != null)
            {
                foreach (var kvp in parameters)
                {
                    var param = cmd.CreateParameter();
                    param.ParameterName = kvp.Key;
                    param.Value = kvp.Value ?? DBNull.Value;
                    cmd.Parameters.Add(param);
                }
            }

            await using var reader = await cmd.ExecuteReaderAsync();
            var newItems = new ObservableConcurrentCollection<DatabaseRow>();
            var uniqueColumns = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            while (await reader.ReadAsync())
            {
                var dict = new Dictionary<string, object?>();
                for (int i = 0; i < reader.FieldCount; i++)
                {
                    var name = reader.GetName(i);
                    var value = await reader.IsDBNullAsync(i) ? null : reader.GetValue(i);
                    dict[name] = value;
                    uniqueColumns.Add(name);
                }

                newItems.Add(new DatabaseRow(dict));
            }

            foreach (var row in newItems)
                Items.Add(row);

            AllColumnNames.Clear();
            foreach (var col in uniqueColumns)
                AllColumnNames.Add(col);

            if (!string.IsNullOrEmpty(SelectedTable))
            {
                await using var countCmd = Connection.CreateCommand();
                countCmd.CommandText = $"SELECT COUNT(*) FROM [{SelectedTable}]";
                TotalCount = (long)await countCmd.ExecuteScalarAsync();
            }
        }
        catch (Exception ex)
        {
            ex.ShowError();
        }
    }







    [RelayCommand]
    private async Task RebuildIndex()
    {
        // Database maintenance logic
        await Task.CompletedTask;
    }

    [RelayCommand]
    private async Task Export()
    {
        // CSV/JSON Export logic
        await Task.CompletedTask;
    }

    [RelayCommand]
    private async Task Purge()
    {
        // Logic to check System.IO.File.Exists for each record and remove if false
        await Task.CompletedTask;
    }

    partial void OnSearchTextChanged(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            ExecuteRawSql($"SELECT * FROM [{SelectedTable}]");
            return;
        }

        if (AllColumnNames.Count <= 0)
        {
            TotalCount = 0;
            return;
        }

        var whereClauses = AllColumnNames
            .Select((col, idx) => $"[{col}] LIKE @p{idx}")
            .ToArray();

        var filteredQuery = $"SELECT * FROM [{SelectedTable}] WHERE {string.Join(" OR ", whereClauses)}";

        var parameters = new Dictionary<string, object?>();
        for (int i = 0; i < AllColumnNames.Count; i++)
            parameters[$"p{i}"] = $"%{value}%";

        ExecuteRawSqlInternal(filteredQuery, parameters);
    }

    partial void OnSelectedTableChanged(string? value)
    {
        if (value != null)
        {
            SelectedTable = value;
            ExecuteRawSql($"SELECT * FROM [{value}] LIMIT 1000");
        }
    }

    [DapperAot]
    private async Task LoadData()
    {
        var tableNames = await Connection.QueryAsync<string>(
            "SELECT name FROM sqlite_master WHERE type='table' AND name NOT LIKE 'sqlite_%';");

        Tables = new ObservableCollection<string>(tableNames);
        SelectedTable = Tables.FirstOrDefault(t => t == "HistoryItems") ?? Tables.FirstOrDefault();
        if (!string.IsNullOrEmpty(SelectedTable))
        {
            await ExecuteRawSql($"SELECT * FROM {SelectedTable} LIMIT 1000");
        }
    }
}
