namespace SnapX.Core.Utils.Parsers;

public class CodeMenuEntryActions(string Value, string Description) : CodeMenuEntry(Value, Description)
{
    protected override string Prefix { get; } = "$";

    public static readonly CodeMenuEntryActions input = new("input", "File path");
    public static readonly CodeMenuEntryActions output = new("output", "File path with output file name extension");

    public static string Parse(string pattern, string inputPath, string outputPath)
    {
        var result = pattern;

        if (inputPath != null)
        {
            result = result.Replace(input.ToPrefixString("%"), '"' + inputPath + '"');
            result = result.Replace(input.ToPrefixString(), inputPath);
        }

        if (outputPath != null)
        {
            result = result.Replace(output.ToPrefixString("%"), '"' + outputPath + '"');
            result = result.Replace(output.ToPrefixString(), outputPath);
        }

        return result;
    }
}

