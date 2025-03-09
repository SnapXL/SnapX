using Avalonia.Controls;
using Avalonia.Input;
using FluentAvalonia.UI.Windowing;
using SnapX.Core.Utils;

namespace SnapX.Avalonia;

public partial class AboutWindow : AppWindow
{
    // Internal instance of the base class (SnapX.CommonUI.AboutDialog)
    private readonly SnapX.CommonUI.AboutDialog _commonAboutDialog;

    // ViewModel properties to bind to the AXAML file
    public string DialogTitle => Lang.AboutSnapX;  // Renamed to avoid conflict
    public string Description { get; set; }
    public string Version { get; set; }
    public string Copyright { get; set; }
    public string License { get; set; }
    public string Website { get; set; }
    public string SystemInfo { get; set; }
    public string OsArchitecture { get; set; }
    public string Runtime { get; set; }
    public string OsPlatform { get; set; }

    public string CombinedText =>
        $"{Description}\n{Version}\n{Copyright}\n{License}\n{SystemInfo}\n{OsArchitecture}\n{Runtime}\n{OsPlatform}";

    public AboutWindow()
    {
        _commonAboutDialog = new CommonUI.AboutDialog();

        Description = _commonAboutDialog.GetDescription();
        Version = _commonAboutDialog.GetVersion();
        Copyright = _commonAboutDialog.GetCopyright();
        License = _commonAboutDialog.GetLicense();
        Website = _commonAboutDialog.GetWebsite();
        SystemInfo = _commonAboutDialog.GetSystemInfo();
        OsArchitecture = _commonAboutDialog.GetOsArchitecture();
        Runtime = _commonAboutDialog.GetRuntime();
        OsPlatform = _commonAboutDialog.GetOsPlatform();
        DataContext = this;
        InitializeComponent();
    }

    private void WebsiteURL_OnPointerPressed(object? Sender, PointerPressedEventArgs E)
    {
        URLHelpers.OpenURL(Website);
    }

    private void WebsiteURL_OnPointerReleased(object? Sender, PointerReleasedEventArgs E)
    {
        URLHelpers.OpenURL(Website);
    }
}

