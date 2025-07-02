using static Bullseye.Targets;
using static SimpleExec.Command;

namespace DefaultNamespace;

// I am on a limited timeframe for NUKING `NUKE.Build`
// Sorry for the crimes against programming

internal class Program
{
    private static readonly ConsoleLogger logger = new();
    private static readonly CommandRunner commandRunner = new(logger);
    internal static readonly DirectoryService directoryService = new(logger, commandRunner);
    private static readonly FS fileSystem = new(logger, commandRunner);

    private async Task ExecuteAsync(BuildConfig config)
    {
        var targetsToRun = config.Targets;
        if (targetsToRun.Length == 0)
        {
            targetsToRun = ["default"];
        }


        Target("format", async () =>
        {
            if (config.ShouldSkip("format")) return;
            await RunAsync("dotnet", "format --verify-no-changes");
        });
        Target("clean", () =>
        {
            if (config.ShouldSkip("clean")) return;
            try
            {
                logger.Information($"Cleaning output directory: {config.OutputDir}");
                if (Directory.Exists(config.OutputDir))
                {
                    Directory.Delete(config.OutputDir, true);
                }
                Directory.CreateDirectory(config.OutputDir);
                logger.Information($"Output directory cleaned and recreated: {config.OutputDir}");
            }
            catch (Exception ex)
            {
                logger.Warning($"Warning: Could not clean and recreate output directory '{config.OutputDir}'. Error: {ex.Message}");
            }
        });

        Target("build",
            dependsOn: config.ShouldSkip("build") ? [] : ["clean"],
            forEach: config.ProjectsToBuild,
            async (project) =>
            {
                var buildProcessor = new Build(logger, commandRunner, fileSystem, config);
                // The ShouldSkip check here is redundant if it's already in dependsOn,
                // but often kept for early exit in forEach loops.
                if (config.ShouldSkip("build")) return;

                // Call the method on your new BuildProcessor instance
                await buildProcessor.ProcessBuildProject(project);
            });

        Target("install",
            dependsOn: ["build"],
            async () =>
            {
                if (config.ShouldSkip("install")) return;

                var installProcessor = new Install(logger, commandRunner, fileSystem, config);
                await installProcessor.ProcessInstall();
            });
        Target("uninstall",
        dependsOn: [],
        async () =>
        {
            if (config.ShouldSkip("uninstall")) return;

            var uninstallProcessor = new Uninstall(logger, fileSystem, config);
            await uninstallProcessor.ProcessUninstall();
        });
        Target("tarball",
            async () =>
            {
                if (config.ShouldSkip("tarball")) return;
                var tarballCreator = new Tarball(logger, commandRunner, fileSystem, config);
                await tarballCreator.ProcessTarball();
            });
        Target("appimage",
            dependsOn: ["tarball"],
            async () =>
        {
            if (config.ShouldSkip("appimage")) return;

            var appImageCreator = new AppImage(logger, commandRunner, fileSystem, config);
            await appImageCreator.ProcessAppImage();
        });

        Target("default", dependsOn: ["build"]);

        await RunTargetsAndExitAsync(targetsToRun, config.BullseyeOptions);
    }
    internal static async Task<int> Main(string[] args)
    {
        var cli = new CLI();
        var program = new Program();
        return await cli.InvokeAsync(args, program.ExecuteAsync);
    }
}
