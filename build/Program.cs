using System.CommandLine;
using System.CommandLine.Invocation;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Xml.Linq;
using Bullseye;
using static Bullseye.Targets;
using static SimpleExec.Command;

// I am on a limited timeframe for NUKING `NUKE.Build`
// Sorry for the crimes against programming

internal class Program
{
    // When I used MAKEFILES on Windows, I was using MSYS2 that gave me an acceptable UNIX like path
    // Now, I have no idea what to default to on Windows. Good luck.
    string _destdir = ""; // Initialize directly
    public string DestDir
    {
        get => _destdir;
        set => _destdir = value ?? ""; // Ensure it's not set to null
    }

    private string _prefix = "/usr/local"; // Initialize directly
    public string Prefix
    {
        get => _prefix;
        set => _prefix = value ?? "/usr/local"; // Ensure it's not set to null, fallback to default
    }

    string BinDir => Path.Join(DestDir, Prefix, "bin");
    string Datadir => Path.Join(DestDir, Prefix, "share");
    private string Docdir { get; set; }
    string Licensedir => Path.Join(Datadir, "licenses", "snapx");
    string Applicationsdir => Path.Join(Datadir, "applications");
    string Icondir => Path.Join(Datadir, "icons", "hicolor");
    string Runtime { get; set; } = RuntimeInformation.RuntimeIdentifier;
    string Metainfodir => Path.Join(Datadir, "metainfo");
    public static readonly string RootDirectory = FindRoot();
    readonly string PackagingDirectory = Path.Combine(RootDirectory, "packaging");
    string Tarballdir => Path.Combine(PackagingDirectory, "tarball");
    string PackagingUsrDir => Path.Combine(PackagingDirectory, "usr");
    private string NMHassemblyName => GetAssemblyNameFromProject(projectsToBuild[^1]);

    public void ApplyCLIOverrides(string? destDir, string? prefix, string? libDir, string? docDir)
    {
        if (destDir is not null) DestDir = destDir;
        if (prefix is not null) Prefix = prefix;
        if (libDir is not null) LibDir = libDir;
        Docdir = docDir;
    }
    private string NMHostPath => !OperatingSystem.IsWindows() ? Path.Join(LibDir, "snapx", NMHassemblyName) : null;

    private string? _libdir; // Nullable backing field
    const string Namespace = "SnapX.";

    // SnapX.NativeMessagingHost must be compiled *last*
    static readonly string[] ProjectNames = ["Avalonia", "CLI", "NativeMessagingHost"];
    readonly string[] projectsToBuild = ProjectNames
        .Where(projectName => OperatingSystem.IsLinux() || projectName != "GTK4")
        .Select(projectName => Path.Combine(Path.GetRelativePath(Directory.GetCurrentDirectory(), RootDirectory), Namespace + projectName))
        .ToArray();
    public string LibDir
    {
        get => _libdir ?? Path.Join(DestDir, Prefix, "lib");
        set => _libdir = value;
    }
    private static readonly Dictionary<string, string> StepAliases = new(StringComparer.OrdinalIgnoreCase)
    {
        { "compile", "build" }
    };

    private HashSet<string> _skippedSteps = new(StringComparer.OrdinalIgnoreCase);

    public void SetSkippedSteps(IEnumerable<string>? steps)
    {
        _skippedSteps = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        if (steps is null) return;

        foreach (var step in steps)
        {
            var normalized = StepAliases.TryGetValue(step, out var canonical)
                ? canonical
                : step;

            _skippedSteps.Add(normalized);
        }
    }

    public bool ShouldSkip(string stepName)
    {
        var normalized = StepAliases.GetValueOrDefault(stepName, stepName);

        return _skippedSteps.Contains(normalized);
    }

    private static string FindRoot()
    {
        var dir = new DirectoryInfo(Directory.GetCurrentDirectory());

        while (dir != null)
        {
            var buildScript = Path.Combine(dir.FullName, "build.sh"); // Assuming build.sh marks the root
            if (File.Exists(buildScript))
                return dir.FullName;

            dir = dir.Parent;
        }

        throw new InvalidOperationException("Could not locate root directory (no build.sh found in any parent directory).");
    }
    private static string GetAssemblyNameFromProject(string projectPath)
    {
        var csprojFiles = Directory.GetFiles(projectPath, "*.csproj");
        if (csprojFiles.Length != 1)
        {
            throw new FileNotFoundException(
                $"ERROR: Expected exactly one .csproj in '{projectPath}' but found {csprojFiles.Length}. Searched in: {Path.GetFullPath(projectPath)}");
        }

        var csprojPath = csprojFiles[0];
        var xml = XDocument.Load(csprojPath);

        var assemblyNameElement = xml
            .Descendants("PropertyGroup")
            .Elements("AssemblyName")
            .FirstOrDefault();

        var csprojFileName = Path.GetFileNameWithoutExtension(csprojPath);
        return assemblyNameElement?.Value?.Trim() ?? csprojFileName.Replace('.', '_');
    }

    // This method is now an instance method and will be called on an instance of Program
    private async Task ExecuteBuildAsync(
        string[] targets,
        bool clear,
        bool dryRun,
        Host? host,
        bool listDependencies,
        bool listInputs,
        bool listTargets,
        bool listTree,
        bool noColor,
        bool noExtendedChars,
        bool parallel,
        bool skipDependencies,
        bool verbose,
        string outputDir,
        string configuration,
        string extraArgs)
    {
        var targetsToRun = targets;
        if (targetsToRun == null || targetsToRun.Length == 0)
        {
            targetsToRun = ["default"]; // Default target if none specified
        }

        var snapXVersion = ThisAssembly.AssemblyInformationalVersion;
        var hasLoggedInfo = false;

        var bullseyeOptions = new Options
        {
            Clear = clear,
            DryRun = dryRun,
            Host = host,
            ListDependencies = listDependencies,
            ListInputs = listInputs,
            ListTargets = listTargets,
            ListTree = listTree,
            NoColor = noColor,
            NoExtendedChars = noExtendedChars,
            Parallel = parallel,
            SkipDependencies = skipDependencies,
            Verbose = verbose,
        };

        Target("format", async () =>
        {
            if (ShouldSkip("format")) return;
            await RunAsync("dotnet", "format --verify-no-changes");
        });
        Target("clean", () =>
        {
            if (ShouldSkip("clean")) return;
            try
            {
                Information($"Cleaning output directory: {outputDir}");
                if (Directory.Exists(outputDir))
                {
                    Directory.Delete(outputDir, true);
                }
                Directory.CreateDirectory(outputDir);
                Information($"Output directory cleaned and recreated: {outputDir}");
            }
            catch (Exception ex)
            {
                Warning($"Warning: Could not clean and recreate output directory '{outputDir}'. Error: {ex.Message}");
            }
        });

        Target("build",
            dependsOn: ShouldSkip("build") ? [] : ["clean"],
            forEach: projectsToBuild,
            async (project) =>
            {
                if (ShouldSkip("build")) return;
                if (!hasLoggedInfo)
                {
                    Information($"Operating System: {RuntimeInformation.OSDescription}");
                    Information($"SnapX Version: {snapXVersion}");
                    Information($"Architecture: {RuntimeInformation.OSArchitecture}");
                    Information($"Runtime Identifier: {RuntimeInformation.RuntimeIdentifier}");
                    hasLoggedInfo = true;
                }

                var assemblyName = GetAssemblyNameFromProject(project);

                var ridPart = $"-r {RuntimeInformation.RuntimeIdentifier}";

                var arch = RuntimeInformation.OSArchitecture.ToString().ToLowerInvariant();

                var isUnsupportedArch = arch is "s390x" or "ppc64le";

                var ridArg = OperatingSystem.IsLinux() && !isUnsupportedArch ? "" : ridPart;

                await RunAsync("dotnet", $"publish \"{project}\" --configuration {configuration} --nologo -o \"{Path.Combine(outputDir, assemblyName)}\" {ridArg} {extraArgs}");
                if (project.Contains("NativeMessagingHost"))
                {
                    foreach (var builtProject in projectsToBuild.Where(p => !p.Contains("NativeMessagingHost")))
                    {
                        var finalAssemblyName = assemblyName;
                        if (OperatingSystem.IsWindows()) finalAssemblyName += ".exe";
                        var sourceNMHOutputPath = Path.Combine(outputDir, assemblyName, finalAssemblyName);
                        var builtAssemblyName = GetAssemblyNameFromProject(builtProject);
                        File.Copy(sourceNMHOutputPath, Path.Combine(outputDir, builtAssemblyName, finalAssemblyName), overwrite: true);
                    }
                    Directory.Delete(Path.Combine(outputDir, assemblyName), true);
                    var manifestFiles = Directory.GetFiles(outputDir, "host-manifest-*.json", SearchOption.AllDirectories);
                    foreach (var manifestFile in manifestFiles)
                    {
                        var json = JsonNode.Parse(await File.ReadAllTextAsync(manifestFile))?.AsObject();
                        var NMHostPath = !OperatingSystem.IsWindows() ? Path.Join(LibDir, "snapx", assemblyName) : null;

                        if (string.IsNullOrWhiteSpace(NMHostPath))
                        {
                            Information($"Skipping {manifestFile} since NMHostPath was not provided");
                            continue;
                        }
                        json["path"] = NMHostPath;


                        await File.WriteAllTextAsync(manifestFile, json.ToJsonString(new JsonSerializerOptions
                        {
                            WriteIndented = true,
                            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                        }));
                    }
                    var rustLib = OperatingSystem.IsLinux() ? "libsnapxrust.so" : "libsnapxrust.dylib";
                    var sourcePath = Path.Combine(RootDirectory, "SnapX.Core", "ScreenCapture", "Rust", "target", "release", rustLib);
                    if (!File.Exists(sourcePath)) return;
                    foreach (var dir in Directory.GetDirectories(outputDir, "*", SearchOption.AllDirectories))
                    {
                        var destinationPath = Path.Combine(dir, rustLib);
                        File.Copy(sourcePath, destinationPath, overwrite: true);
                    }
                }
            });

        Target("install",
            dependsOn: ["build"],
            async () =>
            {
                Docdir ??= Path.Join(Datadir, "doc", "snapx");
                Information($"--- Installation Paths ---");
                Information($"Destination Directory (DESTDIR): {DestDir}");
                Information($"Prefix: {Prefix}");
                Information($"Install Root: {Path.Join(DestDir, Prefix)}");
                Information($"Data directory: {Datadir}");
                Information($"Bin directory: {BinDir}");
                Information($"Documentation directory: {Docdir}");
                Information($"License directory: {Licensedir}");
                Information($"Metainfo directory: {Metainfodir}");
                Information($"Tarball directory: {Tarballdir}"); // Uses instance property
                Information($"Application directory: {Applicationsdir}");
                Information($"Icon directory: {Icondir}");
                Information($"Library directory: {LibDir}"); // Uses instance property
                Information($"Packaging User Directory: {PackagingUsrDir}"); // Uses instance property
                var files = Directory.GetFiles(PackagingUsrDir, "*", SearchOption.AllDirectories);
                foreach (var sourceFile in files)
                {
                    var relativePath = Path.GetRelativePath(PackagingUsrDir, sourceFile);
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
                InstallFile(Path.Join(RootDirectory, "LICENSE.md"), Path.Join(Licensedir, "LICENSE.md"), "0755");
                var documentation = Directory.GetFiles(RootDirectory, "*.md", SearchOption.TopDirectoryOnly);

                foreach (var docFile in documentation)
                {
                    if (docFile.ToLower().Contains("license")) continue;
                    InstallFile(docFile, Path.Join(Docdir, Path.GetFileName(docFile)), "0755");
                }
                var outputFiles = Directory.GetFiles(outputDir, "*", SearchOption.AllDirectories)
                    .OrderBy(Path.GetFileName)
                    .ToArray();
                foreach (var outputFile in outputFiles)
                {
                    var permissions = "0755";
                    var destinationFile = Path.Join(BinDir, Path.GetFileName(outputFile));
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
                            destinationFile = Path.Join(BinDir, Path.GetFileName(destinationFile));
                            Information($"Installing AVALONIABINARY: {Path.GetRelativePath(RootDirectory, outputFile)} -> {destinationFile}");
                            break;
                        case var name when (destinationFile.Contains(".dll") || destinationFile.Contains(".so") || destinationFile.Contains(".dylib")) && !destinationFile.Contains(AvaloniaAssemblyName):
                            destinationFile = Path.Join(BinDir, Path.GetFileName(destinationFile));
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
                await Task.CompletedTask; // To satisfy async
            });
        Target("default", dependsOn: ["build"]);

        await RunTargetsAndExitAsync(targetsToRun, bullseyeOptions);
    }

    internal static async Task<int> Main(string[] args)
    {
        var rootCommand = new RootCommand("Build and configure the application.");

        var targetsArgument = new Argument<string[]>("targets")
        {
            Description = "A list of targets to run or list. If not specified, the \"default\" target will be run, or all targets will be listed.",
            Arity = ArgumentArity.ZeroOrMore
        };
        rootCommand.AddArgument(targetsArgument);

        var clearOption = new Option<bool>(["--clear", "-c"], "Clear the console before execution.");
        rootCommand.AddOption(clearOption);

        var dryRunOption = new Option<bool>(["--dry-run", "-n"], "Do a dry run without executing actions. (Passed to Bullseye)"); // Changed -d to -n to avoid conflict if destdir is added
        rootCommand.AddOption(dryRunOption);

        var hostOption = new Option<Host?>(["--host"], "Force the mode for a specific host environment (normally auto-detected). Valid values: AppVeyor, AzurePipelines, GitHubActions, GitLabCI, TeamCity, Travis, etc.");
        rootCommand.AddOption(hostOption);

        var listDependenciesOption = new Option<bool>("--list-dependencies", "List all (or specified) targets and dependencies, then exit.");
        rootCommand.AddOption(listDependenciesOption);

        var listInputsOption = new Option<bool>("--list-inputs", "List all (or specified) targets and inputs, then exit.");
        rootCommand.AddOption(listInputsOption);

        var listTargetsOption = new Option<bool>(["--list-targets", "-l", "-t"], "List all (or specified) targets, then exit.");
        rootCommand.AddOption(listTargetsOption);

        var listTreeOption = new Option<bool>("--list-tree", "List all (or specified) targets and dependency trees, then exit.");
        rootCommand.AddOption(listTreeOption);

        var noColorOption = new Option<bool>("--no-color", "Disable colored output.");
        rootCommand.AddOption(noColorOption);

        var noExtendedCharsOption = new Option<bool>("--no-extended-chars", "Disable extended characters (for PagerDuty or other hosts which don't support them).");
        rootCommand.AddOption(noExtendedCharsOption);

        var parallelOption = new Option<bool>(["--parallel", "-p"], "Run targets in parallel.");
        rootCommand.AddOption(parallelOption);

        var skipDependenciesOption = new Option<bool>(["--skip-dependencies", "-s"], "Do not run targets' dependencies.");
        rootCommand.AddOption(skipDependenciesOption);

        var verboseOption = new Option<bool>(["--verbose", "-v"], "Enable verbose output.");

        rootCommand.AddOption(verboseOption);
        var skipStepOption = new Option<string[]>(
            name: "--skip",
            description: "Comma-separated list of steps to skip (e.g., build,install,compile).")
        {
            Arity = ArgumentArity.ZeroOrMore
        };
        skipStepOption.AllowMultipleArgumentsPerToken = true;
        rootCommand.AddOption(skipStepOption);

        var outputDirOption = new Option<string>(
            name: "--output-dir",
            description: "The directory to output builds artifacts to.")
        {
            Arity = ArgumentArity.ExactlyOne
        };
        outputDirOption.SetDefaultValue("Output");
        outputDirOption.AddAlias("-o");
        rootCommand.AddOption(outputDirOption);

        var configurationOption = new Option<string>(
            name: "--configuration",
            description: "Build configuration (e.g., Debug or Release).")
        {
            Arity = ArgumentArity.ExactlyOne
        };
        configurationOption.SetDefaultValue("Release"); // Changed default to Release, common for builds
        configurationOption.AddAlias("--config");
        rootCommand.AddOption(configurationOption);

        var extraArgsOption = new Option<string>(
            name: "--extra-args",
            description: "Extra arguments to pass to the dotnet publish command.")
        {
            Arity = ArgumentArity.ExactlyOne,
        };
        extraArgsOption.SetDefaultValue("");
        rootCommand.AddOption(extraArgsOption);
        var destDirOption = new Option<string>(
            name: "--dest-dir",
            description: "Destination directory for installation output.")
        {
            Arity = ArgumentArity.ExactlyOne
        };
        rootCommand.AddOption(destDirOption);

        var prefixOption = new Option<string>(
            name: "--prefix",
            description: "Installation prefix path (e.g., /usr or /usr/local).")
        {
            Arity = ArgumentArity.ExactlyOne
        };
        rootCommand.AddOption(prefixOption);

        var libDirOption = new Option<string>(
            name: "--lib-dir",
            description: "Library directory relative to the prefix (e.g., lib or lib64).")
        {
            Arity = ArgumentArity.ExactlyOne
        };
        rootCommand.AddOption(libDirOption);
        var docDirOption = new Option<string>(
            name: "--doc-dir",
            description: "spit out the readme.md somewhere")
        {
            Arity = ArgumentArity.ExactlyOne
        };
        rootCommand.AddOption(docDirOption);
        rootCommand.SetHandler((Func<InvocationContext, Task>)Handler);

        return await rootCommand.InvokeAsync(args);

        // Define the handler delegate with an explicit type to resolve ambiguity
        async Task Handler(InvocationContext invocationContext)
        {
            Console.BackgroundColor = ConsoleColor.Black;
            Console.ForegroundColor = ConsoleColor.White;
            var targets = invocationContext.ParseResult.GetValueForArgument(targetsArgument) ?? [];
            var clear = invocationContext.ParseResult.GetValueForOption(clearOption);
            var dryRun = invocationContext.ParseResult.GetValueForOption(dryRunOption);
            var host = invocationContext.ParseResult.GetValueForOption(hostOption);
            var listDependencies = invocationContext.ParseResult.GetValueForOption(listDependenciesOption);
            var listInputs = invocationContext.ParseResult.GetValueForOption(listInputsOption);
            var listTargets = invocationContext.ParseResult.GetValueForOption(listTargetsOption);
            var listTree = invocationContext.ParseResult.GetValueForOption(listTreeOption);
            var noColor = invocationContext.ParseResult.GetValueForOption(noColorOption);
            var noExtendedChars = invocationContext.ParseResult.GetValueForOption(noExtendedCharsOption);
            var parallel = invocationContext.ParseResult.GetValueForOption(parallelOption);
            var skipDependencies = invocationContext.ParseResult.GetValueForOption(skipDependenciesOption);
            var verbose = invocationContext.ParseResult.GetValueForOption(verboseOption);
            var outputDir = invocationContext.ParseResult.GetValueForOption(outputDirOption) ?? "Output";
            var configuration = invocationContext.ParseResult.GetValueForOption(configurationOption) ?? "Release";
            var extraArgs = invocationContext.ParseResult.GetValueForOption(extraArgsOption) ?? "";
            var programInstance = new Program();
            var destDir = invocationContext.ParseResult.GetValueForOption(destDirOption);
            var prefix = invocationContext.ParseResult.GetValueForOption(prefixOption);
            var libDir = invocationContext.ParseResult.GetValueForOption(libDirOption);
            var docDir = invocationContext.ParseResult.GetValueForOption(docDirOption);

            programInstance.SetSkippedSteps(invocationContext.ParseResult.GetValueForOption(skipStepOption));
            programInstance.ApplyCLIOverrides(destDir, prefix, libDir, docDir);

            await programInstance.ExecuteBuildAsync(targets, clear, dryRun, host, listDependencies, listInputs, listTargets, listTree, noColor, noExtendedChars, parallel, skipDependencies, verbose, outputDir, configuration, extraArgs);
        }
    }

    // Logging helpers (can be static or instance, kept as instance for now)
    void Error(string message) => Console.Error.WriteLine($"ERROR: {message}");
    void Warning(string message) => Console.WriteLine($"WARNING: {message}");
    void Information(string message) => Console.WriteLine(message);
    void Debug(string message) => Console.WriteLine($"DEBUG: {message}");

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

                if (requiresElevationLikely || !errorOutput.Contains("Permission denied")) return;
                Error("Retrying with elevated privileges (sudo)...");
                requiresElevationLikely = true;
                RunInstallCommand(installArguments);
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

    bool RequiresElevationLikely(string installArguments)
    {
        if (Environment.GetEnvironmentVariable("ELEVATION_NOT_NEEDED") == "1") return false;
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            // On Windows, elevation is handled differently (e.g., run as admin).
            // This simple check is for Unix-like systems.
            return false;
        }

        // Common-protected directories on Unix-like systems
        string[] protectedPaths = ["/usr", "/opt", "/etc", "/var", "/bin", "/sbin"];
        var arguments = installArguments.Split([' ', '\t'], StringSplitOptions.RemoveEmptyEntries);
        var hasWarned = false;
        return arguments.Select(argument => argument.Trim()).Any(arg => protectedPaths.Any(p => arg.StartsWith(p)));
    }

    void EnsureDirectoryExists(string directory)
    {
        if (Directory.Exists(directory)) return;
        Information($"Creating directory: {directory}");
        try
        {
            // Attempt to create directory directly first.
            // This might require elevation if it's a protected path.
            Directory.CreateDirectory(directory);
            Information($"Successfully created directory: {directory}");
        }
        catch (UnauthorizedAccessException)
        {
            Warning($"Failed to create directory '{directory}' due to permissions. Attempting with install command.");
            // Fallback to RunInstallCommand which might use sudo
            // Note: 'mkdir -p' is idempotent and creates parent directories.
            RunInstallCommand($"-p \"{directory}\"", "mkdir");
        }
        catch (Exception ex)
        {
            Error($"Failed to create directory '{directory}': {ex.Message}");
            throw; // Rethrow if it's not an auth issue handled by RunInstallCommand
        }
    }
    internal static bool IsAdmin()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            try
            {
                using var identity = WindowsIdentity.GetCurrent();
                var principal = new WindowsPrincipal(identity);
                return principal.IsInRole(WindowsBuiltInRole.Administrator);
            }
            catch
            {
                return false; // Error occurred, assume not admin
            }
        }
        else // Unix-like systems (Linux, macOS)
        {
            return GetCurrentUid() == 0;
        }
    }

    internal static int GetCurrentUid()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return -1; // UID concept is not directly applicable in the same way.
        }

        var uidStr = Environment.GetEnvironmentVariable("UID");
        if (!string.IsNullOrEmpty(uidStr) && int.TryParse(uidStr, out var uidEnv))
        {
            return uidEnv;
        }

        try
        {
            var processStartInfo = new ProcessStartInfo
            {
                FileName = "id",
                Arguments = "-u",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(processStartInfo);
            if (process == null) return 1000; // Default to non-root if process fails to start

            process.WaitForExit(5000); // Wait for 5 seconds
            if (!process.HasExited)
            {
                process.Kill(); // Kill if it times out
                Console.Error.WriteLine("Warning: 'id -u' command timed out.");
                return 1000;
            }

            if (process.ExitCode != 0) return 1000; // Default to non-root if command fails

            using var reader = process.StandardOutput;
            var output = reader.ReadToEnd();
            return int.TryParse(output.Trim(), out var uidCmd) ? uidCmd : 1000; // Default if parsing fails
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Warning: Could not get UID using 'id -u': {ex.Message}");
            return 1000; // Default in case of any exception
        }
    }
}
