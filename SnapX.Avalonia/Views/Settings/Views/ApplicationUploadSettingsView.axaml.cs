using Avalonia.Controls;
using SnapX.Avalonia.ViewModels.Settings;

namespace SnapX.Avalonia.Views.Settings.Views;

public partial class ApplicationUploadSettingsView : UserControl
{
    internal ApplicationUploadSettingsVM ViewModel;
    public ApplicationUploadSettingsView()
    {
        ViewModel = new ApplicationUploadSettingsVM();
        InitializeComponent();
    }
}

