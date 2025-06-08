using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SnapX.CommonUI;
using SnapX.Core.Utils;
using SnapX.Core.Utils.Miscellaneous;

namespace SnapX.Avalonia.ViewModels;

public partial class AboutWindowViewModel : ViewModelBase
{
    // Internal instance of the base class (SnapX.CommonUI.AboutDialog)
    private AboutDialog _commonAboutDialog;
    public AboutWindowViewModel()
    {
    }

    [RelayCommand]
    private Task InitDataAsync()
    {
        _commonAboutDialog = new AboutDialog();
        var loadedAssemblies = AppDomain.CurrentDomain.GetAssemblies()
            .Concat(App.SnapX.GetAssemblies())
            .Distinct();
        LoadedAssemblies = string.Join(Environment.NewLine, loadedAssemblies
            .Where(a => a.GetName().Name != null)
            .Where(a =>
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
            .Append($"SQLite {Core.SnapX.DbConnection.ServerVersion}")
            .OrderBy(name => name));
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
    public string? systemInformationText;
}
