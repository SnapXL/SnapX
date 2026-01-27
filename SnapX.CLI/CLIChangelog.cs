namespace SnapX.CLI;

public class CLIChangelog : SnapX.CommonUI.Changelog
{
    public CLIChangelog(string version) : base(version)
    {
        Version = version;
    }

    public override void Display()
    {
        // Display changelog in the CLI
        Console.WriteLine($"Changelog for {Version}:");
        var changes = base.GetChangeSummary().GetAwaiter().GetResult();
        Console.WriteLine(changes);
    }
}
