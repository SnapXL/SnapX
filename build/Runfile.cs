using System.Runtime.InteropServices;

namespace DefaultNamespace;

public class Runfile(IBuildLogger Logger, ICommandRunner CommandRunner, FS FileSystem, BuildConfig config)
{
    public async Task ProcessRunfile()
    {
        var TargetInstallAssembly = config.TargetInstallAssembly;
        var edition = config.Edition;
        if (!config.ShouldSkip("tarball"))
        {
            config.SkippedStepsRaw = config.SkippedStepsRaw.Append("archive").ToArray();
            config.SetSkippedSteps(config.SkippedStepsRaw);
            config.Tarballdir = config.Rundir;
            config.DestDir = config.Rundir;
            config.Prefix = string.Empty;
            // config.BinDir += Path.DirectorySeparatorChar;
            // config.LibDir = config.BinDir;
            // config.DisableWrapperScript = true;
            var tarballCreator = new Tarball(Logger, CommandRunner, FileSystem, config);
            await tarballCreator.ProcessTarball();
        }
        var goBin = Environment.GetEnvironmentVariable("GOBIN");
        var goPath = Environment.GetEnvironmentVariable("GOPATH");
        var home = Environment.GetEnvironmentVariable("HOME") ?? Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        string selfextractPath;

        if (!string.IsNullOrEmpty(goBin))
        {
            selfextractPath = Path.Combine(goBin, "selfextract");
        }
        else if (!string.IsNullOrEmpty(goPath))
        {
            selfextractPath = Path.Combine(goPath, "bin", "selfextract");
        }
        else
        {
            selfextractPath = Path.Combine(home, "go", "bin", "selfextract");
        }

        if (!File.Exists(selfextractPath) || !IsExecutable(selfextractPath))
        {
            Logger.Information($"selfextract not found or not executable at {selfextractPath}, installing...");
            await InstallRunfile(CommandRunner, Logger);
        }
        var fileTypeCheck = await CommandRunner.CaptureAsync("file", EscapeShellArg(selfextractPath));
        if (fileTypeCheck.Contains("dynamically linked"))
        {
            Logger.Information($"selfextract at {selfextractPath} is dynamically linked, reinstalling...");
            await InstallRunfile(CommandRunner, Logger);
        }
        var version = config.SnapXVersion;
        var arch = RuntimeInformation.ProcessArchitecture.ToString();
        var uname = OperatingSystem.IsFreeBSD() ? config.Uname : string.Empty;
        var suffix = !string.IsNullOrEmpty(edition) ? $"-{edition}" : "";
        var unameSuffix = !string.IsNullOrEmpty(uname) ? $"-{uname}" : "";
        var runFileName = $"SnapX{suffix}-{config.Configuration}{unameSuffix}-{version}-{arch}.run";
        File.Move(Path.Join(config.BinDir, TargetInstallAssembly ?? "snapx-ui"), Path.Join(config.BinDir, "selfextract_startup"), true);
        await CommandRunner.RunAsync(selfextractPath, $"-v -f {runFileName} -C {config.Rundir} .");
    }

    async Task InstallRunfile(ICommandRunner commandRunner, IBuildLogger logger)
    {
        var installCmdArgs = new[]
        {
            "CGO_ENABLED=0",
            "go",
            "install", "-v", "github.com/synthesio/selfextract@latest"
        };

        await commandRunner.RunAsync("env", string.Join(' ', installCmdArgs));

        Logger.Information("selfextract installed/reinstalled successfully.");
    }
    static bool IsExecutable(string path)
    {
        try
        {
            return new FileInfo(path).Exists && (new FileInfo(path).Attributes & FileAttributes.Directory) == 0;
        }
        catch { return false; }
    }

    static string EscapeShellArg(string arg)
    {
        return "'" + arg.Replace("'", "'\"'\"'") + "'";
    }
}
