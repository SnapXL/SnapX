using System.Diagnostics.CodeAnalysis;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SnapX.Core.Utils;
using SnapX.Core.Utils.Miscellaneous;

namespace SnapX.CommonUI.ViewModels;

public partial class AboutWindowViewModel : ViewModelBase
{
    // Internal instance of the base class (SnapX.CommonUI.AboutDialog)
    private readonly AboutDialog _commonAboutDialog = new();

    [UnconditionalSuppressMessage("Trimming",
         "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code",
         Justification = "<Pending>"),
     RelayCommand]
    private Task InitDataAsync()
    {
        var combinedLoadedAssemblies = AppDomain.CurrentDomain.GetAssemblies();
        LoadedAssemblies = string.Join(Environment.NewLine, combinedLoadedAssemblies
            .Where(a => a.GetName().Name != null)
            .Where(a =>
#pragma warning disable CS8602 // Dereference of a possibly null reference.
                !a.GetName().Name.StartsWith("System") &&
                !a.GetName().Name.StartsWith("SnapX", StringComparison.OrdinalIgnoreCase) &&
                !a.GetName().Name.StartsWith("Anonymous", StringComparison.OrdinalIgnoreCase) &&
                !a.GetName().Name.StartsWith("Microsoft", StringComparison.OrdinalIgnoreCase) &&
                // A dependency on Avalonia is self-explanatory.
                !a.GetName().Name.StartsWith("SkiaSharp", StringComparison.OrdinalIgnoreCase) &&
                !a.GetName().Name.StartsWith("Harfbuzz", StringComparison.OrdinalIgnoreCase) &&
                !a.GetName().Name.Contains("mscorlib", StringComparison.OrdinalIgnoreCase) &&
                !a.GetName().Name.Contains("Mono", StringComparison.OrdinalIgnoreCase) &&
                !a.GetName().Name.StartsWith("Microcom", StringComparison.OrdinalIgnoreCase) &&
                !a.GetName().Name.Contains("netstandard", StringComparison.OrdinalIgnoreCase))
            .Select(a => new { a.GetName().Name, a.GetName().Version })
            .GroupBy(a => a.Name.Split('.')[0])
            .Select(g => g.Count() > 1
                ? $"{g.Key} {g.First().Version.Major}.{g.First().Version.Minor}.{g.First().Version.Build}"
                : $"{g.First().Name} {g.First().Version.Major}.{g.First().Version.Minor}.{g.First().Version.Build}")
            .Append($"SQLite {Core.SnapX.DbConnection?.ServerVersion}")
            .OrderBy(name => name));
#pragma warning restore CS8602 // Dereference of a possibly null reference.

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
        SystemInformationText = $"{SystemInfo} ({OsArchitecture}, {OsPlatform}) powered by {Runtime}!";
        return Task.CompletedTask;
    }
    [ObservableProperty] private string dialogTitle = Lang.AboutSnapX;
    [ObservableProperty] private string? description;
    [ObservableProperty] private string? buildInformation;
    [ObservableProperty] private string? version;
    [ObservableProperty] private string? copyright;
    [ObservableProperty] private string? license;
    [ObservableProperty] private string? website;
    [ObservableProperty] private string? systemInfo;
    [ObservableProperty] private string? osArchitecture;
    [ObservableProperty] private string? runtime;
    [ObservableProperty] private string? osPlatform;
    [ObservableProperty] private string? documentation;
    [ObservableProperty] private string? issues;
    [ObservableProperty] private string? discord;
    [ObservableProperty] private string? donate;
    [ObservableProperty] private string? loadedAssemblies;
    [ObservableProperty] private string? systemInformationText;
}
