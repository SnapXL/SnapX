using System.CommandLine;
using System.Runtime.InteropServices;
using System.Xml.Linq;
using static Bullseye.Targets;
using static SimpleExec.Command;
using System.Diagnostics;
using System.Security.Principal;
using Bullseye;

// I am on a limited timeframe for NUKING `NUKE.Build`
// Sorry for the crimes against programming

internal class Program
{
    private static async Task ExecuteBuildAsync(
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

        Target("format", async () => await RunAsync("dotnet", "format --verify-no-changes"));
        Target("clean", () =>
        {
            try
            {
                if (Directory.Exists(outputDir))
                {
                    Directory.Delete(outputDir, true);
                }
                Directory.CreateDirectory(outputDir);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Warning: Could not clean and recreate output directory '{outputDir}'. Error: {ex.Message}");
            }
        });

        Target("build",
            dependsOn: ["clean"],
            forEach: ["./SnapX.Avalonia", "./SnapX.CLI", "./SnapX.NativeMessagingHost"],
            async (project) =>
            {
                if (!hasLoggedInfo)
                {
                    Console.WriteLine($"Operating System: {RuntimeInformation.OSDescription}");
                    Console.WriteLine($"SnapX Version: {snapXVersion}");
                    Console.WriteLine($"Architecture: {RuntimeInformation.OSArchitecture} {RuntimeInformation.RuntimeIdentifier}");
                    hasLoggedInfo = true;
                }

                var csprojFiles = Directory.GetFiles(project, "*.csproj");
                if (csprojFiles.Length != 1)
                {
                    throw new FileNotFoundException($"ERROR: Expected exactly one .csproj in '{project}' but found {csprojFiles.Length}.");
                }

                var csprojPath = csprojFiles[0];
                var xml = XDocument.Load(csprojPath);

                var assemblyNameElement = xml.Descendants("PropertyGroup").Elements("AssemblyName").FirstOrDefault();
                var csprojFileName = Path.GetFileNameWithoutExtension(csprojPath);
                var assemblyName = assemblyNameElement?.Value?.Trim() ?? csprojFileName.Replace('.', '_');

                await RunAsync("dotnet", $"publish \"{project}\" --configuration {configuration} --nologo -o \"{Path.Combine(outputDir, assemblyName)}\" -r {RuntimeInformation.RuntimeIdentifier} {extraArgs}");
            });
        Target("install",
            dependsOn: ["build"],

            async () =>
            {
                // Information($"Destination Directory: {DestDir}");
                // Information($"Prefix: {Prefix}");
                // Information($"Installing to {Path.Join(DestDir, Prefix)}");
                // Information($"Data directory: {Datadir}");
                // Information($"Bin directory: {BinDir}");
                // Information($"Documentation directory: {Docdir}");
                // Information($"License directory: {Licensedir}");
                // Information($"Metainfo directory: {Metainfodir}");
                // Information($"Tarball directory: {Tarballdir}");
                // Information($"Application directory: {Applicationsdir}");
                // Information($"Icon directory: {Icondir}");
                // Information($"Library directory: {LibDir}");
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

        var dryRunOption = new Option<bool>(["--dry-run", "-d"], "Do a dry run without executing actions.");
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
        configurationOption.SetDefaultValue("Debug");
        rootCommand.AddOption(configurationOption);

        var extraArgsOption = new Option<string>(
            name: "--extra-args",
            description: "Extra arguments to pass to the build system.")
        {
            Arity = ArgumentArity.ExactlyOne,
        };
        extraArgsOption.SetDefaultValue("");
        rootCommand.AddOption(extraArgsOption);

     rootCommand.SetHandler(async (invocationContext) =>
        {
            Console.BackgroundColor = ConsoleColor.Black;
            Console.ForegroundColor = ConsoleColor.White;
            var targets = invocationContext.ParseResult.GetValueForArgument(targetsArgument);
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
            var outputDir = invocationContext.ParseResult.GetValueForOption(outputDirOption);
            var configuration = invocationContext.ParseResult.GetValueForOption(configurationOption);
            var extraArgs = invocationContext.ParseResult.GetValueForOption(extraArgsOption);

            await ExecuteBuildAsync(
                targets!,
                clear,
                dryRun,
                host,
                listDependencies,
                listInputs,
                listTargets,
                listTree,
                noColor,
                noExtendedChars,
                parallel,
                skipDependencies,
                verbose,
                outputDir!,
                configuration!,
                extraArgs!
            );
        });


        return await rootCommand.InvokeAsync(args);
    }
    void Error(string message) => Console.Error.WriteLine(message);
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
    // Helper function to detect if elevation (sudo) is LIKELY needed based on arguments
    bool RequiresElevationLikely(string installArguments)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return false;
        }

        // Split arguments and check for paths starting with commonly protected directories
        var arguments = installArguments.Split(new char[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);

        foreach (var argument in arguments)
        {
            var arg = argument.Trim();
            if (arg.StartsWith("/usr") || arg.StartsWith("/opt") || arg.StartsWith("/etc") || arg.StartsWith("/var") || arg.StartsWith("/bin") || arg.StartsWith("/sbin"))
            {
                return true;
            }
        }

        return false;
    }

    void EnsureDirectoryExists(string directory)
    {
        if (Directory.Exists(directory)) return;
        Information($"Creating directory: {directory}");
        RunInstallCommand($"-p {directory}", "mkdir");
    }
    // This is a direct violation of DRY, but, it saves build time.
    public static bool IsAdmin()
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
                return false;
            }
        }

        return GetCurrentUid() == 0;
    }

    private static int GetCurrentUid()
    {
        var uidStr = Environment.GetEnvironmentVariable("UID");
        if (!string.IsNullOrEmpty(uidStr) && int.TryParse(uidStr, out var uid)) return uid;

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
            if (process == null) return 1000;

            process.WaitForExit();
            if (process.ExitCode != 0) return 1000;

            using var reader = process.StandardOutput;
            var output = reader.ReadToEnd();
            if (int.TryParse(output.Trim(), out uid)) return uid;

            return 1000;
        }
        catch (Exception) { return 1000; }
    }

}
