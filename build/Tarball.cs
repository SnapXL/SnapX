using System.Runtime.InteropServices;
using Bullseye;

namespace DefaultNamespace;

public class Tarball(IBuildLogger Logger, ICommandRunner CommandRunner, FS FileSystem, BuildConfig config)
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
            var defaultConfig = new BuildConfig
            {
                BullseyeOptions = new Options()
            };
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
            if (installConfig.LibDir == Path.Join(config.DestDir, config.Prefix, "lib", "snapx"))
            {
                installConfig.LibDir = Path.Join(destDir, installConfig.Prefix, "lib");
            }

            if (!installConfig.ShouldSkip("copy_deps"))
            {
                var libDirWithoutDestDir = !string.IsNullOrWhiteSpace(config.DestDir) &&
                                           config.LibDir.StartsWith(config.DestDir, StringComparison.Ordinal)
                    ? config.LibDir[config.DestDir.Length..].TrimStart('/')
                    : config.LibDir.TrimStart('/');
                installConfig.CustomWrapperScript = $"""
                                                     #!/usr/bin/env sh
                                                     # SnapX version: {config.SnapXVersion}

                                                     # Attempt to re-exec with bash if available
                                                     if [ -z "$BOOTSTRAPPED_WITH_BASH" ] && command -v bash >/dev/null 2>&1; then
                                                       BOOTSTRAPPED_WITH_BASH=1 exec bash "$0" "$@"
                                                     fi

                                                     dir="$(cd -P -- "$(dirname -- "$0")" && pwd -P)"
                                                     cd "$dir" || exit

                                                     EXTRA_ARGS=""
                                                     if [ -n "$BASH_VERSION" ]; then
                                                         EXTRA_ARGS="-a -c"
                                                     fi

                                                     ld_path=$(echo lib/ld*.so* | head -n 1)

                                                     if [ -e "$ld_path" ]; then
                                                         exec "$EXTRA_ARGS" "$ld_path" "{Path.Join(libDirWithoutDestDir, TargetInstallAssembly ?? "snapx-ui")}" "$@"
                                                     else
                                                         exec "{Path.Join(libDirWithoutDestDir, TargetInstallAssembly ?? "snapx-ui")}" "$@"
                                                     fi
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
            var installedLibFiles = Directory.GetFiles(config.LibDir, "*", SearchOption.AllDirectories);

            foreach (var libFile in installedLibFiles)
            {
                var fileName = Path.GetFileNameWithoutExtension(libFile);

                if (config.knownAssemblyNames.Contains(fileName))
                {
                    if (!string.Equals(fileName, TargetInstallAssembly, StringComparison.OrdinalIgnoreCase))
                    {
                        Logger.Debug($"Removing unwanted assembly '{fileName}'");
                        await FileSystem.TryDeleteFile(libFile);
                        await FileSystem.TryDeleteFile(Path.Join(config.BinDir, fileName));
                    }
                }
            }
        }
        if ((OperatingSystem.IsLinux() || OperatingSystem.IsFreeBSD()) &&
            !config.ShouldSkip("copy_deps"))
        {
            var processedFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var targetOutputDir = TargetInstallAssembly is not null
                ? Path.Join(config.OutputDir, TargetInstallAssembly)
                : Path.Join(config.OutputDir);
            var files = Directory.EnumerateFiles(targetOutputDir, "*", SearchOption.AllDirectories);
            await FileSystem.TryDeleteFile("/tmp/processed_deps.lockfile");
            foreach (var file in files)
            {
                try
                {
                    if (!IsSoFile(file) && !IsElfFile(file)) continue;
                    if (!processedFiles.Add(file)) continue;

                    var args = $"\"{file}\" \"{config.LibDir}\"";
                    await CommandRunner.RunAsync(Path.Join(config.PackagingDirectory, "copy_deps.sh"), args);
                }
                catch (Exception ex)
                {
                    Logger.Error($"Error processing {file}: {ex.Message}");
                }
            }

            await FileSystem.TryDeleteFile("/tmp/processed_deps.lockfile");
            var snapxfiles = Directory.EnumerateFiles(config.LibDir, "*snapx*", SearchOption.AllDirectories);
            foreach (var file in snapxfiles)
            {
                await CommandRunner.RunAsync("patchelf", $"--set-rpath \"$ORIGIN\" {file}");
            }
        }

        if (!config.ShouldSkip("archive"))
        {
            var version = config.SnapXVersion;
            var arch = RuntimeInformation.ProcessArchitecture.ToString();
            var uname = config.Uname;
            var suffix = !string.IsNullOrEmpty(edition) ? $"-{edition}" : "";

            var tarballName = Path.Combine(
                config.RootDirectory,
                $"SnapX{suffix}-{config.Configuration}-{uname}-{version}-{arch}.tar.xz"
            );
            await CommandRunner.RunInstallCommand($"-cJf {tarballName} -C {config.Tarballdir} .", "tar");
        }
    }

    private static bool IsSoFile(string filePath)
    {
        string fileName = Path.GetFileName(filePath);
        return fileName.Contains(".so");
    }

    private static bool IsElfFile(string filePath)
    {
        try
        {
            using var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read);
            byte[] magic = new byte[4];
            int read = fs.Read(magic, 0, 4);
            return read == 4 && magic[0] == 0x7F && magic[1] == (byte)'E' && magic[2] == (byte)'L' && magic[3] == (byte)'F';
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
