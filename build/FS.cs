namespace DefaultNamespace;

public class FS(IBuildLogger Logger, CommandRunner CommandRunner) : IFileSystem
{
    public async Task TryDeleteFile(string path)
    {
        if (!File.Exists(path)) return;
        await CommandRunner.RunInstallCommand($"-f {path}", "rm");
        Logger.Information($"Removed file: {path}");
    }

    public async Task DeleteDirectory(string path)
    {
        if (!Directory.Exists(path)) return;
        await CommandRunner.RunInstallCommand(path, "rmdir");
    }

    public async Task TryDeleteEmptyDir(string path)
    {
        if (!Directory.Exists(path))
            return;

        foreach (var dir in Directory.EnumerateDirectories(path))
        {
            await TryDeleteEmptyDir(dir);
        }

        var isEmpty = !Directory.EnumerateFileSystemEntries(path).Any();

        if (isEmpty)
        {
            await DeleteDirectory(path);
            Logger.Information($"Removed empty directory: {path}");
        }
    }
    public async Task TryDeleteMatchingFiles(string directoryPath, string[] searchPatterns)
    {
        if (!Directory.Exists(directoryPath))
            return;

        var filesToDelete = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var pattern in searchPatterns)
        {
            foreach (var file in Directory.GetFiles(directoryPath, pattern, SearchOption.TopDirectoryOnly))
            {
                filesToDelete.Add(file);
            }
        }

        foreach (var file in filesToDelete)
        {
            await TryDeleteFile(file);
        }
    }

    public void FileCopy(string sourceFileName, string destFileName, bool overwrite)
    {
        File.Copy(sourceFileName, destFileName, overwrite);
    }

    public void DirectoryDelete(string path, bool recursive)
    {
        if (!Directory.Exists(path)) return;
        Directory.Delete(path, recursive);
    }

    public string[] DirectoryGetFiles(string path, string searchPattern, SearchOption searchOption)
    {
        return Directory.GetFiles(path, searchPattern, searchOption);
    }

    public string[] DirectoryGetDirectories(string path, string searchPattern, SearchOption searchOption)
    {
        DirectoryInfo dir = new(path);
        var directories = dir.GetDirectories(searchPattern, searchOption);
        return directories.Select((dir) => dir.FullName).ToArray();
    }

    public async Task<string> FileReadAllTextAsync(string path)
    {
        return await File.ReadAllTextAsync(path);
    }

    public async Task FileWriteAllTextAsync(string path, string contents)
    {
        await File.WriteAllTextAsync(path, contents);
    }

    public Task EnsureDirectoryExistsAsync(string path)
    {
        DirectoryService.EnsureDirectoryExists(path);
        return Task.CompletedTask;
    }
}
