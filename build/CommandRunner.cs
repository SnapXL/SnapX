using System.Diagnostics;
using System.Security.Principal;

namespace DefaultNamespace;

public class CommandRunner(IBuildLogger Logger) : ICommandRunner
{
    public async Task RunAsync(string command, string args)
    {
        await SimpleExec.Command.RunAsync(command, args);
    }
    public async Task InstallFile(string source, string destination, string permissions)
    {
        if (File.Exists(source))
        {
            var directoryPath = Path.GetDirectoryName(destination);
            if (!string.IsNullOrEmpty(directoryPath) && !Directory.Exists(directoryPath) && !File.Exists(directoryPath))
            {
                DirectoryService.EnsureDirectoryExists(directoryPath);
            }
            if (source.Contains("dSYM", StringComparison.InvariantCultureIgnoreCase))
            {
                Logger.Information($"Source file {source} detected as macOS debug file. Ignoring.");
                return;
            }
            var installArgs = $"-m {permissions} {source} {destination}";
            await RunInstallCommand(installArgs);

            if (Environment.GetEnvironmentVariable("SKIP_TOUCH") is not null) return;
            var touchArgs = $"-r {source} {destination}";
            await RunInstallCommand(touchArgs, "touch");
        }
        else
        {
            Logger.Information($"Source file not found: {source}");
        }
    }
    PersistentGitBash? persistentBash;
    public async Task RunInstallCommand(string installArguments, string executionCommand = "install")
    {
        var requiresElevationLikely = !IsAdmin() && RequiresElevationLikely(installArguments);
        var executionArguments = installArguments;
        var execCommand = executionCommand;

        if (OperatingSystem.IsWindows())
        {
            var shellEnv = Environment.GetEnvironmentVariable("SHELL");

            var isBashShell = shellEnv != null &&
                              !shellEnv.Contains("powershell", StringComparison.OrdinalIgnoreCase) &&
                              !shellEnv.Contains("cmd", StringComparison.OrdinalIgnoreCase);

            if (!isBashShell)
            {
                installArguments = string.Join(" ", installArguments
                    .Split(' ')
                    .Select(DirectoryService.ToUnixPath));

                var rawArgs = requiresElevationLikely ? $"sudo {executionCommand} {installArguments}" : $"{executionCommand} {installArguments}";

                if (persistentBash == null)
                {
                    persistentBash = new PersistentGitBash("bash.exe");
                    Logger.Information("Initialized persistent git bash session for commands.");
                }
                Logger.Debug($"bash.exe -c \"{rawArgs}\"");

                var task = await persistentBash.RunCommandAsync(rawArgs).ConfigureAwait(false);
                var (success, output, error) = task;

                if (!success)
                {
                    Logger.Error($"Install command failed: {error}");
                    // if (requiresElevationLikely || !error.Contains("Permission denied", StringComparison.OrdinalIgnoreCase))
                    //     return;
                    // Error("Retrying with elevated privileges (sudo)...");
                    // RunInstallCommand(installArguments);
                }
                else
                {
                    if (!output.Contains("__CMD_DONE__0")) Logger.Warning($"Install command succedded: {output}");
                }
                return;
            }
        }

        if (requiresElevationLikely)
        {
            executionArguments = $"{execCommand} {executionArguments}";
            execCommand = "sudo";
        }
        // Logger.Debug($"{execCommand} {executionArguments}");

        await RunAsync(execCommand, executionArguments);
    }

    private bool RequiresElevationLikely(string installArguments)
    {
        if (Environment.GetEnvironmentVariable("ELEVATION_NOT_NEEDED") == "1")
            return false;

        if (OperatingSystem.IsWindows())
            // On Windows, elevation is handled differently (e.g., run as admin).
            // This simple check is for Unix-like systems.
            return false;

        // Common protected base paths (must be rooted)
        string[] protectedPaths = ["/usr", "/opt", "/etc", "/var", "/bin", "/sbin"];

        var arguments = installArguments
            .Split([' ', '\t'], StringSplitOptions.RemoveEmptyEntries)
            .Select(arg => arg.Trim().Trim('"', '\''));

        return (from arg in arguments where Path.IsPathRooted(arg) from protectedPath in protectedPaths where arg == protectedPath || arg.StartsWith(protectedPath + '/') select arg).Any();
    }

    private bool IsAdmin()
    {
        if (OperatingSystem.IsWindows())
        {
            try
            {
                using var identity = WindowsIdentity.GetCurrent();
                var principal = new WindowsPrincipal(identity);
                return principal.IsInRole(WindowsBuiltInRole.Administrator);
            }
            catch
            {
                return false; // Error occurred, assume not admin
            }
        }
        else // Unix-like systems (Linux, macOS)
        {
            return GetCurrentUid() == 0;
        }
    }

    private int GetCurrentUid()
    {
        if (OperatingSystem.IsWindows())
        {
            return -1; // UID concept is not directly applicable in the same way.
        }

        var uidStr = Environment.GetEnvironmentVariable("UID");
        if (!string.IsNullOrEmpty(uidStr) && int.TryParse(uidStr, out var uidEnv))
        {
            return uidEnv;
        }

        try
        {
            var processStartInfo = new ProcessStartInfo
            {
                FileName = "id",
                Arguments = "-u",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(processStartInfo);
            if (process == null) return 1000; // Default to non-root if process fails to start

            process.WaitForExit(5000); // Wait for 5 seconds
            if (!process.HasExited)
            {
                process.Kill(); // Kill if it times out
                Console.Error.WriteLine("Warning: 'id -u' command timed out.");
                return 1000;
            }

            if (process.ExitCode != 0) return 1000; // Default to non-root if command fails

            using var reader = process.StandardOutput;
            var output = reader.ReadToEnd();
            return int.TryParse(output.Trim(), out var uidCmd) ? uidCmd : 1000; // Default if parsing fails
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Warning: Could not get UID using 'id -u': {ex.Message}");
            return 1000; // Default in case of any exception
        }
    }
}
