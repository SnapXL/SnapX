using Avalonia.Controls;
using Avalonia.Interactivity;
using SnapX.Avalonia.ViewModels;
using SnapX.Core;

namespace SnapX.Avalonia;

public partial class HomePageView : UserControl
{
    private HomePageViewModel ViewModel;

    public HomePageView(HomePageViewModel vm)
    {
        DataContext = vm;
        ViewModel = vm;
        InitializeComponent();
    }
    public HomePageView() : this(new HomePageViewModel())
    {
    }

    private void PopupFlyoutBase_OnOpening(object? Sender, EventArgs E)
    {
        DebugHelper.WriteLine("PopupFlyoutBase_OnOpening");
    }

    private void Control_OnLoaded(object? Sender, RoutedEventArgs E)
    {
        ViewModel.Initialize();
    }
}
