namespace DefaultNamespace;

public class AppImage(IBuildLogger Logger, ICommandRunner CommandRunner, FS FileSystem, BuildConfig config)
{
    public async Task ProcessAppImage()
    {
        Logger.Information($"For creating AppImages, this script expects https://github.com/probonopd/go-appimage/tree/master/src/mkappimage in $PATH and named mkappimage without .AppImage extension");
        var TargetInstallAssembly = config.TargetInstallAssembly;
        if (!config.ShouldSkip("tarball"))
        {
            config.SkippedStepsRaw = config.SkippedStepsRaw.Append("archive").ToArray();
            config.SetSkippedSteps(config.SkippedStepsRaw);
            config.Tarballdir = config.Appdir;
            config.DestDir = config.Appdir;
            config.Prefix = "usr";
            config.BinDir += Path.DirectorySeparatorChar;
            config.LibDir = Path.Join(config.DestDir, config.Prefix, "lib");
            var tarballCreator = new Tarball(Logger, CommandRunner, FileSystem, config);
            await tarballCreator.ProcessTarball();
        }
        var files = Directory.EnumerateFiles(config.Metainfodir);

        foreach (var file in files)
        {
            var fileName = Path.GetFileName(file);

            if (fileName.Contains("metainfo"))
            {
                var newFileName = fileName.Replace("metainfo", "appdata");
                var directory = Path.GetDirectoryName(file)!;
                var newPath = Path.Combine(directory, newFileName);

                File.Move(file, newPath, true);
            }
        }

        // Only the biggest PNG — the most pixel-dense, the thiccest of them all —
        // shall be offered to the AppDir as a symbol of our respect and devotion.
        // 🏆✨ Let the icon hunger games begin.
        var pngFiles = Directory.EnumerateFiles(config.Icondir, "*.png", SearchOption.AllDirectories);

        string? largestPng = null;
        long maxSize = -1;

        foreach (var file in pngFiles)
        {
            var fileInfo = new FileInfo(file);
            if (fileInfo.Length > maxSize)
            {
                largestPng = file;
                maxSize = fileInfo.Length;
            }
        }

        if (largestPng != null)
        {
            var destPath = Path.Combine(config.Appdir, Path.GetFileName(largestPng));
            File.Copy(largestPng, destPath, overwrite: true);
        }
        var sourceFiles = Directory.EnumerateFiles(config.Applicationsdir, "*", SearchOption.TopDirectoryOnly);

        foreach (var file in sourceFiles)
        {
            var fileName = Path.GetFileName(file);
            var destPath = Path.Combine(config.Appdir, fileName);

            File.Copy(file, destPath, overwrite: true);
        }
        // We inscribe the sacred launch scroll into AppRun — a bash incantation of great power.
        // This script is the entrypoint, the oracle, the keeper of $APPDIR and guardian of $LD_LIBRARY_PATH.
        // It does what all wise elders do: prints debug info, chases symbolic links, and launches binaries like it's 1999.
        // And yes, it even negotiates with zenity when things go sideways.
        // May the chmod +x bless it, and may your AppImage rise without segfault.
        File.Move(Path.Join(config.BinDir, TargetInstallAssembly ?? "snapx-ui"), Path.Join(config.Appdir, "AppRun"), true);
        // await CommandRunner.RunAsync("chmod", $"+x {Path.Join(config.Appdir, "AppRun")}");
        var arch = await CommandRunner.CaptureAsync("arch", "");
        await CommandRunner.RunAsync(
            "env",
            $"VERSION={config.SnapXVersion} APPIMAGELAUNCHER_DISABLE=1 mkappimage --comp zstd --ll -u \"gh-releases-zsync|SnapXL|SnapX|latest|SnapX-*{arch}.AppImage.zsync\" {config.Appdir}"
        );

    }

}
