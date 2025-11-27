using System.CommandLine;
using Bullseye;

namespace DefaultNamespace;

public class CLI
{
    public async Task<int> InvokeAsync(string[] args, Func<BuildConfig, Task> handler)
    {
        var rootCommand = new RootCommand("Build and configure the application.");

        var targetsArgument = new Argument<string[]>("targets")
        {
            Description = "A list of targets to run or list. If not specified, the \"default\" target will be run, or all targets will be listed.",
            Arity = ArgumentArity.ZeroOrMore
        };
        rootCommand.Arguments.Add(targetsArgument);

        var clearOption = new Option<bool>("--clear", "-c")
        {
            Description = "Clear the console before execution."
        };
        rootCommand.Options.Add(clearOption);

        var dryRunOption = new Option<bool>("--dry-run", "-n")
        {
            Description = "Do a dry run without executing actions. (Passed to Bullseye)"
        };
        rootCommand.Options.Add(dryRunOption);

        var hostOption = new Option<Host?>("--host")
        {
            Description = "Force the mode for a specific host environment (normally auto-detected). Valid values: AppVeyor, AzurePipelines, GitHubActions, GitLabCI, TeamCity, Travis, etc."
        };
        rootCommand.Options.Add(hostOption);

        var listDependenciesOption = new Option<bool>("--list-dependencies")
        {
            Description = "List all (or specified) targets and dependencies, then exit."
        };
        rootCommand.Options.Add(listDependenciesOption);

        var listInputsOption = new Option<bool>("--list-inputs")
        {
            Description = "List all (or specified) targets and inputs, then exit."
        };
        rootCommand.Options.Add(listInputsOption);

        var listTargetsOption = new Option<bool>("--list-targets", "-l", "-t")
        {
            Description = "List all (or specified) targets, then exit."
        };
        rootCommand.Options.Add(listTargetsOption);

        var listTreeOption = new Option<bool>("--list-tree")
        {
            Description = "List all (or specified) targets and dependency trees, then exit."
        };
        rootCommand.Options.Add(listTreeOption);

        var noColorOption = new Option<bool>("--no-color")
        {
            Description = "Disable colored output."
        };
        rootCommand.Options.Add(noColorOption);

        var noExtendedCharsOption = new Option<bool>("--no-extended-chars")
        {
            Description = "Disable extended characters (for PagerDuty or other hosts which don't support them)."
        };
        rootCommand.Options.Add(noExtendedCharsOption);

        var parallelOption = new Option<bool>("--parallel", "-p")
        {
            Description = "Run targets in parallel."
        };
        rootCommand.Options.Add(parallelOption);

        var skipDependenciesOption = new Option<bool>("--skip-dependencies", "-s")
        {
            Description = "Do not run targets' dependencies."
        };
        rootCommand.Options.Add(skipDependenciesOption);

        var verboseOption = new Option<bool>("--verbose", "-v")
        {
            Description = "Enable verbose output."
        };
        rootCommand.Options.Add(verboseOption);

        var skipStepOption = new Option<string[]>("--skip")
        {
            Description = "Comma-separated list of steps to skip (e.g., build,install,compile).",
            Arity = ArgumentArity.ZeroOrMore,
            AllowMultipleArgumentsPerToken = true
        };
        rootCommand.Options.Add(skipStepOption);

        var outputDirOption = new Option<string>("--output-dir", "-o")
        {
            Description = "The directory to output builds artifacts to.",
            Arity = ArgumentArity.ExactlyOne,
            DefaultValueFactory = _ => "Output"
        };
        rootCommand.Options.Add(outputDirOption);

        var configurationOption = new Option<string>("--configuration", "--config")
        {
            Description = "Build configuration (e.g., Debug or Release).",
            Arity = ArgumentArity.ExactlyOne,
            DefaultValueFactory = _ => "Release"
        };
        rootCommand.Options.Add(configurationOption);

        var extraArgsOption = new Option<string>("--extra-args")
        {
            Description = "Extra arguments to pass to the dotnet publish command.",
            Arity = ArgumentArity.ExactlyOne,
            DefaultValueFactory = _ => ""
        };
        rootCommand.Options.Add(extraArgsOption);

        var destDirOption = new Option<string>("--dest-dir")
        {
            Description = "Destination directory for installation output.",
            Arity = ArgumentArity.ExactlyOne
        };
        rootCommand.Options.Add(destDirOption);

        var prefixOption = new Option<string>("--prefix")
        {
            Description = "Installation prefix path (e.g., /usr or /usr/local).",
            Arity = ArgumentArity.ZeroOrOne
        };
        rootCommand.Options.Add(prefixOption);

        var libDirOption = new Option<string>("--lib-dir")
        {
            Description = "Library directory relative to the prefix (e.g., lib or lib64).",
            Arity = ArgumentArity.ExactlyOne
        };
        rootCommand.Options.Add(libDirOption);

        var docDirOption = new Option<string>("--doc-dir")
        {
            Description = "spit out the readme.md somewhere",
            Arity = ArgumentArity.ExactlyOne
        };
        rootCommand.Options.Add(docDirOption);
        var targetAssemblyOption = new Option<string>("--assembly")
        {
            Description = "Tell the install system to only install files related to an specific assembly (snapx-ui, snapx)",
            Arity = ArgumentArity.ExactlyOne
        };
        rootCommand.Options.Add(targetAssemblyOption);
        var enableWrapperFallbackOption = new Option<bool>("--enable-wrapper-fallback")
        {
            Description = "Enable the fallback to the packaging DESTDIR path in the wrapper script.",
            Arity = ArgumentArity.ZeroOrOne,
            DefaultValueFactory = _ => false
        };
        rootCommand.Options.Add(enableWrapperFallbackOption);

        rootCommand.SetAction(async (parseResult, token) =>
        {
            Console.BackgroundColor = ConsoleColor.Black;
            Console.ForegroundColor = ConsoleColor.White;

            var config = new BuildConfig
            {
                Targets = parseResult.GetValue(targetsArgument) ?? [],
                OutputDir = parseResult.GetValue(outputDirOption) ?? "Output",
                Configuration = parseResult.GetValue(configurationOption) ?? "Release",
                ExtraArgs = parseResult.GetValue(extraArgsOption) ?? "",
                SkippedStepsRaw = parseResult.GetValue(skipStepOption) ?? [],
                EnableWrapperScriptFallback = parseResult.GetValue(enableWrapperFallbackOption),
                TargetInstallAssembly = parseResult.GetValue(targetAssemblyOption),
                BullseyeOptions = new Options
                {
                    Clear = parseResult.GetValue(clearOption),
                    DryRun = parseResult.GetValue(dryRunOption),
                    Host = parseResult.GetValue(hostOption),
                    ListDependencies = parseResult.GetValue(listDependenciesOption),
                    ListInputs = parseResult.GetValue(listInputsOption),
                    ListTargets = parseResult.GetValue(listTargetsOption),
                    ListTree = parseResult.GetValue(listTreeOption),
                    NoColor = parseResult.GetValue(noColorOption),
                    NoExtendedChars = parseResult.GetValue(noExtendedCharsOption),
                    Parallel = parseResult.GetValue(parallelOption),
                    SkipDependencies = parseResult.GetValue(skipDependenciesOption),
                    Verbose = parseResult.GetValue(verboseOption),
                }
            };

            // Apply overrides for installation directories
            var destDir = parseResult.GetValue(destDirOption);
            var prefix = parseResult.GetValue(prefixOption);
            var libDir = parseResult.GetValue(libDirOption);
            var docDir = parseResult.GetValue(docDirOption);

            if (destDir is not null) config.DestDir = destDir;
            if (prefix is not null) config.Prefix = prefix;
            if (libDir is not null) config.LibDir = libDir;
            if (docDir is not null) config.Docdir = docDir;

            config.SetSkippedSteps(config.SkippedStepsRaw);

            await handler(config);
        });
        return await rootCommand.Parse(args).InvokeAsync();
    }
}
