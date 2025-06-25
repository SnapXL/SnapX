using Avalonia.Controls;
using SnapX.Avalonia.ViewModels;

namespace SnapX.Avalonia.Views.Settings.Views;

public partial class SettingsHomePageView : UserControl
{
    private SettingsHomePageViewVM ViewModel;

    public SettingsHomePageView(SettingsHomePageViewVM vm)
    {
        DataContext = vm;
        ViewModel = vm;
        InitializeComponent();
    }
    public SettingsHomePageView() : this(new SettingsHomePageViewVM())
    {
        InitializeComponent();
    }
}

