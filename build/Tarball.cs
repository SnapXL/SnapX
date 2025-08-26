using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using Bullseye;

namespace DefaultNamespace;

public class Tarball(IBuildLogger Logger, ICommandRunner CommandRunner, FS FileSystem, BuildConfig config)
{
    public async Task ProcessTarball()
    {
        Logger.Information("Tarball started");
        var TargetInstallAssembly = config.TargetInstallAssembly;
        if (!config.ShouldSkip("install"))
        {
            var destDir = config.Tarballdir + Path.DirectorySeparatorChar;
            var installConfig = config;
            var defaultConfig = new BuildConfig
            {
                BullseyeOptions =  new Options()
            };
            if (installConfig.DestDir == defaultConfig.DestDir)
            {
                installConfig.DestDir = string.Empty;
            }
            if (installConfig.Prefix == defaultConfig.Prefix)
            {
                installConfig.Prefix = string.Empty;
            }

            if (installConfig.BinDir == defaultConfig.BinDir)
            {
                installConfig.BinDir = destDir;
            }
            if (installConfig.LibDir == defaultConfig.LibDir)
            {
                installConfig.LibDir = Path.Join(destDir, installConfig.Prefix, "lib");
            }

            installConfig.Docdir = destDir;
            installConfig.Applicationsdir = destDir;
            installConfig.Licensedir = destDir;
            // This is only for regular packaging. In tarball land, it holds different meaning.
            installConfig.TargetInstallAssembly = null;

            var installProcessor = new Install(Logger, CommandRunner, FileSystem, config);
            await installProcessor.ProcessInstall();
        }
        // This will be implemented tomorrow.
        // await CommandRunner.RunAsync(Path.Join(config.PackagingDirectory, "copy_deps.sh"), "<binary> <dest>");
        if (!config.ShouldSkip("archive"))
        {
            var version = config.SnapXVersion;
            var arch = RuntimeInformation.ProcessArchitecture.ToString();
            var uname = string.Empty;
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) uname = "Windows";
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux)) uname = "Linux";
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX)) uname = "macOS";
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.FreeBSD)) uname = "FreeBSD";
            if (uname == string.Empty) uname = Environment.OSVersion.Platform.ToString();
            var edition = TargetInstallAssembly;

            if (!string.IsNullOrEmpty(edition))
            {
                var dashIndex = edition.IndexOf('-');
                if (dashIndex >= 0 && dashIndex < edition.Length - 1)
                {
                    edition = edition[(dashIndex + 1)..].ToUpperInvariant();
                }
            }
            else
            {
                edition = null;
            }

            var suffix = !string.IsNullOrEmpty(edition) ? $"-{edition}" : "";

            var tarballName = Path.Combine(
                config.RootDirectory,
                $"SnapX{suffix}-{config.Configuration}-{uname}-{version}-{arch}.tar.xz"
            );

            await CommandRunner.RunInstallCommand($"-cJf {tarballName} -C {config.Tarballdir} .", "tar");
        }

    }
}
