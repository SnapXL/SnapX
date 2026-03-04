using System.Runtime.InteropServices;
using Bullseye;

namespace DefaultNamespace;

public class Tarball(
    IBuildLogger Logger,
    ICommandRunner CommandRunner,
    FS FileSystem,
    BuildConfig config
)
{
    public async Task ProcessTarball()
    {
        Logger.Information("Tarball started");
        var TargetInstallAssembly = config.TargetInstallAssembly;
        var edition = config.Edition;
        if (!config.ShouldSkip("install"))
        {
            var destDir = config.Tarballdir + Path.DirectorySeparatorChar;
            var installConfig = config;
            var defaultConfig = new BuildConfig { BullseyeOptions = new Options() };
            if (string.IsNullOrWhiteSpace(installConfig.DestDir))
            {
                installConfig.DestDir = destDir;
            }
            if (installConfig.Prefix == defaultConfig.Prefix)
            {
                installConfig.Prefix = string.Empty;
            }

            if (installConfig.BinDir == Path.Join(config.DestDir, config.Prefix, "bin"))
            {
                installConfig.BinDir = destDir;
            }

            if (OperatingSystem.IsLinux() || OperatingSystem.IsFreeBSD())
            {
                if (
                    installConfig.LibDir == Path.Join(config.DestDir, config.Prefix, "lib", "snapx")
                )
                {
                    installConfig.LibDir = Path.Join(destDir, installConfig.Prefix, "lib");
                }
            }
            else
            {
                if (
                    installConfig.LibDir == Path.Join(config.DestDir, config.Prefix, "lib", "snapx")
                )
                {
                    installConfig.LibDir = destDir;
                }
                var junkDirectoryInfo = Directory.CreateTempSubdirectory("BUILD_SNAPX");
                var junkDir = junkDirectoryInfo.FullName;
                installConfig.SetSkippedSteps(["sniff_deps", "copy_deps"]);
                installConfig.Applicationsdir = junkDir;
                installConfig.Icondir = junkDir;
                installConfig.Metainfodir = junkDir;
                installConfig.DisableWrapperScript = true;
            }

            if (!installConfig.ShouldSkip("copy_deps"))
            {
                var libDirWithoutDestDir =
                    !string.IsNullOrWhiteSpace(config.DestDir)
                    && config.LibDir.StartsWith(config.DestDir, StringComparison.Ordinal)
                        ? config.LibDir[config.DestDir.Length..].TrimStart('/')
                        : config.LibDir.TrimStart('/');
                var relativeLibPath = Path.GetRelativePath(config.Tarballdir, config.LibDir);
                installConfig.CustomWrapperScript = $"""
                    #!/usr/bin/env sh
                    # SnapX version: {config.SnapXVersion}

                    dir="$(cd -P -- "$(dirname -- "$0")" && pwd -P)"
                    cd "$dir" || exit

                    PATH="$dir:$dir/{relativeLibPath}:$PATH"

                    exec "{Path.Join(
                        libDirWithoutDestDir,
                        TargetInstallAssembly ?? "snapx-ui"
                    )}" "$@"
                    """;
            }
            installConfig.Docdir = destDir;
            // installConfig.Applicationsdir = destDir;
            installConfig.Licensedir = destDir;
            // This is only for regular packaging. In tarball land, it holds different meaning.
            installConfig.TargetInstallAssembly = null;

            var installProcessor = new Install(Logger, CommandRunner, FileSystem, config);
            await installProcessor.ProcessInstall();
        }
        if (TargetInstallAssembly is not null)
        {
            var installedLibFiles = Directory.GetFiles(
                config.LibDir,
                "*",
                SearchOption.AllDirectories
            );

            foreach (var libFile in installedLibFiles)
            {
                var fileName = Path.GetFileNameWithoutExtension(libFile);

                if (!config.knownAssemblyNames.Contains(fileName) &&
                    (!fileName.EndsWith(".exe", StringComparison.OrdinalIgnoreCase) ||
                     !config.knownAssemblyNames.Contains(fileName.Substring(0, fileName.Length - 4)))) continue;
                if (string.Equals(
                        fileName,
                        TargetInstallAssembly,
                        StringComparison.OrdinalIgnoreCase
                    )) continue;
                Logger.Debug($"Removing unwanted assembly '{fileName}'");
                await FileSystem.TryDeleteFile(libFile);
                await FileSystem.TryDeleteFile(Path.Join(config.BinDir, fileName));
            }
        }
        if (
            (OperatingSystem.IsLinux() || OperatingSystem.IsFreeBSD())
            && !config.ShouldSkip("copy_deps")
        )
        {
            var processedFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var targetOutputDir = TargetInstallAssembly is not null
                ? Path.Join(config.OutputDir, TargetInstallAssembly)
                : Path.Join(config.OutputDir);
            var files = Directory.EnumerateFiles(targetOutputDir, "*", SearchOption.AllDirectories);
            foreach (var file in files)
            {
                try
                {
                    if (!IsSoFile(file) && !IsElfFile(file))
                        continue;
                    if (!processedFiles.Add(file))
                        continue;

                    var args = $"\"{file}\" \"{config.LibDir}\"";
                    await CommandRunner.RunAsync(
                        Path.Join(config.PackagingDirectory, "copy_deps.sh"),
                        args
                    );
                }
                catch (Exception ex)
                {
                    Logger.Error($"Error processing {file}: {ex.Message}");
                }
            }
            if (!config.ShouldSkip("sniff_deps"))
            {
                var usesWrapperScript = config.DisableWrapperScript is false;
                var target = TargetInstallAssembly ?? "snapx-ui";
                var args =
                    $"\"{(usesWrapperScript ? Path.Join(config.Tarballdir, target) : Path.Join(config.LibDir, target))}\" \"{config.LibDir}\"";
                await CommandRunner.RunAsync(
                    Path.Join(config.PackagingDirectory, "sniff_deps.sh"),
                    args
                );
            }
            await FileSystem.TryDeleteFile("/tmp/processed_deps.lockfile");
            var allLibFiles = Directory.EnumerateFiles(
                config.LibDir,
                "*",
                SearchOption.AllDirectories
            );
            var interpreterFile = Directory
                .EnumerateFiles(config.LibDir, "ld*.so*", SearchOption.TopDirectoryOnly)
                .Select(Path.GetFileName)
                .FirstOrDefault(f => f.StartsWith("ld"));

            var interpreterArgs = string.Empty;

            if (
                !config.ShouldSkip("set_interpreter") && !string.IsNullOrWhiteSpace(interpreterFile)
            )
            {
                var relativeLibPath = Path.GetRelativePath(config.Tarballdir, config.LibDir);
                interpreterArgs =
                    $"--set-interpreter {Path.Combine(relativeLibPath, interpreterFile)} ";
            }
            foreach (var file in allLibFiles)
            {
                try
                {
                    if (IsSoFile(file) || !IsElfFile(file)) continue;
                    await CommandRunner.RunAsync(
                        "patchelf",
                        $"--set-rpath \"$ORIGIN\" --force-rpath {interpreterArgs} {file}"
                    );
                }
                catch (Exception ex)
                {
                    Logger.Error($"Error patching {file}: {ex.Message}");
                }
            }
        }

        if (!config.ShouldSkip("archive"))
        {
            var version = config.SnapXVersion;
            var arch = RuntimeInformation.ProcessArchitecture.ToString();
            var uname = config.Uname;
            var suffix = !string.IsNullOrEmpty(edition) ? $"-{edition}" : "";

            var (ext, flags) = OperatingSystem.IsLinux() || OperatingSystem.IsFreeBSD()
                ? ("tar.zst", "--zstd -cf")
                : OperatingSystem.IsMacOS()
                    ? ("zip", "-a -cf")
                    : ("tar.xz", "-cJf");

            var tarballName = Path.Combine(
                config.RootDirectory,
                $"SnapX{suffix}-{config.Configuration}-{uname}-{version}-{arch}.{ext}"
            );

            await CommandRunner.RunInstallCommand(
                $"{flags} {tarballName} -C {config.Tarballdir} .",
                "tar"
            );
        }
    }

    private static bool IsSoFile(string filePath)
    {
        string fileName = Path.GetFileName(filePath);
        return fileName.Contains(".so");
    }

    public static bool IsElfFile(string filePath)
    {
        try
        {
            using var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read);
            byte[] magic = new byte[4];
            int read = fs.Read(magic, 0, 4);
            return read == 4
                && magic[0] == 0x7F
                && magic[1] == (byte)'E'
                && magic[2] == (byte)'L'
                && magic[3] == (byte)'F';
        }
        catch
        {
            return false;
        }
    }

    string NormalizePath(string path)
    {
        return Path.GetFullPath(path);
    }

    string EscapeShellArg(string arg)
    {
        // Safely escape a shell argument (POSIX-style)
        return "'" + arg.Replace("'", "'\"'\"'") + "'";
    }
}
