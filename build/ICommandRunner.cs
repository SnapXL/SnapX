namespace DefaultNamespace;

public interface ICommandRunner
{
    Task RunAsync(string command, string args);

    // New method for file installation logic
    Task InstallFile(string source, string destination, string permissions);
    Task RunInstallCommand(string installArguments, string executionCommand = "install");
}
