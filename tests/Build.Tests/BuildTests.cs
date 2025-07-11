namespace DefaultNamespace;

public class BuildTests
{
    private static readonly IBuildLogger logger = new ConsoleLogger();
    private static readonly ICommandRunner commandRunner = new CommandRunner(logger);
    private static readonly FS fileSystem = new(logger, commandRunner);

    [Test]
    public async Task Builds()
    {
        var cli = new CLI();
        BuildConfig? config = null;
        await cli.InvokeAsync([], buildconfig =>
        {
            config = buildconfig;
            return Task.CompletedTask;
        });
        if (config is null) throw new NullReferenceException(nameof(config));
        var buildProcessor = new Build(logger, commandRunner, fileSystem, config);

        await buildProcessor.ProcessBuildProject(config.ProjectsToBuild[0]);
    }

    [Test, DependsOn(nameof(Builds))]
    public async Task Installs()
    {
        //         var snapx = new SnapX.Core.SnapX();
        // #if RELEASE
        // snapx.silenceLogging();
        // #elif DEBUG
        // #else
        // snapx.silenceLogging();
        // #endif
        //         snapx.start([]);
        //
        //         var CLIManager = snapx.GetCLIManager();
        //
        //         Task.Run(() => CLIManager.UseCommandLineArgs().GetAwaiter().GetResult()).ConfigureAwait(false).GetAwaiter().GetResult();
        var cli = new CLI();
        BuildConfig? config = null;
        await cli.InvokeAsync([], buildconfig =>
        {
            config = buildconfig;
            return Task.CompletedTask;
        });
        if (config is null) throw new NullReferenceException(nameof(config));
        config.Prefix = Path.Join(Path.DirectorySeparatorChar.ToString(), "usr");
        config.DestDir = Path.Join(AppContext.BaseDirectory, nameof(BuildTests));

        var installer = new Install(logger, commandRunner, fileSystem, config);

        await installer.ProcessInstall();
    }
    [After(Test)]
    public void Cleanup()
    {
        var testDir = Path.Join(AppContext.BaseDirectory, nameof(BuildTests));
        if (Directory.Exists(testDir))
        {
            Directory.Delete(testDir, recursive: true);
        }
    }

}
