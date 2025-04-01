if (args.Length < 2)
{
    Console.WriteLine("Usage: XamlToGtkBuilder <input directory or pattern> <output directory>");
    return;
}

var inputPattern = args[0];
var outputDir = args[1];

// Ensure output directory exists
Directory.CreateDirectory(outputDir);

// Resolve input files (supporting wildcards)
var inputDir = Path.GetDirectoryName(inputPattern) ?? ".";
var searchPattern = Path.GetFileName(inputPattern);
var xamlFiles = Directory.GetFiles(inputDir, searchPattern).Where(f => f.EndsWith(".xaml"));

if (!xamlFiles.Any())
{
    Console.WriteLine("No XAML files found matching the pattern.");
    return;
}

foreach (var xamlFile in xamlFiles)
{
    var fileNameWithoutExt = Path.GetFileNameWithoutExtension(xamlFile);
    var outputFilePath = Path.Combine(outputDir, fileNameWithoutExt + ".ui");

    try
    {
        var xamlContent = File.ReadAllText(xamlFile);
        var gtkXml = XamlToGtkBuilder.ConvertXamlToGtk(xamlContent);
        File.WriteAllText(outputFilePath, gtkXml);
        Console.WriteLine($"Converted {xamlFile} -> {outputFilePath}");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error converting {xamlFile}: {ex.Message}");
        Environment.Exit(1);
    }
}
