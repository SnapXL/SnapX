// SPDX-License-Identifier: GPL-3.0-or-later


using static System.String;

namespace SnapX.Core.Indexer;

public abstract class Indexer(IndexerSettings IndexerSettings)
{
    protected IndexerSettings? settings = IndexerSettings;

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
        var folderInfo = new FolderInfo(folderPath);

        if (settings.MaxDepthLevel == 0 || level < settings.MaxDepthLevel)
        {
            try
            {
                var currentDirectoryInfo = new DirectoryInfo(folderPath);

                foreach (var directoryInfo in currentDirectoryInfo.EnumerateDirectories())
                {
                    if (settings.SkipHiddenFolders && directoryInfo.Attributes.HasFlag(FileAttributes.Hidden))
                    {
                        continue;
                    }

                    var subFolderInfo = GetFolderInfo(directoryInfo.FullName, level + 1);
                    folderInfo.Folders.Add(subFolderInfo);
                    subFolderInfo.Parent = folderInfo;
                }

                foreach (var fileInfo in currentDirectoryInfo.EnumerateFiles())
                {
                    if (settings.SkipHiddenFiles && fileInfo.Attributes.HasFlag(FileAttributes.Hidden))
                    {
                        continue;
                    }

                    folderInfo.Files.Add(fileInfo);
                }

                folderInfo.Files.Sort((x, y) => Compare(x.Name, y.Name, StringComparison.Ordinal));
            }
            catch (UnauthorizedAccessException)
            {
            }
        }

        return folderInfo;
    }
}

