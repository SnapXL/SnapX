using Avalonia.Controls;
using SnapX.Avalonia.ViewModels.Settings;

namespace SnapX.Avalonia.Views.Settings;

public partial class ApplicationPathSettingsView : UserControl
{
    internal ApplicationPathSettingsVM ViewModel;
    public ApplicationPathSettingsView()
    {
        ViewModel = new ApplicationPathSettingsVM();
        var topLevel = TopLevel.GetTopLevel(this);
        ViewModel.SetStorageService(new StorageService(topLevel));
        InitializeComponent();
    }
}

