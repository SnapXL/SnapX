using SnapX.CommonUI;

namespace SnapX.CLI;

public class CLIAbout : AboutDialog
{

    public override void Show()
    {
        SnapX.Core.SnapX.Qualifier = " CLI";
        Console.WriteLine($"===============  {GetTitle()}     ================");
        Console.WriteLine($"{GetDescription()}");
        Console.WriteLine($"Version: {GetVersion()}");
        Console.WriteLine($"{GetCopyright()} Licensed under {GetLicense()}");
        Console.WriteLine($"GitHub: {GetWebsite()}");
        Console.WriteLine($"OS: {GetSystemInfo()} ({GetOsArchitecture()})");
        Console.WriteLine($".NET Version: {GetRuntime()}");
        Console.WriteLine($"Platform: {GetOsPlatform()}");
        Console.WriteLine("===================================================");

    }
}
