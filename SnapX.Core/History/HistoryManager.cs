
// SPDX-License-Identifier: GPL-3.0-or-later


using SnapX.Core.Utils;

namespace SnapX.Core.History;

public abstract class HistoryManager
{
    public string? FilePath { get; private set; }
    public string? BackupFolder { get; set; }
    public bool CreateBackup { get; set; }
    public bool CreateWeeklyBackup { get; set; }

    public HistoryManager(string? filePath)
    {
        FilePath = filePath;
    }

    public virtual List<HistoryItem> GetHistoryItems(int Items = Int32.MaxValue)
    {
        try
        {
            return Load();
        }
        catch (Exception e)
        {
            DebugHelper.WriteException(e);
        }

        return [];
    }

    public virtual HistoryItem UpdateHistoryItem(HistoryItem historyItem)
    {
        var allHistoryItems = Load();

        var index = allHistoryItems.FindIndex(h => h.Id == historyItem.Id);
        if (index == -1)
        {
            throw new InvalidOperationException($"History item with ID {historyItem.Id} was not found.");
        }

        allHistoryItems[index] = historyItem;

        Save(FilePath, allHistoryItems);

        return historyItem;
    }
    public virtual bool RemoveHistoryItem(HistoryItem historyItem)
    {
        var allHistoryItems = Load();

        var index = allHistoryItems.FindIndex(h => h.Id == historyItem.Id);
        if (index == -1)
        {
            throw new InvalidOperationException($"History item with ID {historyItem.Id} was not found.");
        }

        allHistoryItems.RemoveAt(index);

        return Save(FilePath, allHistoryItems);
    }


    public virtual async Task<List<HistoryItem>> GetHistoryItemsAsync(int Items = int.MaxValue)
    {
        return await Task.Run(() => GetHistoryItems(Items));
    }

    public virtual bool AppendHistoryItem(HistoryItem historyItem)
    {
        return AppendHistoryItems([historyItem]);
    }

    public bool AppendHistoryItems(IEnumerable<HistoryItem> historyItems)
    {
        try
        {
            return Append(historyItems.Where(x => IsValidHistoryItem(x)));
        }
        catch (Exception e)
        {
            DebugHelper.WriteException(e);
        }

        return false;
    }

    private bool IsValidHistoryItem(HistoryItem historyItem)
    {
        return historyItem != null && !string.IsNullOrEmpty(historyItem.FileName) && historyItem.DateTime != DateTime.MinValue &&
            (!string.IsNullOrEmpty(historyItem.URL) || !string.IsNullOrEmpty(historyItem.FilePath));
    }

    protected List<HistoryItem> Load()
    {
        return Load(FilePath);
    }

    protected abstract List<HistoryItem> Load(string? filePath);

    protected bool Append(IEnumerable<HistoryItem> historyItems)
    {
        return Append(FilePath, historyItems);
    }

    protected abstract bool Append(string? filePath, IEnumerable<HistoryItem> historyItems);
    protected abstract bool Save(string? filePath, IEnumerable<HistoryItem> historyItems);

    protected void Backup(string filePath)
    {
        if (!string.IsNullOrEmpty(BackupFolder))
        {
            if (CreateBackup)
            {
                FileHelpers.CopyFile(filePath, BackupFolder);
            }

            if (CreateWeeklyBackup)
            {
                FileHelpers.BackupFileWeekly(filePath, BackupFolder);
            }
        }
    }

    public void Test(int itemCount)
    {
        Test(FilePath, itemCount);
    }

    public void Test(string? filePath, int itemCount)
    {
        HistoryItem historyItem = new HistoryItem()
        {
            FileName = "Example.png",
            FilePath = "/home/romvnly/Pictures/Mister_Brit.png",
            DateTime = DateTime.Now,
            Type = "Image",
            Host = "Imgur",
            URL = "https://brycen.app/Example.png",
            ThumbnailURL = "https://brycen.app/Example.png",
            DeletionURL = "https://brycen.app/Example.png",
            ShortenedURL = "https://brycen.app/Example.png"
        };

        HistoryItem[] historyItems = new HistoryItem[itemCount];
        for (int i = 0; i < itemCount; i++)
        {
            historyItems[i] = historyItem;
        }

        Thread.Sleep(1000);

        Append(filePath, historyItems);

        Load(filePath);
    }
}

