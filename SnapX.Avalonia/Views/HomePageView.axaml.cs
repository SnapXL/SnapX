using AsyncImageLoader.Loaders;
using Avalonia.Controls;
using Avalonia.Interactivity;
using FluentAvalonia.UI.Controls;
using SnapX.Avalonia.ViewModels;
using SnapX.Core;
using SnapX.Core.Utils.Miscellaneous;

namespace SnapX.Avalonia;

public partial class HomePageView : UserControl
{
    private HomePageViewModel ViewModel;

    public HomePageView(HomePageViewModel vm)
    {
        DataContext = vm;
        ViewModel = vm;
        InitializeComponent();
        AsyncImageLoader.ImageLoader.AsyncImageLoader = new DiskCachedWebImageLoader(HttpClientFactory.Get(), false, Path.Combine(Core.SnapX.CacheFolder, "Images"));
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
        Task.Run(() => ViewModel.Initialize()).ConfigureAwait(false);
    }

    private void DeleteLocallyButton_OnClick(object? Sender, RoutedEventArgs E)
    {
        if (Sender is not MenuFlyoutItem menuFlyoutItem) return;
        ViewModel.DeleteHistoryItemLocallyCommand.Execute(menuFlyoutItem.DataContext);
        ViewModel.InvalidateCache();
        ViewModel.StopTimer();
        ViewModel.RefreshTasks();
        ViewModel.StartTimer();
    }

    private void RemoveHistoryItem_OnClick(object? Sender, RoutedEventArgs E)
    {
        if (Sender is not MenuFlyoutItem menuFlyoutItem) return;
        ViewModel.RemoveHistoryItemCommand.Execute(menuFlyoutItem.DataContext);
        ViewModel.InvalidateCache();
        ViewModel.StopTimer();
        ViewModel.RefreshTasks();
        ViewModel.StartTimer();
    }

    private void Control_OnUnloaded(object? Sender, RoutedEventArgs E)
    {
        ViewModel.StopTimer();
    }

    private void Button_OnClick(object? Sender, RoutedEventArgs E)
    {
        ViewModel.StopTimer();
    }
}
