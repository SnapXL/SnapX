namespace DefaultNamespace;

public class Uninstall(IBuildLogger Logger, IFileSystem FileSystem, BuildConfig config)
{
    private static readonly string[] libraryExtensions = ["*.so", "*.dylib", "*.dll"];
    public async Task ProcessUninstall()
    {
        if (Directory.Exists(config.BinDir))
        {
            string[] searchPatterns =
            [
                    "*snapx*",
                    "libe_sqlite3.so",
                    "libHarfBuzzSharp.so",
                    "libSkiaSharp.so"
            ];


            await FileSystem.TryDeleteMatchingFiles(config.BinDir, searchPatterns);
            // Previously, SnapX pooped out *.so files in the LibDir without the snapx directory.
            await FileSystem.TryDeleteMatchingFiles(Path.Combine(config.LibDir, ".."), searchPatterns);
        }

        if (Directory.Exists(config.OutputDir))
        {
            foreach (var pattern in libraryExtensions)
            {
                foreach (var file in Directory.GetFiles(config.OutputDir, pattern, SearchOption.TopDirectoryOnly))
                {
                    var fileName = Path.GetFileName(file);
                    var installedPath = Path.Combine(config.BinDir, fileName);
                    await FileSystem.TryDeleteFile(installedPath);
                }
            }
        }

        if (Directory.Exists(config.Applicationsdir))
        {
            foreach (var file in Directory.GetFiles(config.Applicationsdir, "io.github.BrycensRanch.SnapX.desktop",
                         SearchOption.TopDirectoryOnly))
            {
                await FileSystem.TryDeleteFile(file);
            }
        }

        if (Directory.Exists(config.Metainfodir))
        {
            foreach (var file in Directory.GetFiles(config.Metainfodir, "io.github.BrycensRanch.SnapX.metainfo.xml",
                         SearchOption.TopDirectoryOnly))
            {
                await FileSystem.TryDeleteFile(file);
            }
        }
        if (Directory.Exists(config.LibDir))
        {
            var libDir = Path.GetFullPath(config.LibDir);

            var forbiddenRoots = new[]
            {
                    "/", "/usr", "/lib", "/lib64", "/usr/lib", "/usr/lib64", "/usr/local/lib",
                    "/usr/lib/x86_64-linux-gnu", "/lib/x86_64-linux-gnu",
                    "/usr/lib/i386-linux-gnu", "/lib/i386-linux-gnu",
                    "/usr/lib/arm-linux-gnueabihf", "/lib/arm-linux-gnueabihf"
                };

            if (forbiddenRoots.Any(root => string.Equals(libDir, root, StringComparison.Ordinal)))
            {
                Logger.Warning($"Refusing to clean protected system library directory: {libDir}");
                return;
            }

            foreach (var file in Directory.GetFiles(libDir, "*", SearchOption.AllDirectories))
            {
                await FileSystem.TryDeleteFile(file);
            }
        }
        if (Directory.Exists(config.Icondir))
        {
            foreach (var file in Directory.GetFiles(config.Icondir, "*io.github.BrycensRanch.SnapX*",
                         SearchOption.AllDirectories))
            {
                await FileSystem.TryDeleteFile(file);
            }
        }
        if (Directory.Exists(config.Docdir))
        {
            foreach (var file in Directory.GetFiles(config.Docdir, "*.md", SearchOption.TopDirectoryOnly))
            {
                await FileSystem.TryDeleteFile(file);
            }
        }

        if (Directory.Exists(config.Licensedir))
        {
            foreach (var file in Directory.GetFiles(config.Licensedir, "LICENSE.md", SearchOption.TopDirectoryOnly))
            {
                await FileSystem.TryDeleteFile(file);
            }
        }

        if (Directory.Exists(Path.Join(config.Datadir, "SnapX")))
        {
            foreach (var file in Directory.GetFiles(Path.Join(config.Datadir, "SnapX"), "*.json",
                         SearchOption.AllDirectories))
            {
                await FileSystem.TryDeleteFile(file);
            }
        }

        // Remove NMH binary
        await FileSystem.TryDeleteFile(config.NMHostPath);

        // Clean up empty directories (optional, and cautious)
        await FileSystem.TryDeleteEmptyDir(config.BinDir);
        await FileSystem.TryDeleteEmptyDir(config.Applicationsdir);
        await FileSystem.TryDeleteEmptyDir(config.Metainfodir);
        await FileSystem.TryDeleteEmptyDir(config.Docdir);
        await FileSystem.TryDeleteEmptyDir(config.Licensedir);
        await FileSystem.TryDeleteEmptyDir(Path.Join(config.Datadir, "SnapX"));
        await FileSystem.TryDeleteEmptyDir(config.LibDir);

        if (!string.IsNullOrWhiteSpace(config.DestDir))
        {
            await FileSystem.TryDeleteEmptyDir(config.DestDir);
        }
    }
    private void LogInstallationPaths()
    {
        Logger.Information($"--- Installation Paths ---");
        Logger.Information($"Destination Directory (DESTDIR): {config.DestDir}");
        Logger.Information($"Prefix: {config.Prefix}");
        Logger.Information($"Install Root: {Path.Join(config.DestDir, config.Prefix)}");
        Logger.Information($"Data directory: {config.Datadir}");
        Logger.Information($"Bin directory: {config.BinDir}");
        Logger.Information($"Documentation directory: {config.Docdir}");
        Logger.Information($"License directory: {config.Licensedir}");
        Logger.Information($"Metainfo directory: {config.Metainfodir}");
        Logger.Information($"Tarball directory: {config.Tarballdir}");
        Logger.Information($"Application directory: {config.Applicationsdir}");
        Logger.Information($"Icon directory: {config.Icondir}");
        Logger.Information($"Library directory: {config.LibDir}");
        Logger.Information($"Packaging User Directory: {config.PackagingUsrDir}");
    }
}
