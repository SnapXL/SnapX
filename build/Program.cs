
using System.Runtime.InteropServices;
using System.Xml.Linq;
using static Bullseye.Targets;
using static SimpleExec.Command;

const string outputDir = "Output";
var SnapXVersion = "???";
var hasLoggedInfo = false;
var hasCleanedOutputDir = false;
Target("format", () => RunAsync("dotnet", "format --verify-no-changes"));

Target("build",
    forEach: ["./SnapX.Avalonia", "./SnapX.CLI", "./SnapX.NativeMessagingHost"],
    async(project) =>
    {

    if (!hasLoggedInfo)
    {
        Console.WriteLine($"Operating System: {RuntimeInformation.OSDescription}");
        Console.WriteLine($"SnapX Version: {SnapXVersion}");
        Console.WriteLine($"Architecture: {RuntimeInformation.OSArchitecture} {RuntimeInformation.RuntimeIdentifier}");
        hasLoggedInfo = true;
    }

    try
    {
        if (!hasCleanedOutputDir)
        {
            Directory.Delete(outputDir, true);
            hasCleanedOutputDir = true;
        }
    }
    catch (Exception e)
    {
        //
    }
    var csprojFiles = Directory.GetFiles(project, "*.csproj");
    if (csprojFiles.Length != 1)
    {
        throw new Exception($"ERROR: Expected exactly one .csproj in '{project}' but found {csprojFiles.Length}.");
    }

    var csprojPath = csprojFiles[0];

    var xml = XDocument.Load(csprojPath);

    var assemblyNameElement = xml
        .Descendants("PropertyGroup")
        .Elements("AssemblyName")
        .FirstOrDefault();

    var projectName = new DirectoryInfo(project).Name.Replace('.', '_');
    var assemblyName = assemblyNameElement?.Value?.Trim()
                       ?? projectName;

    await RunAsync("dotnet", $"publish {project} --configuration Release --nologo -o {outputDir}/{assemblyName} -r {RuntimeInformation.RuntimeIdentifier} -m");

});

Target(
    "test",
    dependsOn: ["build"],
    forEach: ["./FooTests.Acceptance", "./FooTests.Performance"],
    project => RunAsync($"dotnet", $"test {project} --configuration Release --no-build --nologo --verbosity quiet"));

Target("default", dependsOn: ["build"]);


await RunTargetsAndExitAsync(args, ex => ex is SimpleExec.ExitCodeException);
