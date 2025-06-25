using FluentAvalonia.UI.Windowing;
using SnapX.Avalonia.ViewModels;

namespace SnapX.Avalonia.Views.Settings;

public partial class SettingsWindow : AppWindow
{
    public SettingsWindow(SettingsMainViewVM vm)
    {
        DataContext = vm;
        InitializeComponent();
    }

    public SettingsWindow() : this(new SettingsMainViewVM())
    {
    }

    private void SettingsWindowInit(object? Sender, EventArgs E)
    {
        var activeScreen = Screens.ScreenFromWindow(this);
        var screenWidth = activeScreen?.Bounds.Width ?? 1920;
        var screenHeight = activeScreen?.Bounds.Height ?? 1080;
        Width = screenWidth * 0.6;
        Height = screenHeight * 0.55;
    }
}

