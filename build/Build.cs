using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace DefaultNamespace;

public class Build(IBuildLogger Logger, ICommandRunner CommandRunner, IFileSystem FileSystem, BuildConfig config)
{
    private bool _hasLoggedInfo;

    public async Task ProcessBuildProject(
        string project)
    {
        LogBuildInfo();

        var index = Array.IndexOf(config.projectsToBuild, project);
        var assemblyName = config.knownAssemblyNames[index];
        var ridPart = $"-r {config.Runtime}";
        var arch = RuntimeInformation.OSArchitecture.ToString().ToLowerInvariant();
        var isUnsupportedArch = arch is "s390x" or "ppc64le";
        var ridArg = OperatingSystem.IsLinux() && !isUnsupportedArch ? "" : ridPart;

        await CommandRunner.RunAsync("dotnet", $"publish \"{project}\" --configuration {config.Configuration} --nologo -o \"{Path.Combine(config.OutputDir, assemblyName)}\" {ridArg} {config.ExtraArgs}");

        if (project.Contains("NativeMessagingHost"))
        {
            await HandleNativeMessagingHost(assemblyName, config.OutputDir, config.LibDir, config.projectsToBuild.Where(p => !p.Contains("NativeMessagingHost")));
            await HandleRustLibCopy(config.RootDirectory, config.OutputDir);
        }
    }

    private void LogBuildInfo()
    {
        if (_hasLoggedInfo) return;
        Logger.Information($"Operating System: {RuntimeInformation.OSDescription}");
        Logger.Information($"SnapX Version: {config.SnapXVersion}");
        Logger.Information($"Architecture: {RuntimeInformation.OSArchitecture}");
        Logger.Information($"Runtime Identifier: {RuntimeInformation.RuntimeIdentifier}");
        _hasLoggedInfo = true;
    }

    private async Task HandleNativeMessagingHost(string assemblyName, string outputDir, string libDir, IEnumerable<string> otherProjects)
    {
        var finalAssemblyName = assemblyName;
        if (OperatingSystem.IsWindows()) finalAssemblyName += ".exe";
        var sourceNMHOutputPath = Path.Combine(outputDir, assemblyName, finalAssemblyName);

        foreach (var builtProject in otherProjects)
        {
            var builtAssemblyName = config.knownAssemblyNames[Array.IndexOf(config.projectsToBuild, builtProject)];
            FileSystem.FileCopy(sourceNMHOutputPath, Path.Combine(outputDir, builtAssemblyName, finalAssemblyName), overwrite: true);
        }

        FileSystem.DirectoryDelete(Path.Combine(outputDir, assemblyName), true);

        var manifestFiles = FileSystem.DirectoryGetFiles(outputDir, "host-manifest-*.json", SearchOption.AllDirectories);
        foreach (var manifestFile in manifestFiles)
        {
            var json = JsonNode.Parse(await FileSystem.FileReadAllTextAsync(manifestFile))?.AsObject();
            var NMHostPath = !OperatingSystem.IsWindows() ? Path.Join(libDir, "snapx", assemblyName) : null;

            if (string.IsNullOrWhiteSpace(NMHostPath))
            {
                Logger.Information($"Skipping {manifestFile} since NMHostPath was not provided");
                continue;
            }
            if (json is null) continue;
            json["path"] = NMHostPath;

            await FileSystem.FileWriteAllTextAsync(manifestFile, json.ToJsonString(new JsonSerializerOptions
            {
                WriteIndented = true,
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            }));
        }
    }

    private Task HandleRustLibCopy(string rootDirectory, string outputDir)
    {
        var rustLib = OperatingSystem.IsLinux() ? "libsnapxrust.so" : "libsnapxrust.dylib";
        var sourcePath = Path.Combine(rootDirectory, "SnapX.Core", "ScreenCapture", "Rust", "target", "release", rustLib);

        if (!File.Exists(sourcePath)) return Task.CompletedTask;

        foreach (var dir in FileSystem.DirectoryGetDirectories(outputDir, "*", SearchOption.AllDirectories))
        {
            var destinationPath = Path.Combine(dir, rustLib);
            FileSystem.FileCopy(sourcePath, destinationPath, overwrite: true);
        }

        return Task.CompletedTask;
    }
}
