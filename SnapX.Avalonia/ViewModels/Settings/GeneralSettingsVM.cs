using CommunityToolkit.Mvvm.ComponentModel;
using SnapX.Core;

namespace SnapX.Avalonia.ViewModels.Settings;

public partial class GeneralSettingsVM : ViewModelBase
{
    private readonly ApplicationConfig _config;

    [ObservableProperty]
    private bool _rememberMainWindowPosition;

    [ObservableProperty]
    private bool _disableTelemetry;
    public EImageFormat[] SupportedFormats { get; } = Enum.GetValues<EImageFormat>();
    [ObservableProperty]
    private EImageFormat _selectedFormat;

    public GeneralSettingsVM()
    {
        _config = SnapXL.Settings;

        _rememberMainWindowPosition = _config.RememberMainFormPosition;
        _disableTelemetry = _config.DisableTelemetry;
        _selectedFormat = _config.DefaultTaskSettings.ImageSettings.ImageFormat;
    }
    partial void OnSelectedFormatChanged(EImageFormat value)
    {
        _config.DefaultTaskSettings.ImageSettings.ImageFormat = value;
    }

    partial void OnRememberMainWindowPositionChanged(bool value)
    {
        _config.RememberMainFormPosition = value;
    }

    partial void OnDisableTelemetryChanged(bool value)
    {
        _config.DisableTelemetry = value;
    }
}
