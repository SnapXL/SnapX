using AsyncImageLoader.Loaders;
using Avalonia.Controls;
using Avalonia.Interactivity;
using FluentAvalonia.UI.Controls;
using SnapX.Avalonia.Models;
using SnapX.Avalonia.Services;
using SnapX.Avalonia.ViewModels;
using SnapX.Core;
using SnapX.Core.Utils;
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

    private void OCRImageClick(object? Sender, RoutedEventArgs E)
    {
        if (Sender is not MenuFlyoutItem menuFlyoutItem) return;

        ViewModel.OCRImageCommand.Execute(menuFlyoutItem.DataContext);
    }

    private void DownloadButton_OnClick(object? Sender, RoutedEventArgs E)
    {
        if (Sender is not MenuFlyoutItem menuFlyoutItem) return;
        ViewModel.DownloadButtonCommand.Execute(menuFlyoutItem.DataContext);
        ViewModel.StopTimer();
        ViewModel.RefreshTasks();
        ViewModel.StartTimer();
    }

    private void UploadButton_OnClick(object? Sender, RoutedEventArgs E)
    {
        if (Sender is not MenuFlyoutItem menuFlyoutItem) return;
        ViewModel.UploadButtonCommand.Execute(menuFlyoutItem.DataContext);
    }
    private void DynamicOpenURL(object? sender, RoutedEventArgs e)
    {
        if (sender is not MenuFlyoutItem menuFlyoutItem)
            return;

        if (menuFlyoutItem.DataContext is not ListTaskTemplate listTaskTemplate)
            return;
        // menuFlyoutItem.Command?.Execute(menuFlyoutItem.CommandParameter);
        var path = menuFlyoutItem.Tag as string;
        ViewModel.OpenURLCommand.Execute(path);
    }
    private void OpenFolder_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is not MenuFlyoutItem menuItem) return;

        var filePath = menuItem.Tag as string;
        if (string.IsNullOrEmpty(filePath)) return;

        var folderPath = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(folderPath))
        {
            URLHelpers.OpenURL(folderPath);
        }
    }

    private void DynamicCopy(object? sender, RoutedEventArgs e)
    {
        if (sender is not MenuFlyoutItem menuItem) return;
        var text = menuItem.Tag as string;
        ClipboardService.Owner.Clipboard?.SetTextAsync(text);
    }


}
