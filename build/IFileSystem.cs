namespace DefaultNamespace;

public interface IFileSystem
{
    void FileCopy(string sourceFileName, string destFileName, bool overwrite);
    void DirectoryDelete(string path, bool recursive);
    string[] DirectoryGetFiles(string path, string searchPattern, SearchOption searchOption);
    string[] DirectoryGetDirectories(string path, string searchPattern, SearchOption searchOption);
    Task<string> FileReadAllTextAsync(string path);
    Task FileWriteAllTextAsync(string path, string contents);
    Task EnsureDirectoryExistsAsync(string path);
}
