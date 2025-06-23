
namespace DefaultNamespace;
public class DirectoryService
{
    private static IBuildLogger Logger = null!;
    private static ICommandRunner CommandRunner = null!;

    public DirectoryService(IBuildLogger logger, ICommandRunner commandRunner)
    {
        Logger = logger;
        CommandRunner = commandRunner;
    }
    public static void TryDeleteJsonFilesInDirectory(string directoryPath)
    {
        if (Directory.Exists(directoryPath))
        {
            foreach (var file in Directory.GetFiles(directoryPath, "*.json", SearchOption.TopDirectoryOnly))
            {
                TryDeleteFile(file);
            }
        }
    }
    public static void TryDeleteFile(string path)
    {
        if (!File.Exists(path)) return;
        CommandRunner.RunInstallCommand($"-f {path}", "rm");
        Logger.Information($"Removed file: {path}");
    }

    public static void TryDeleteEmptyDir(string path)
    {
        if (!Directory.Exists(path) || Directory.EnumerateFileSystemEntries(path).Any()) return;
        CommandRunner.RunInstallCommand(path, "rmdir");
        Logger.Information($"Removed empty directory: {path}");
    }
    public static void TryDeleteMatchingFiles(string directoryPath, string[] searchPatterns)
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
            TryDeleteFile(file);
        }
    }
    public static void EnsureDirectoryExists(string directory)
    {
        if (Directory.Exists(directory)) return;
        try
        {
            // Attempt to create directory directly first.
            // This might require elevation if it's a protected path.
            Directory.CreateDirectory(directory);
            Logger.Information($"Successfully created directory: {directory}");
        }
        catch (UnauthorizedAccessException)
        {
            Logger.Warning($"Failed to create directory '{directory}' due to permissions. Attempting with install command.");
            // Fallback to RunInstallCommand which might use sudo
            // Note: 'mkdir -p' is idempotent and creates parent directories.
            CommandRunner.RunInstallCommand($"-p \"{directory}\"", "mkdir").GetAwaiter().GetResult();
        }
        catch (Exception ex)
        {
            Logger.Error($"Failed to create directory '{directory}': {ex.Message}");
            throw; // Rethrow if it's not an auth issue handled by RunInstallCommand
        }
    }
    public static string FindRoot()
    {
        var dir = new DirectoryInfo(Directory.GetCurrentDirectory());

        while (dir != null)
        {
            var buildScript = Path.Combine(dir.FullName, "build.sh"); // Assuming build.sh marks the root
            if (File.Exists(buildScript))
                return dir.FullName;

            dir = dir.Parent;
        }

        throw new InvalidOperationException("Could not locate root directory (no build.sh found in any parent directory).");
    }

    public static string ToUnixPath(string path)
    {
        if (!OperatingSystem.IsWindows()) return path;
        // Heuristic to check if the path is ALREADY in a Unix-like format:
        // - Starts with a forward slash (/)
        // - Does NOT contain any backslashes (\)
        // - Does NOT contain a Windows drive letter (e.g., C:)
        // This is a heuristic and might not cover all edge cases, but covers common ones.
        if (path.StartsWith("/") && !path.Contains('\\') && !Path.IsPathRooted(path.AsSpan(1)))
        {
            // It looks like a Unix-style path already (e.g., "/c/Users/...", "/usr/bin/").
            // Just ensure it's properly quoted for the shell.
            return $"\"{path.Replace("\"", "\\\"")}\"";
        }
        // 1. Convert backslashes to forward slashes
        var unixStylePath = path.Replace('\\', '/');

        // 2. Handle the drive letter (e.g., C:/ becomes /c/)
        // This assumes standard Windows drive letters like C:, D:, etc.
        if (unixStylePath is [_, ':', ..])
        {
            // Convert 'C:/' to '/c/'
            // The drive letter is converted to lowercase as is common in Unix-like environments.
            unixStylePath = "/" + unixStylePath[0].ToString().ToLowerInvariant() + unixStylePath.Substring(2);
        }

        // 3. Quote the path for shell script usage and escape any internal double quotes.
        // This is crucial for paths containing spaces or other special shell characters.
        // While file paths rarely contain double quotes, this ensures robustness.
        return $"{unixStylePath.Replace("\"", "\\\"")}";
    }
}
