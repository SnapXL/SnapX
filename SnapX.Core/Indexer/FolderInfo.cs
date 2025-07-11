
// SPDX-License-Identifier: GPL-3.0-or-later


using static System.String;

namespace SnapX.Core.Indexer;

public record FolderInfo(string FolderPath)
{
    public string FolderPath { get; set; } = FolderPath;
    public List<FileInfo> Files { get; set; } = [];
    public List<FolderInfo> Folders { get; set; } = [];
    public long Size { get; private set; }
    public int TotalFileCount { get; private set; }
    public int TotalFolderCount { get; private set; }
    public FolderInfo? Parent { get; set; }

    public string FolderName => Path.GetFileName(FolderPath);

    public bool IsEmpty => TotalFileCount == 0 && TotalFolderCount == 0;

    public void Update()
    {
        Folders.ForEach(x => x.Update());
        Folders.Sort((x, y) => Compare(x.FolderName, y.FolderName, StringComparison.Ordinal));
        Size = Folders.Sum(x => x.Size) + Files.Sum(x => x.Length);
        TotalFileCount = Files.Count + Folders.Sum(x => x.TotalFileCount);
        TotalFolderCount = Folders.Count + Folders.Sum(x => x.TotalFolderCount);
    }
}

