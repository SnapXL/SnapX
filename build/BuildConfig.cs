using System.Reflection;
using System.Runtime.InteropServices;
using System.Xml.Linq;
using Bullseye;

namespace DefaultNamespace;

public class BuildConfig
{
    public string DestDir
    {
        get => field ?? "";
        set;
    }

    public string Prefix
    {
        get => field ?? Path.Join(Path.DirectorySeparatorChar.ToString(), "usr", "local");
        set;
    }

    public string BinDir
    {
        get => field ?? Path.Join(DestDir, Prefix, "bin");
        set;
    }
    public string Uname
    {
        get
        {
            if (OperatingSystem.IsWindows()) return "Windows";
            if (OperatingSystem.IsLinux()) return "Linux";
            if (OperatingSystem.IsMacOS()) return "macOS";
            if (OperatingSystem.IsFreeBSD()) return "FreeBSD";

            return Environment.OSVersion.Platform.ToString(); // Fallback
        }
    }

    public string Edition
    {
        get
        {
            var edition = TargetInstallAssembly;
            if (!string.IsNullOrEmpty(edition))
            {
                var dashIndex = edition.IndexOf('-');
                if (dashIndex >= 0 && dashIndex < edition.Length - 1)
                {
                    edition = edition[(dashIndex + 1)..].ToUpperInvariant();
                }
            }
            if (edition == "snapx") edition = "CLI";
            return edition;
        }
    }

    public string Datadir => Path.Join(DestDir, Prefix, "share");

    public string Docdir
    {
        get => field ??= Path.Join(Datadir, "doc", "snapx");
        set;
    }

    public string Licensedir
    {
        get => field ?? Path.Join(Datadir, "licenses", "snapx");
        set;
    }
    public string Applicationsdir
    {
        get => field ?? Path.Join(Datadir, "applications");
        set;
    }
    public string Icondir
    {
        get => field ?? Path.Join(Datadir, "icons", "hicolor");
        set;
    }
    public string Runtime { get; set; } = RuntimeInformation.RuntimeIdentifier;
    public string Metainfodir
    {
        get => field ?? Path.Join(Datadir, "metainfo");
        set;
    }
    public string RootDirectory { get; } = Path.GetRelativePath(Directory.GetCurrentDirectory(), DirectoryService.FindRoot());
    public string PackagingDirectory
    {
        get => field ?? Path.Combine(RootDirectory, "packaging");
        set;
    }
    public string Tarballdir
    {
        get => field ?? Path.Combine(PackagingDirectory, "tarball");
        set;
    }
    public string Appdir => Path.Combine(PackagingDirectory, "AppDir");
    public string Rundir
    {
        get => field ?? Path.Combine(PackagingDirectory, "run");
        set;
    }

    public string PackagingUsrDir => Path.Combine(PackagingDirectory, "usr");

    public string? TargetInstallAssembly { get; set; }

    public string LibDir
    {
        get => field ?? Path.Join(DestDir, Prefix, "lib", "snapx");
        set;
    }

    public string? NMHostPath => !OperatingSystem.IsWindows() ? Path.Join(LibDir, NMHassemblyName) : null;

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

    public required Options BullseyeOptions { get; init; }

    public string[] Targets { get; init; } = [];
    public string[] SkippedStepsRaw { get; set; } = [];
    public string OutputDir { get; init; } = "Output";
    public string Configuration { get; init; } = "Release";
    public bool EnableWrapperScriptFallback { get; init; }
    public string? CustomWrapperScript { get; set; }
    public bool DisableWrapperScript { get; set; } = OperatingSystem.IsWindows();
    public string ExtraArgs { get; init; } = "";
    private const string Namespace = "SnapX.";
    public readonly string SnapXVersion = Assembly
        .GetExecutingAssembly()
        .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
        .InformationalVersion ?? "0.0.0";
    // SnapX.NativeMessagingHost must be compiled *last*
    private readonly string[] ProjectNames = ["Avalonia", "CLI", "NativeMessagingHost"];
    public string[] projectsToBuild => ProjectNames
        .Where(projectName => OperatingSystem.IsLinux() || projectName != "GTK4")
        .Select(projectName => Path.Combine(
            Path.GetRelativePath(Directory.GetCurrentDirectory(), RootDirectory),
            Namespace + projectName))
        .ToArray();
    public string[] ProjectsToBuild => projectsToBuild;

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
        return assemblyNameElement?.Value.Trim() ?? csprojFileName.Replace('.', '_');
    }
}
