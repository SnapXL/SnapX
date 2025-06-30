namespace DefaultNamespace;

public class Tarball(IBuildLogger Logger, ICommandRunner CommandRunner, FS FileSystem, BuildConfig config)
{
    public async Task ProcessTarball()
    {
        Logger.Information("Tarball started");
        if (!config.ShouldSkip("install"))
        {
            var destDir = config.Tarballdir;
            var installConfig = config;
            installConfig.DestDir = destDir;
            installConfig.Prefix = $"{Path.DirectorySeparatorChar}usr";
            installConfig.DisableWrapperScript = true;
            // installConfig. = destDir;
            installConfig.LibDir = installConfig.BinDir;
            var installProcessor = new Install(Logger, CommandRunner, FileSystem, config);
            await installProcessor.ProcessInstall();
        }
    }
}
