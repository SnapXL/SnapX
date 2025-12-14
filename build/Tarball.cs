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

            if (!OperatingSystem.IsWindows())
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
                installConfig.Applicationsdir = junkDir;
                installConfig.Icondir = junkDir;
                installConfig.Metainfodir = junkDir;
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
                    )}" --inhibit-cache "$@"
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

                if (config.knownAssemblyNames.Contains(fileName))
                {
                    if (
                        !string.Equals(
                            fileName,
                            TargetInstallAssembly,
                            StringComparison.OrdinalIgnoreCase
                        )
                    )
                    {
                        Logger.Debug($"Removing unwanted assembly '{fileName}'");
                        await FileSystem.TryDeleteFile(libFile);
                        await FileSystem.TryDeleteFile(Path.Join(config.BinDir, fileName));
                    }
                }
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
            await FileSystem.TryDeleteFile("/tmp/processed_deps.lockfile");
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

            await FileSystem.TryDeleteFile("/tmp/processed_deps.lockfile");
            var snapxfiles = Directory.EnumerateFiles(
                config.LibDir,
                "*snapx*",
                SearchOption.AllDirectories
            );
            var interpreterFile = Directory
                .EnumerateFiles(config.LibDir, "ld-*.so.*", SearchOption.TopDirectoryOnly)
                .Select(Path.GetFileName)
                .FirstOrDefault(f => f.StartsWith("ld"));

            string interpreterArgs = string.Empty;

            if (!config.ShouldSkip("set_interpreter") && !string.IsNullOrWhiteSpace(interpreterFile))
            {
                var relativeLibPath = Path.GetRelativePath(config.Tarballdir, config.LibDir);
                interpreterArgs = $"--set-interpreter {Path.Combine(relativeLibPath, interpreterFile)} ";
            }

            foreach (var file in snapxfiles)
            {
                try
                {
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
            File.WriteAllText(
                Path.Combine(config.LibDir, "README"),
                """
                The ELF binaries in this directory expect to be run from the root of the tarball. Additionally, they are not intended to be used outside of the tarball. They are patched for this tarball and may not work correctly if moved.


                You can undo the patching with 'patchelf --remove-rpath --set-interpreter $(readelf -l "$(command -v uname)" | awk '/Requesting program interpreter/ {print $NF}' | tr -d '[]') <file>'.


                """
            );
        }

        if (!config.ShouldSkip("archive"))
        {
            var version = config.SnapXVersion;
            var arch = RuntimeInformation.ProcessArchitecture.ToString();
            var uname = config.Uname;
            var suffix = !string.IsNullOrEmpty(edition) ? $"-{edition}" : "";

            var tarballName = Path.Combine(
                config.RootDirectory,
                $"SnapX{suffix}-{config.Configuration}-{uname}-{version}-{arch}.tar.zst"
            );
            await CommandRunner.RunInstallCommand(
                $"--zstd -cf {tarballName} -C {config.Tarballdir} .",
                "tar"
            );
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
