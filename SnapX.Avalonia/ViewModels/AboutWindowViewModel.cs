using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SnapX.CommonUI;
using SnapX.Core.Utils;
using SnapX.Core.Utils.Miscellaneous;

namespace SnapX.Avalonia.ViewModels;

public partial class AboutWindowViewModel : ViewModelBase
{
    // Internal instance of the base class (SnapX.CommonUI.AboutDialog)
    private AboutDialog _commonAboutDialog = new();

    public static string ToFriendlyString(Version v)
    {
        // Start with Major.Minor (always present)
        string result = $"{v.Major}.{v.Minor}.{v.Build}";
        if (v.Revision > 0)
        {
            result += $".{v.Revision}";
        }

        return result;
    }

    public static string? GetUrl(Assembly assembly)
    {
        var attributes = assembly.GetCustomAttributes<AssemblyMetadataAttribute>().ToList();

        var repoUrl = attributes
            .FirstOrDefault(m => m.Key.Equals("RepositoryUrl", StringComparison.OrdinalIgnoreCase))
            ?.Value;

        if (!string.IsNullOrEmpty(repoUrl))
            return repoUrl;

        var projectUrl = attributes
            .FirstOrDefault(m =>
                m.Key.Equals("PackageProjectUrl", StringComparison.OrdinalIgnoreCase)
            )
            ?.Value;

        if (!string.IsNullOrEmpty(projectUrl))
            return projectUrl;

        var projectSite = attributes
            .FirstOrDefault(m => m.Key.Equals("ProjectUrl", StringComparison.OrdinalIgnoreCase))
            ?.Value;

        return projectSite;
    }

    public static readonly string[] excludedPrefixes =
    [
        "System",
        "SnapX",
        "Anonymous",
        "Microsoft",
        "SkiaSharp",
        "Harfbuzz",
        "SQLitePCLRaw",
        "Microcom",
        "Color",
    ];
    public static readonly string[] excludedKeywords = ["mscorlib", "Mono", "netstandard", "Newtonsoft"];
    public static IEnumerable<Assembly> CombinedLoadedAssemblies =>
        AppDomain.CurrentDomain.GetAssemblies().Concat(App.SnapX.GetAssemblies()).Distinct();

    [
        UnconditionalSuppressMessage(
            "Trimming",
            "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code",
            Justification = "<Pending>"
        ),
        RelayCommand
    ]
    private Task InitDataAsync()
    {
        CombinedRawAssemblies = string.Join(
            "  " + Environment.NewLine,
            CombinedLoadedAssemblies.Select(a => $"{a.GetName().Name} {a.GetName().Version}")
        );
        LoadedAssemblies = string.Join(
            "  " + Environment.NewLine,
            CombinedLoadedAssemblies
                .Where(a => a.GetName().Name != null)
                .Where(a =>
                    !excludedPrefixes.Any(p =>
                        a.GetName()!.Name!.StartsWith(p, StringComparison.OrdinalIgnoreCase)
                    )
                )
                .Where(a =>
                    !excludedKeywords.Any(k =>
                        a.GetName()!.Name!.Contains(k, StringComparison.OrdinalIgnoreCase)
                    )
                )
                .Select(a =>
                {
                    var sourceURL = GetUrl(a);
                    return new
                    {
                        a.GetName().Name,
                        a.GetName().Version,
                        sourceURL,
                    };
                })
                .Append(
                    new
                    {
                        Name = "SQLite",
                        Version = new Version(Core.SnapX.DbConnection.ServerVersion),
                        sourceURL = "https://www.nuget.org/packages/Microsoft.Data.Sqlite",
                    }
                )
                .GroupBy(a =>
                {
                    // Markdown.Avalonia needs to be fully expressed
                    if (a.Name.StartsWith("Markdown", StringComparison.InvariantCultureIgnoreCase))
                    {
                        var parts = a.Name.Split('.');

                        if (parts.Length > 2)
                        {
                            return $"{parts[0]}.{parts[1]}";
                        }

                        return a.Name;
                    }
                    return a.Name!.Split('.') switch
                    {
                        var p when p[0] is "Sdcb" or "SixLabors" or "Tmds" && p.Length > 1 => p[1],
                        var p when p[0] is "DotNext" or "CommunityToolkit" && p.Length > 1 =>
                            $"{p[0]}.{p[1]}",
                        var p when p[0] is "NeoSolve" && p.Length > 2 => $"{p[1]}.{p[2]}",
                        var p => p[0],
                    };
                })
                .OrderBy(g => g.Key)
                .Select(g =>
                {
                    var best = g.OrderByDescending(x => x.Version).First();
                    var version = ToFriendlyString(best.Version!);

                    return !string.IsNullOrEmpty(best.sourceURL)
                        ? $"[{g.Key} {version}]({best.sourceURL})"
                        : $"{g.Key} {version}";
                })
        );
        Description = _commonAboutDialog.GetDescription();
        Version = _commonAboutDialog.GetVersion();
        Copyright = _commonAboutDialog.GetCopyright();
        License = _commonAboutDialog.GetLicenseURL();
        Documentation = _commonAboutDialog.GetDocumentation();
        Issues = _commonAboutDialog.GetIssues();
        Discord = _commonAboutDialog.GetDiscord();
        Donate = Links.Donate;
        Website = _commonAboutDialog.GetWebsite();
        SystemInfo = _commonAboutDialog.GetSystemInfo();
        OsArchitecture = _commonAboutDialog.GetOsArchitecture();
        Runtime = _commonAboutDialog.GetRuntime();
        OsPlatform = _commonAboutDialog.GetOsPlatform();
        BuildInformation = _commonAboutDialog.GetBuildInformation();
        SystemInformationText =
            $"{SystemInfo} ({OsArchitecture}, {OsPlatform}) powered by {Runtime}!";
        return Task.CompletedTask;
    }

    [ObservableProperty]
    public string dialogTitle = Lang.AboutSnapX;

    [ObservableProperty]
    private string? description;

    [ObservableProperty]
    public string? buildInformation;

    [ObservableProperty]
    public string? version;

    [ObservableProperty]
    public string? copyright;

    [ObservableProperty]
    public string? license;

    [ObservableProperty]
    public string? website;

    [ObservableProperty]
    public string? systemInfo;

    [ObservableProperty]
    public string? osArchitecture;

    [ObservableProperty]
    public string? runtime;

    [ObservableProperty]
    public string? osPlatform;

    [ObservableProperty]
    public string? documentation;

    [ObservableProperty]
    public string? issues;

    [ObservableProperty]
    public string? discord;

    [ObservableProperty]
    public string? donate;

    [ObservableProperty]
    public string? loadedAssemblies;

    [ObservableProperty]
    public string? _combinedRawAssemblies;

    [ObservableProperty]
    public string? systemInformationText;
}
