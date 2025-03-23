using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Avalonia.Styling;
using FluentAvalonia.Styling;
using FluentAvalonia.UI.Media;
using FluentAvalonia.UI.Windowing;
using SnapX.Core;
using SnapX.Core.Utils;

namespace SnapX.Avalonia;

public partial class AboutWindow : AppWindow
{
    // Internal instance of the base class (SnapX.CommonUI.AboutDialog)
    private readonly SnapX.CommonUI.AboutDialog _commonAboutDialog;

    // ViewModel properties to bind to the AXAML file
    public string DialogTitle => Lang.AboutSnapX;  // Renamed to avoid conflict
    public string Description { get; set; }
    public string BuildInformation { get; set; }
    public string Version { get; set; }
    public string Copyright { get; set; }
    public string License { get; set; }
    public string Website { get; set; }
    public string SystemInfo { get; set; }
    public string OsArchitecture { get; set; }
    public string Runtime { get; set; }
    public string OsPlatform { get; set; }
    public string Documentation { get; set; }
    public string Issues { get; set; }
    public string Discord { get; set; }

    public string LoadedAssemblies { get; set; }

    public string SystemInformationText =>
        $"{SystemInfo} ({OsArchitecture}, {OsPlatform}) powered by {Runtime}!";

    public AboutWindow()
    {
        _commonAboutDialog = new CommonUI.AboutDialog();
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
            .OrderBy(name => name));
        Description = _commonAboutDialog.GetDescription();
        Version = _commonAboutDialog.GetVersion();
        Copyright = _commonAboutDialog.GetCopyright();
        License = _commonAboutDialog.GetLicenseURL();
        Documentation = _commonAboutDialog.GetDocumentation();
        Issues = _commonAboutDialog.GetIssues();
        Discord = _commonAboutDialog.GetDiscord();
        Website = _commonAboutDialog.GetWebsite();
        SystemInfo = _commonAboutDialog.GetSystemInfo();
        OsArchitecture = _commonAboutDialog.GetOsArchitecture();
        Runtime = _commonAboutDialog.GetRuntime();
        OsPlatform = _commonAboutDialog.GetOsPlatform();
        BuildInformation = _commonAboutDialog.GetBuildInformation();
        DataContext = this;
        InitializeComponent();
    }

    private void FindURLOnDescendant(ILogical control)
    {
        foreach (var child in control.GetLogicalChildren())
        {
            var toolTip = child.FindLogicalDescendantOfType<ToolTip>(true);
            if (toolTip is null)
            {
                FindURLOnDescendant(child);
            }
            var url = toolTip?.Content as string ?? string.Empty;
            if (!string.IsNullOrEmpty(url)) URLHelpers.OpenURL(url);
        }
    }
    private void DynamicURL_OnPointerPressed(object? Sender, RoutedEventArgs E)
    {
        DebugHelper.WriteLine($"{nameof(DynamicURL_OnPointerPressed)}: {Sender} {E.Source}");
        if (Sender is Control control)
        {
            // The ToolTip class has a storage of loaded tooltips, however, when a user clicks without hovering for a second the button didn't work.
            // So I added the second if-clause.
            if (ToolTip.GetTip(control) is string url)
            {
                URLHelpers.OpenURL(url);
                return;
            }

            FindURLOnDescendant(control);
        }
        else
        {
            DebugHelper.WriteLine($"{nameof(DynamicURL_OnPointerPressed)} called with {Sender} which is not a Control!!");
        }
    }
    private void ApplicationActualThemeVariantChanged(object? sender, EventArgs e)
    {
        if (!OperatingSystem.IsWindows()) return;
        if (IsWindows11 && ActualThemeVariant != FluentAvaloniaTheme.HighContrastTheme)
        {
            TryEnableMicaEffect();
        }
        else if (ActualThemeVariant != FluentAvaloniaTheme.HighContrastTheme)
        {
            SetValue(BackgroundProperty, AvaloniaProperty.UnsetValue);
        }
    }
    protected override void OnOpened(EventArgs e)
    {
        base.OnOpened(e);

        Application.Current!.ActualThemeVariantChanged += ApplicationActualThemeVariantChanged;
        var thm = ActualThemeVariant;
        if (!IsWindows11 || thm == FluentAvaloniaTheme.HighContrastTheme) return;
        TransparencyBackgroundFallback = Brushes.Transparent;
        TransparencyLevelHint = new[]
            { WindowTransparencyLevel.Mica, WindowTransparencyLevel.AcrylicBlur, WindowTransparencyLevel.None };

        TryEnableMicaEffect();
    }
    private void TryEnableMicaEffect()
    {
        if (ActualThemeVariant == ThemeVariant.Dark)
        {
            var color = this.TryFindResource("SolidBackgroundFillColorBase",
                ThemeVariant.Dark, out var value)
                ? (Color2)(Color)value!
                : new Color2(32, 32, 32);

            color = color.LightenPercent(-0.8f);

            Background = new ImmutableSolidColorBrush(color, 0.78);
        }
        else if (ActualThemeVariant == ThemeVariant.Light)
        {
            // Similar effect here
            var color = this.TryFindResource("SolidBackgroundFillColorBase",
                ThemeVariant.Light, out var value)
                ? (Color2)(Color)value!
                : new Color2(243, 243, 243);

            color = color.LightenPercent(0.5f);

            Background = new ImmutableSolidColorBrush(color, 0.9);
        }
    }
}

