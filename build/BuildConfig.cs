using System.Reflection;
using System.Runtime.InteropServices;
using System.Xml.Linq;
using Bullseye;

namespace DefaultNamespace;

public class BuildConfig
{
    private string _destdir = "";
    public string DestDir
    {
        get => _destdir;
        set => _destdir = value ?? "";
    }

    private string _prefix = Path.Join("/", "usr", "local");
    public string Prefix
    {
        get => _prefix;
        set => _prefix = value ?? Path.Join("usr", "local");
    }

    public string BinDir => Path.Join(DestDir, Prefix, "bin");
    public string Datadir => Path.Join(DestDir, Prefix, "share");

    private string? _docdir;
    public string Docdir
    {
        get => _docdir ??= Path.Join(Datadir, "doc", "snapx");
        set => _docdir = value ?? Path.Join(Datadir, "doc", "snapx");
    }

    public string Licensedir => Path.Join(Datadir, "licenses", "snapx");
    public string Applicationsdir => Path.Join(Datadir, "applications");
    public string Icondir => Path.Join(Datadir, "icons", "hicolor");
    public string Runtime { get; set; } = RuntimeInformation.RuntimeIdentifier;
    public string Metainfodir => Path.Join(Datadir, "metainfo");
    public string RootDirectory { get; } = Path.GetRelativePath(Directory.GetCurrentDirectory(), DirectoryService.FindRoot());
    public string PackagingDirectory => Path.Combine(RootDirectory, "packaging");
    public string Tarballdir => Path.Combine(PackagingDirectory, "tarball");
    public string PackagingUsrDir => Path.Combine(PackagingDirectory, "usr");

    private string? _libdir;
    public string LibDir
    {
        get => _libdir ?? Path.Join(DestDir, Prefix, "lib", "snapx");
        set => _libdir = value;
    }

    public string NMHostPath => !OperatingSystem.IsWindows() ? Path.Join(LibDir, NMHassemblyName) : null;

    private static readonly Dictionary<string, string> StepAliases = new(StringComparer.OrdinalIgnoreCase)
    {
        { "compile", "build" }
    };

    private HashSet<string> _skippedSteps = new(StringComparer.OrdinalIgnoreCase);

    public void SetSkippedSteps(IEnumerable<string>? steps)
    {
        _skippedSteps = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        if (steps is null) return;

        foreach (var step in steps)
        {
            var normalized = StepAliases.GetValueOrDefault(step, step);
            _skippedSteps.Add(normalized);
        }
    }

    public bool ShouldSkip(string stepName)
    {
        var normalized = StepAliases.GetValueOrDefault(stepName, stepName);
        return _skippedSteps.Contains(normalized);
    }

    public required Options BullseyeOptions { get; set; }

    public string[] Targets { get; set; } = [];
    public string[] SkippedStepsRaw { get; set; } = [];
    public string OutputDir { get; set; } = "Output";
    public string Configuration { get; set; } = "Release";
    public bool EnableWrapperScriptFallback { get; set; }
    public string ExtraArgs { get; set; } = "";
    const string Namespace = "SnapX.";
    public readonly string SnapXVersion = Assembly
        .GetExecutingAssembly()
        .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
        .InformationalVersion ?? "0.0.0";
    // SnapX.NativeMessagingHost must be compiled *last*
    public readonly string[] ProjectNames = ["Avalonia", "CLI", "NativeMessagingHost"];
    public string[] projectsToBuild => ProjectNames
        .Where(projectName => OperatingSystem.IsLinux() || projectName != "GTK4")
        .Select(projectName => Path.Combine(
            Path.GetRelativePath(Directory.GetCurrentDirectory(), RootDirectory),
            Namespace + projectName))
        .ToArray();
    public string[] ProjectsToBuild
    {
        get => projectsToBuild;
    }
    public string[] knownAssemblyNames => projectsToBuild
        .Select(GetAssemblyNameFromProject)
        .ToArray();
    public string NMHassemblyName => knownAssemblyNames[^1];

    public static string GetAssemblyNameFromProject(string projectPath)
    {
        var csprojFiles = Directory.GetFiles(projectPath, "*.csproj");
        if (csprojFiles.Length != 1)
        {
            throw new FileNotFoundException(
                $"ERROR: Expected exactly one .csproj in '{projectPath}' but found {csprojFiles.Length}. Searched in: {Path.GetFullPath(projectPath)}");
        }

        var csprojPath = csprojFiles[0];
        var xml = XDocument.Load(csprojPath);

        var assemblyNameElement = xml
            .Descendants("PropertyGroup")
            .Elements("AssemblyName")
            .FirstOrDefault();

        var csprojFileName = Path.GetFileNameWithoutExtension(csprojPath);
        return assemblyNameElement?.Value?.Trim() ?? csprojFileName.Replace('.', '_');
    }
}
