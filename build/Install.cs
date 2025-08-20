namespace DefaultNamespace;

public class Install(IBuildLogger Logger, ICommandRunner CommandRunner, FS FileSystem, BuildConfig config)
{
    public async Task ProcessInstall()
    {
        LogInstallationPaths();
        await InstallPackagingFiles();
        await InstallDocumentationFiles();
        await InstallBuildOutputFiles();
    }

    private void LogInstallationPaths()
    {
        Logger.Information($"--- Installation Paths ---");
        Logger.Information($"Root directory: {config.RootDirectory}");
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
    private (string destination, string permissions) GetPackagingFileDestination(string sourceFile, string relativePath)
    {
        var destinationFile = Path.Join(config.DestDir, config.Prefix, relativePath);
        var permissions = "0644";

        switch (sourceFile)
        {
            case var file when file.EndsWith(".desktop"):
                permissions = "0755";
                destinationFile = Path.Combine(config.Applicationsdir, Path.GetFileName(file));
                break;
            case var file when file.EndsWith(".metainfo.xml"):
                destinationFile = Path.Combine(config.Metainfodir, Path.GetFileName(file));
                break;
            case var file when file.EndsWith(".md", StringComparison.OrdinalIgnoreCase):
                destinationFile = Path.Combine(config.Docdir, Path.GetFileName(file));
                break;
        }

        return (destinationFile, permissions);
    }
    private async Task InstallPackagingFiles()
    {
        if (!Directory.Exists(config.PackagingUsrDir)) return;

        // snapx does not own these files.
        // Only UI frontends (Avalonia, GTK) should.
        if (config.TargetInstallAssembly is not null &&
            string.Equals(config.TargetInstallAssembly, "snapx", StringComparison.OrdinalIgnoreCase)) return;
        var files = Directory.GetFiles(config.PackagingUsrDir, "*", SearchOption.AllDirectories);
        foreach (var sourceFile in files)
        {
            var relativePath = Path.GetRelativePath(config.PackagingUsrDir, sourceFile);
            var (destinationFile, permissions) = GetPackagingFileDestination(sourceFile, relativePath);

            await CommandRunner.InstallFile(sourceFile, destinationFile, permissions);
        }
    }
    private async Task InstallDocumentationFiles()
    {
        if (config.TargetInstallAssembly is not null &&
            !string.Equals(config.TargetInstallAssembly, "snapx", StringComparison.OrdinalIgnoreCase)) return;
        await CommandRunner.InstallFile(Path.Combine(config.RootDirectory, "LICENSE.md"), Path.Combine(config.Licensedir, "LICENSE.md"), "0644");
        var documentation = Directory.GetFiles(config.RootDirectory, "*.md", SearchOption.TopDirectoryOnly);
        var packagingDoc = Directory.GetFiles(config.PackagingDirectory, "*.md", SearchOption.TopDirectoryOnly);

        var allDocs = documentation.Concat(packagingDoc).ToArray();

        foreach (var docFile in allDocs)
        {
            if (Path.GetFileName(docFile).Equals("LICENSE.md", StringComparison.OrdinalIgnoreCase)) continue;
            await CommandRunner.InstallFile(docFile, Path.Combine(config.Docdir, Path.GetFileName(docFile)), "0644");
        }
    }
    private async Task InstallBuildOutputFiles()
    {
        if (!Directory.Exists(config.OutputDir)) return;

        var seenFileOwners = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        var outputFiles = Directory.GetFiles(config.OutputDir, "*", SearchOption.AllDirectories)
            .OrderBy(file =>
            {
                var relativePath = Path.GetRelativePath(config.OutputDir, file);
                var topLevelDir = relativePath.Split(Path.DirectorySeparatorChar).FirstOrDefault() ?? "";
                return (topLevelDir, Path.GetFileName(file));
            });
        foreach (var outputFile in outputFiles)
        {
            var fileName = Path.GetFileName(outputFile);
            if (fileName.EndsWith(".dbg") || fileName.EndsWith(".pdb")) continue;

            // Prevent package pollution
            var assembly = GetOwningAssembly(outputFile);

            if (config.TargetInstallAssembly is not null)
            {
                if (!seenFileOwners.TryAdd(fileName, assembly))
                {
                    // Someone else already owns this file
                    if (!string.Equals(seenFileOwners[fileName], config.TargetInstallAssembly,
                            StringComparison.OrdinalIgnoreCase))
                    {
                        Logger.Information($"{fileName} is owned by another assembly!");
                        continue;
                    }
                }

                if (!string.Equals(assembly, config.TargetInstallAssembly, StringComparison.OrdinalIgnoreCase))
                {
                    Logger.Information($"{outputFile} is not apart of the requested assembly!");
                    continue;
                }
            }
            var (destinationFile, permissions) = GetBuildOutputDestination(outputFile);

            if (destinationFile == null) continue;

            await CommandRunner.InstallFile(outputFile, destinationFile, permissions);

            await CreateWrapperScriptIfApplicable(outputFile);
        }
    }
    private string GetOwningAssembly(string filePath)
    {
        var relativePath = Path.GetRelativePath(config.OutputDir, filePath);
        var firstComponent = relativePath.Split(Path.DirectorySeparatorChar).FirstOrDefault() ?? "";
        return firstComponent;
    }
    private (string? destination, string permissions) GetBuildOutputDestination(string outputFile)
    {
        string permissions = "0644";
        string destinationFile;
        var fileName = Path.GetFileName(outputFile);
        var assemblyName = Path.GetFileNameWithoutExtension(outputFile);

        var rawRelativePath = Path.GetRelativePath(config.OutputDir, outputFile);
        var relativePath = Path.Combine(rawRelativePath.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar).Skip(1).ToArray());

        if (assemblyName.Equals(config.NMHassemblyName, StringComparison.OrdinalIgnoreCase) && config.NMHostPath is not null)
        {
            destinationFile = config.NMHostPath;
            permissions = "0755";
        }
        else if (fileName.EndsWith(".dll") || fileName.EndsWith(".so") || fileName.EndsWith(".dylib"))
        {
            destinationFile = Path.Combine(config.LibDir, relativePath);
        }
        else if (fileName.EndsWith(".json", StringComparison.OrdinalIgnoreCase) && fileName.Contains("host", StringComparison.OrdinalIgnoreCase))
        {
            destinationFile = Path.Join(config.Datadir, "SnapX", relativePath);
        }
        else
        {
            destinationFile = Path.Combine(config.LibDir, relativePath);
            permissions = "0755";
        }

        return (destinationFile, permissions);
    }

    private async Task CreateWrapperScriptIfApplicable(string sourceFile)
    {
        if (config.DisableWrapperScript) return;
        var fileName = Path.GetFileNameWithoutExtension(sourceFile);
        if (!config.knownAssemblyNames.Contains(fileName) || fileName == config.knownAssemblyNames[^1]) return;
        var scriptPath = Path.GetFullPath(Path.Combine(config.BinDir, fileName));
        var outputDir = Path.GetDirectoryName(sourceFile)!;
        await FileSystem.EnsureDirectoryExistsAsync(Path.GetDirectoryName(scriptPath)!);
        var rawRelativePath = Path.GetRelativePath(outputDir, sourceFile);
        var relativePath = Path.Combine(
            rawRelativePath
                .Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                .Where(part => !part.Contains("snapx", StringComparison.OrdinalIgnoreCase))
                .ToArray()
        );
        var libDirWithoutDestDir = !string.IsNullOrWhiteSpace(config.DestDir) &&
                                   config.LibDir.StartsWith(config.DestDir, StringComparison.Ordinal)
            ? config.LibDir[config.DestDir.Length..]
            : config.LibDir;
        // This is the full path inside the staging DESTDIR (used during packaging)
        var destDirPath = Path.Combine(config.LibDir, relativePath, fileName);
        var finalInstallPath = Path.Combine(libDirWithoutDestDir, relativePath, fileName);
        var enableFallback = !string.IsNullOrWhiteSpace(config.DestDir) && config.EnableWrapperScriptFallback;

        var script =
            // Generates the wrapper shell script for SnapX execution.
            //
            // The script either:
            // 1) Executes the primary installed binary path directly (when fallback is disabled), or
            // 2) Checks the primary path and falls back to the staging (DESTDIR) path if enabled (for packaging/dev).
            //
            // The fallback logic is controlled by the 'enableFallback' boolean,
            // which should only be true during packaging or development.
            //
            // Maintainers: Avoid embedding hardcoded DESTDIR paths in production scripts.
            // Use '--enable-wrapper-fallback' to toggle fallback support cleanly.
            enableFallback ? $"""
                              #!/usr/bin/env sh
                              # SnapX version: {config.SnapXVersion}

                              dir="$(cd -P -- "$(dirname -- "$0")" && pwd -P)"
                              cd "$dir"

                              PRIMARY_PATH="{finalInstallPath}"
                              FALLBACK_PATH="{destDirPath}"

                              if [ -x "$PRIMARY_PATH" ]; then
                                  exec env "$PRIMARY_PATH" "$@"
                              elif [ -x "$FALLBACK_PATH" ]; then
                                  exec env "$FALLBACK_PATH" "$@"
                              else
                                  echo "Error: SnapX binary not found in expected location(s). Primary path: $PRIMARY_PATH Fallback path: $FALLBACK_PATH" >&2
                                  exit 1
                              fi
                              """ : $"""
                                     #!/usr/bin/env sh
                                     # SnapX version: {config.SnapXVersion}

                                     dir="$(cd -P -- "$(dirname -- "$0")" && pwd -P)"
                                     cd "$dir"

                                     exec env "{finalInstallPath}" "$@"
                                     """;
        Logger.Information($"Attempting to write wrapper script\n{script}");
        if (OperatingSystem.IsWindows() && Environment.GetEnvironmentVariable("USE_INSTALL_FOR_WRAPPER_SCRIPT") == null)
        {
            Logger.Information($"Writing to {scriptPath} using .NET directly since you're on Windows. Set environment variable USE_INSTALL_FOR_WRAPPER_SCRIPT=1 to use the normal install command.");
            await File.WriteAllTextAsync(scriptPath, script).ConfigureAwait(false);
        }
        else
        {
            await CommandRunner.RunInstallCommand($"-c \"cat > {scriptPath} <<EOF\n{script.Replace("\"", "\\\"").Replace("$", "\\$")}\nEOF\"", "sh");
            await CommandRunner.RunInstallCommand($"+x {scriptPath}", "chmod");
        }
    }
}
