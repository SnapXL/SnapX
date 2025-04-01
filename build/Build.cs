using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Principal;
using JetBrains.Annotations;
using Newtonsoft.Json.Linq;
using Nuke.Common;
using Nuke.Common.IO;
using Nuke.Common.ProjectModel;
using Nuke.Common.Tools.DotNet;
using Nuke.Common.Tools.NerdbankGitVersioning;
using SnapX.Core.Utils;
using YamlDotNet.Core.Tokens;
using static Nuke.Common.Tools.DotNet.DotNetTasks;
using static Serilog.Log;
using Information = Microsoft.VisualBasic.Information;

class Build : NukeBuild
{
    /// Support plugins are available for:
    ///   - JetBrains ReSharper        https://nuke.build/resharper
    ///   - JetBrains Rider            https://nuke.build/rider
    ///   - Microsoft VisualStudio     https://nuke.build/visualstudio
    ///   - Microsoft VSCode           https://nuke.build/vscode

    public static int Main() => Execute<Build>(x => x.Compile);

    [Parameter("Configuration to build - Default is 'Debug' (local) or 'Release' (server)")]
    readonly Configuration Configuration = IsLocalBuild ? Configuration.Debug : Configuration.Release;
    [Parameter("Output Directory")]
    readonly AbsolutePath OutputDirectory = RootDirectory / "Output";
    readonly AbsolutePath PackagingDirectory = RootDirectory / "packaging";
    const string Namespace = "SnapX.";

    static readonly string[] ProjectNames = ["Avalonia", "CLI", "NativeMessagingHost"];
    readonly string[] ProjectsToBuild = ProjectNames
        .Where(projectName => OperatingSystem.IsLinux() || projectName != "GTK4")
        .Select(projectName => Path.Combine(RootDirectory, Namespace + projectName, Namespace + projectName + ".csproj"))
        .ToArray();
    [Solution(GenerateProjects = true)]
    readonly Solution Solution;

    string _prefix;

    [Parameter("PREFIX")]
    public string Prefix
    {
        get => _prefix ?? "/usr/local";
        set => _prefix = value;
    }
    // When I used MAKEFILES on Windows, I was using MSYS2 that gave me an acceptable UNIX like path
    // Now, I have no idea what to default to on Windows. Good luck.
    string _destdir;
    [Parameter("DESTDIR")]
    public string DestDir
    {
        get => _destdir ?? "";
        set => _destdir = value;
    }

    string Bindir => Path.Join(DestDir, Prefix, "bin");
    string Datadir => Path.Join(DestDir, Prefix, "share");
    string Docdir => Path.Join(Datadir, "doc", "snapx");
    string Licensedir => Path.Join(Datadir, "licenses", "snapx");
    string Applicationsdir => Path.Join(Datadir, "applications");
    string Icondir => Path.Join(Datadir, "icons", "hicolor");
    private string _libdir;

    [Parameter("LIBDIR")]
    public string LibDir
    {
        get => _libdir ?? Path.Join(DestDir, Prefix, "lib");
        set => _libdir = value;
    }
    string Metainfodir => Path.Join(Datadir, "metainfo");
    AbsolutePath Tarballdir => PackagingDirectory / "tarball";
    string packagingDir => Path.Combine(PackagingDirectory, "usr");
    /*
    Project NMH => Solution.SnapX_NativeMessagingHost;
    */

    string NMHassemblyName => "SnapX_NativeMessagingHost";
    [Parameter("Path to NativeMessagingHost for web extension support")]
    string NMHostPath => !OperatingSystem.IsWindows() ? Path.Join(LibDir, "snapx", NMHassemblyName) : null;

    [Parameter("Runtime you're compiling for")]
    string Runtime = RuntimeInformation.RuntimeIdentifier;

    [NerdbankGitVersioning][CanBeNull] readonly NerdbankGitVersioning NerdbankVersioning;

    string SnapXVersion =>
        NerdbankVersioning?.AssemblyInformationalVersion ?? Environment.GetEnvironmentVariable("VERSION");

    Target Clean => _ => _
        .Before(Restore)
        .Executes(() =>
        {
            DotNetClean();
            OutputDirectory.CreateOrCleanDirectory();
            Tarballdir.CreateOrCleanDirectory();

        });

    Target Restore => _ => _
        .Executes(() =>
        {
            foreach (var project in ProjectsToBuild)
            {
                DotNetRestore(s => s
                    .SetProjectFile(project));
            }

        });

    Target Compile => _ => _
        .DependsOn(Restore)
        .Executes(() =>
        {
            var projectsThatWereBuilt = new Collection<Project>();
            // Build each project and copy the artifacts to Output
            foreach (var project in ProjectsToBuild)
            {
                var projectName = Path.GetFileNameWithoutExtension(project);
                var projectData = Solution.GetProject(projectName);
                projectsThatWereBuilt.Add(projectData);
                var assemblyName = projectData.GetProperty("AssemblyName")!;
                var projectOutput = Path.Combine(OutputDirectory, assemblyName);
                Information($"Publishing {project}");
                DotNetPublish(s => s
                    .SetProject(project)
                    .SetConfiguration(Configuration)
                    .SetOutput(projectOutput)
                    .SetAssemblyVersion(SnapXVersion)
                    .SetRuntime(Runtime)
                    .EnableNoLogo()
                    .EnableNoRestore());
                Information($"Artifacts for {projectName} output to {OutputDirectory}");
            }

            if (ProjectsToBuild.Any(projectName => projectName.Contains("NativeMessagingHost")))
            {

                var projectOutput = Path.Combine(OutputDirectory, NMHassemblyName);

                var exeProjects = projectsThatWereBuilt
                    .Where(p => p.GetOutputType() == "Exe" && !p.Name.Contains("NativeMessagingHost"));

                foreach (var exeProject in exeProjects)
                {
                    var exeFileName = exeProject.GetProperty("AssemblyName")!;
                    var exeOutputDirectory = Path.Combine(OutputDirectory, exeFileName, NMHassemblyName);
                    if (OperatingSystem.IsWindows() && !exeOutputDirectory.EndsWith(".exe")) exeOutputDirectory += ".exe";
                    var sourceNMHOutputPath = Path.Combine(projectOutput, NMHassemblyName);
                    if (OperatingSystem.IsWindows()) sourceNMHOutputPath += ".exe";

                    File.Copy(sourceNMHOutputPath, exeOutputDirectory, overwrite: true);
                }

                if (Directory.Exists(projectOutput))
                {
                    Directory.Delete(projectOutput, recursive: true);
                }
            }
            var manifestFiles = Directory.GetFiles(OutputDirectory, "host-manifest-*.json", SearchOption.AllDirectories);
            foreach (var manifestFile in manifestFiles)
            {
                // Pump the brakes, is that Newtonsoft.JSON in disguise?!?!
                var json = JObject.Parse(File.ReadAllText(manifestFile));
                if (string.IsNullOrWhiteSpace(NMHostPath))
                {
                    Information($"Skipping {manifestFile} since NMHostPath was not provided");
                    continue;
                }

                json["path"] = NMHostPath;

                File.WriteAllText(manifestFile, json.ToString());
            }
        });
    Target Install => _ => _
        .DependsOn(Compile)
        .Executes(() =>
        {
            Information($"Destination Directory: {DestDir}");
            Information($"Prefix: {Prefix}");
            Information($"Installing to {Path.Join(DestDir, Prefix)}");
            Information($"Data directory: {Datadir}");
            Information($"Bin directory: {Bindir}");
            Information($"Documentation directory: {Docdir}");
            Information($"License directory: {Licensedir}");
            Information($"Metainfo directory: {Metainfodir}");
            Information($"Tarball directory: {Tarballdir}");
            Information($"Application directory: {Applicationsdir}");
            Information($"Icon directory: {Icondir}");
            Information($"Library directory: {LibDir}");
            Information($"Operating System: {RuntimeInformation.OSDescription}");
            Information($"SnapX Version: {SnapXVersion}");
            Information($"Architecture: {RuntimeInformation.OSArchitecture} {RuntimeInformation.RuntimeIdentifier}");

            var files = Directory.GetFiles(packagingDir, "*", SearchOption.AllDirectories);

            foreach (var sourceFile in files)
            {
                var relativePath = Path.GetRelativePath(packagingDir, sourceFile);
                var destinationFile = Path.Join(DestDir, Prefix, relativePath);

                EnsureDirectoryExists(Path.GetDirectoryName(destinationFile));

                var permissions = "0644";

                switch (sourceFile)
                {
                    case var file when file.EndsWith(".desktop"):
                        permissions = "0755";
                        destinationFile = Path.Join(Applicationsdir, Path.GetFileName(file));
                        Information($"Installing desktop file: {relativePath} -> {destinationFile}");
                        break;
                    case var file when file.EndsWith(".metainfo.xml"):
                        destinationFile = Path.Join(Metainfodir, Path.GetFileName(file));
                        Information($"Installing metainfo file: {relativePath} -> {destinationFile}");
                        break;
                    case var file when file.EndsWith(".md", StringComparison.OrdinalIgnoreCase):
                        destinationFile = Path.Join(Docdir, Path.GetFileName(file));
                        Information($"Installing documentation file: {relativePath} -> {destinationFile}");
                        break;
                    default:
                        Information($"Installing {Path.GetExtension(sourceFile)} file: {relativePath} -> {destinationFile}");
                        break;
                }

                InstallFile(sourceFile, destinationFile, permissions);
            }
            // Install License
            InstallFile(Path.Join(RootDirectory, "LICENSE.md"), Path.Join(Licensedir, "LICENSE.md"), "0755");
            var documentation = Directory.GetFiles(RootDirectory, "*.md", SearchOption.TopDirectoryOnly);

            foreach (var docFile in documentation)
            {
                if (docFile.ToLower().Contains("license")) continue;
                InstallFile(docFile, Path.Join(Docdir, Path.GetFileName(docFile)), "0755");
            }

            var outputFiles = OutputDirectory.GetFiles("*", 5).OrderBy(f => f.Name).ToArray();
            foreach (var outputFile in outputFiles)
            {
                var permissions = "0755";
                var destinationFile = Path.Join(Bindir, Path.GetFileName(outputFile));
                var AvaloniaAssemblyName = "snapx-ui" + (OperatingSystem.IsWindows() ? ".exe" : "");

                switch (Path.GetFileNameWithoutExtension(destinationFile))
                {
                    case var name when destinationFile.Contains(".dbg") || destinationFile.Contains(".pdb"):
                        continue;
                    case var name when destinationFile.Contains(NMHassemblyName):
                        destinationFile = NMHostPath;
                        Information($"Installing NMH Binary: {Path.GetRelativePath(RootDirectory, outputFile)} -> {destinationFile}");
                        break;
                    case var name when destinationFile.Contains(AvaloniaAssemblyName):
                        destinationFile = Path.Join(LibDir, "snapx", Path.GetFileName(destinationFile));
                        Information($"Installing AVALONIABINARY: {Path.GetRelativePath(RootDirectory, outputFile)} -> {destinationFile}");
                        break;
                    case var name when (destinationFile.Contains(".dll") || destinationFile.Contains(".so") || destinationFile.Contains(".dylib")) && !destinationFile.Contains(AvaloniaAssemblyName):
                        destinationFile = Path.Join(LibDir, "snapx", Path.GetFileName(destinationFile));
                        Information($"Installing {Path.GetExtension(destinationFile)}: {Path.GetRelativePath(RootDirectory, outputFile)} -> {destinationFile}");
                        break;
                    case var name when destinationFile.Contains(".json"):
                        destinationFile = Path.Join(Datadir, "SnapX", Path.GetFileName(destinationFile));
                        Information($"Installing {Path.GetExtension(destinationFile)}: {Path.GetRelativePath(RootDirectory, outputFile)} -> {destinationFile}");
                        break;
                    default:
                        Information($"Installing binary: {Path.GetRelativePath(RootDirectory, outputFile)} -> {destinationFile}");
                        break;
                }
                InstallFile(outputFile, destinationFile, permissions);
            }

            var localAvaloniaWrapperScript = Path.Join(RootDirectory, "snapx-ui");

            var avaloniaPath = Path.Join(Prefix, "lib", "snapx", "snapx-ui");
            var fallbackPath = Path.Join(LibDir, "snapx", "snapx-ui");
            using (var writer = new StreamWriter(localAvaloniaWrapperScript))
            {
                writer.WriteLine("#!/bin/sh");
                writer.WriteLine("# Wrapper script provided by SnapX to invoke the true Avalonia binary.");
                writer.WriteLine($"# NMH Path: {NMHostPath}");
                writer.WriteLine($"# Version: {SnapXVersion}");
                writer.WriteLine(@$"
if [ -f ""{avaloniaPath}"" ]; then
    exec {avaloniaPath} ""$@""
else
    exec {fallbackPath} ""$@""
fi
");
            }

            InstallFile(localAvaloniaWrapperScript, Path.Join(Bindir, "snapx-ui"), "0755");
            RunInstallCommand($"+x {Path.Join(Bindir, "snapx-ui")}", "chmod");
            File.Delete(localAvaloniaWrapperScript);
        });
    void InstallFile(string source, string destination, string permissions)
    {
        if (File.Exists(source))
        {
            var installArgs = $"-Dpm {permissions} {source} {destination}";
            RunInstallCommand(installArgs);
        }
        else
        {
            Information($"Source file not found: {source}");
        }
    }

    void RunInstallCommand(string installArguments, string executionCommand = "install")
    {
        var requiresElevationLikely = !IsAdmin() && RequiresElevationLikely(installArguments);

        var executionArguments = installArguments;

        if (requiresElevationLikely)
        {
            executionArguments = $"{executionCommand} " + installArguments;
            executionCommand = "sudo";
        }

        var processStartInfo = new ProcessStartInfo
        {
            FileName = executionCommand,
            Arguments = executionArguments,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };

        try
        {
            using var process = Process.Start(processStartInfo);
            if (process == null) throw new Exception($"Failed to start {processStartInfo.FileName} {processStartInfo.Arguments}");
            process.WaitForExit();

            if (process.ExitCode != 0)
            {
                var errorOutput = process.StandardError.ReadToEnd();
                Error($"Install command failed: {errorOutput}");

                // Check if permission denied and elevation wasn't already attempted
                if (!requiresElevationLikely && errorOutput.Contains("Permission denied"))
                {
                    Error("Retrying with elevated privileges (sudo)...");
                    requiresElevationLikely = true;
                    RunInstallCommand(installArguments);
                }
            }
            else
            {
                Debug($"{processStartInfo.FileName} {processStartInfo.Arguments}");
                Debug($"Install command succeeded. {process.StandardOutput.ReadToEnd()}");
            }
        }
        catch (System.ComponentModel.Win32Exception ex)
        {
            // Handle cases where sudo isn't available (e.g., on Windows)
            if (ex.Message.Contains("The system cannot find the file specified") && requiresElevationLikely)
            {
                Error("Elevation utility (sudo) is not available. Ensure it's installed or run as root.");
            }
            else
            {
                Error($"Error starting process: {ex.Message}");
            }
        }
    }
    bool IsAdmin()
    {
        return Helpers.IsAdministrator();
    }

    // Helper function to detect if elevation (sudo) is LIKELY needed based on arguments
    bool RequiresElevationLikely(string installArguments)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return false; // No elevation (sudo) on Windows
        }

        // Split arguments and check for paths starting with commonly protected directories
        var arguments = installArguments.Split(new char[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);

        foreach (var argument in arguments)
        {
            var arg = argument.Trim();
            if (arg.StartsWith("/usr") || arg.StartsWith("/opt") || arg.StartsWith("/etc") || arg.StartsWith("/var") || arg.StartsWith("/bin") || arg.StartsWith("/sbin"))
            {
                return true; // Likely requires elevation
            }
        }

        return false; // Probably doesn't require elevation
    }

    void EnsureDirectoryExists(string directory)
    {
        if (Directory.Exists(directory)) return;
        Information($"Creating directory: {directory}");
        RunInstallCommand($"-p {directory}", "mkdir");
    }
}
