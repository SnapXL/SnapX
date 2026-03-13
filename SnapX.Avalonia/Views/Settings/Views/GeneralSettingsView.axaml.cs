using Avalonia.Controls;
using SnapX.Avalonia.ViewModels.Settings;

namespace SnapX.Avalonia.Views.Settings.Views;

public partial class GeneralSettingsView : UserControl
{
    internal GeneralSettingsVM ViewModel;
    public GeneralSettingsView()
    {
        ViewModel = new GeneralSettingsVM();
        InitializeComponent();
    }
}

