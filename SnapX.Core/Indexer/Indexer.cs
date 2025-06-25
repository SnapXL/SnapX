
// SPDX-License-Identifier: GPL-3.0-or-later


namespace SnapX.Core.Indexer;

public abstract class Indexer
{
    protected IndexerSettings settings = null;
    protected Indexer(IndexerSettings indexerSettings)
    {
        settings = indexerSettings;
    }
    public string? Index(string? folderPath)
    {
        Indexer indexer = null;

        switch (settings.Output)
        {
            case IndexerOutput.Html:
                indexer = new IndexerHtml(settings);
                break;
            case IndexerOutput.Txt:
                indexer = new IndexerText(settings);
                break;
            case IndexerOutput.Xml:
                indexer = new IndexerXml(settings);
                break;
            case IndexerOutput.Json:
                indexer = new IndexerJson(settings);
                break;
        }

        return indexer?.Index(folderPath);
    }
    public static string? Index(string? folderPath, IndexerSettings settings)
    {
        Indexer indexer = null;

        switch (settings.Output)
        {
            case IndexerOutput.Html:
                indexer = new IndexerHtml(settings);
                break;
            case IndexerOutput.Txt:
                indexer = new IndexerText(settings);
                break;
            case IndexerOutput.Xml:
                indexer = new IndexerXml(settings);
                break;
            case IndexerOutput.Json:
                indexer = new IndexerJson(settings);
                break;
        }

        return indexer.Index(folderPath);
    }

    protected abstract void IndexFolder(FolderInfo dir, int level = 0);

    protected FolderInfo GetFolderInfo(string folderPath, int level = 0)
    {
        FolderInfo folderInfo = new FolderInfo(folderPath);

        if (settings.MaxDepthLevel == 0 || level < settings.MaxDepthLevel)
        {
            try
            {
                DirectoryInfo currentDirectoryInfo = new DirectoryInfo(folderPath);

                foreach (DirectoryInfo directoryInfo in currentDirectoryInfo.EnumerateDirectories())
                {
                    if (settings.SkipHiddenFolders && directoryInfo.Attributes.HasFlag(FileAttributes.Hidden))
                    {
                        continue;
                    }

                    FolderInfo subFolderInfo = GetFolderInfo(directoryInfo.FullName, level + 1);
                    folderInfo.Folders.Add(subFolderInfo);
                    subFolderInfo.Parent = folderInfo;
                }

                foreach (FileInfo fileInfo in currentDirectoryInfo.EnumerateFiles())
                {
                    if (settings.SkipHiddenFiles && fileInfo.Attributes.HasFlag(FileAttributes.Hidden))
                    {
                        continue;
                    }

                    folderInfo.Files.Add(fileInfo);
                }

                folderInfo.Files.Sort((x, y) => x.Name.CompareTo(y.Name));
            }
            catch (UnauthorizedAccessException)
            {
            }
        }

        return folderInfo;
    }
}

