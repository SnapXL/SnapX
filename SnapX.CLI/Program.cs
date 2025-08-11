using System.Reflection;
using SnapX.CLI;
using SnapX.Core;
using SnapX.Core.Utils;

if (args.Length != 0 && (args[0] == "--version" || args[0] == "-v"))
{
    var informationalVersion = Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "Unknown";
    Console.WriteLine(informationalVersion);
    return;
}

var snapx = new SnapX.Core.SnapX();
#if RELEASE
snapx.silenceLogging();
#elif DEBUG
#else
snapx.silenceLogging();
#endif
snapx.start(args);

var CLIManager = snapx.GetCLIManager();

Task.Run(() => CLIManager.UseCommandLineArgs().GetAwaiter().GetResult()).ConfigureAwait(false).GetAwaiter().GetResult();

var version = Helpers.GetApplicationVersion();
if (args.Length == 0 || args[0] == "--help" || args[0] == "-h")
{
    var changelog = new CLIChangelog(version);
    changelog.Display();
}

var about = new CLIAbout();
about.Show();

Console.WriteLine();
Console.WriteLine("SnapX.CLI is an empty project to dedicated to the developer feedback loop.");
Console.WriteLine("It makes running SnapX's CLI faster than running Avalonia and it's more simple & universal.");
Console.WriteLine("You can use ShareX's documentation found here. https://getsharex.com/docs/command-line-arguments to test SnapX.Core");
var sigintReceived = false;


Console.CancelKeyPress += (_, ea) =>
{
    if (sigintReceived) return;
    ea.Cancel = true;
    sigintReceived = true;
    Console.WriteLine("Received SIGINT (Ctrl+C)");
    snapx.shutdown();
    Environment.Exit(0);
};
AppDomain.CurrentDomain.ProcessExit += (_, _) =>
{
    if (!sigintReceived)
    {
        sigintReceived = true;
        DebugHelper.WriteLine("Received SIGTERM");
        snapx.shutdown();
        Environment.Exit(0);
    }
    else
    {
        DebugHelper.WriteLine("Received SIGTERM, ignoring it because already processed SIGINT");
    }
};
snapx.shutdown();
