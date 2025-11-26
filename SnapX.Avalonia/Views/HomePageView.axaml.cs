using AsyncImageLoader.Loaders;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using FluentAvalonia.UI.Controls;
using SnapX.Avalonia.Models;
using SnapX.Avalonia.ViewModels;
using SnapX.Core;
using SnapX.Core.Upload;
using SnapX.Core.Utils;
using HttpClientFactory = SnapX.Core.Utils.Miscellaneous.HttpClientFactory;

namespace SnapX.Avalonia;

public partial class HomePageView : UserControl
{
    private HomePageViewModel ViewModel;

    public HomePageView(HomePageViewModel vm)
    {
        DataContext = vm;
        ViewModel = vm;
        InitializeComponent();
        AddHandler(DragDrop.DragOverEvent, DragOver);
        AddHandler(DragDrop.DragEnterEvent, DragEnter);
        AddHandler(DragDrop.DropEvent, Drop);
        AsyncImageLoader.ImageLoader.AsyncImageLoader = new DiskCachedWebImageLoader(
            HttpClientFactory.Get(),
            false,
            Path.Combine(Core.SnapX.CacheFolder, "Images")
        );
    }

    private async void DoDrag(
        Action<DataObject> factory,
        PointerEventArgs e,
        DragDropEffects effects
    )
    {
        var dragData = new DataObject();
        factory(dragData);

        var result = await DragDrop.DoDragDrop(e, dragData, effects);
    }

    private void DragEnter(object? Sender, DragEventArgs e)
    {
        // DebugHelper.WriteLine("DragEnter Event");
        // DebugHelper.WriteLine($"Sender: {Sender} | EventArgs: {e.GetPosition(this)}");
    }

    private void DragOver(object? Sender, DragEventArgs e)
    {
        // DebugHelper.WriteLine("DragOver Event");
        // DebugHelper.WriteLine($"Sender: {Sender} | EventArgs: {e.GetPosition(this)}");
    }

    private void Drop(object? Sender, DragEventArgs e)
    {
        DebugHelper.WriteLine("Drop Event");
        DebugHelper.WriteLine($"Sender: {Sender} | EventArgs: {e.GetPosition(this)}");
        if (e.Source is Control)
        {
            e.DragEffects &= DragDropEffects.Move;
        }
        else
        {
            e.DragEffects &= DragDropEffects.Copy;
        }
        if (e.Data.Contains(DataFormats.Text))
        {
            UploadManager.UploadText(e.Data.GetText());
        }
        else if (e.Data.Contains(DataFormats.Files))
        {
            var files = e.Data.GetFiles() ?? Array.Empty<IStorageItem>();

            foreach (var item in files)
            {
                switch (item)
                {
                    case IStorageFile file:
                        UploadManager.UploadFile(file.Path.AbsolutePath);
                        break;
                    case IStorageFolder folder:
                        UploadManager.UploadFolder(folder.Path.AbsolutePath);
                        break;
                }
            }
        }

        DebugHelper.WriteLine($"{string.Join(", ", e.Data.GetDataFormats())}");
    }

    public HomePageView()
        : this(new HomePageViewModel()) { }

    private void PopupFlyoutBase_OnOpening(object? Sender, EventArgs E)
    {
        DebugHelper.WriteLine("PopupFlyoutBase_OnOpening");
    }

    private void Control_OnLoaded(object? Sender, RoutedEventArgs E)
    {
        Task.Run(() => ViewModel.Initialize()).ConfigureAwait(false);
    }
    private void Control_OnInitialized(object? Sender, EventArgs E)
    {

    }

    private void DeleteLocallyButton_OnClick(object? Sender, RoutedEventArgs E)
    {
        if (Sender is not MenuFlyoutItem menuFlyoutItem)
            return;
        ViewModel.DeleteHistoryItemLocallyCommand.Execute(menuFlyoutItem.DataContext);
        ViewModel.InvalidateCache();
        ViewModel.StopTimer();
        _ = ViewModel.RefreshTasks();
        ViewModel.StartTimer();
    }

    private void RemoveHistoryItem_OnClick(object? Sender, RoutedEventArgs E)
    {
        if (Sender is not MenuFlyoutItem menuFlyoutItem)
            return;
        ViewModel.RemoveHistoryItemCommand.Execute(menuFlyoutItem.DataContext);
        ViewModel.InvalidateCache();
        ViewModel.StopTimer();
        _ = ViewModel.RefreshTasks();
        ViewModel.StartTimer();
    }

    private void Control_OnUnloaded(object? Sender, RoutedEventArgs E)
    {
        ViewModel.StopTimer();
        _ = ViewModel.HaltActiveTasks();
    }

    private void OCRImageClick(object? Sender, RoutedEventArgs E)
    {
        if (Sender is not MenuFlyoutItem menuFlyoutItem)
            return;

        ViewModel.OCRImageCommand.Execute(menuFlyoutItem.DataContext);
    }

    private void DownloadButton_OnClick(object? Sender, RoutedEventArgs E)
    {
        if (Sender is not MenuFlyoutItem menuFlyoutItem)
            return;
        ViewModel.DownloadButtonCommand.Execute(menuFlyoutItem.DataContext);
        ViewModel.StopTimer();
        ViewModel.RefreshTasks();
        ViewModel.StartTimer();
    }

    private void UploadButton_OnClick(object? Sender, RoutedEventArgs E)
    {
        if (Sender is not MenuFlyoutItem menuFlyoutItem)
            return;
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
        if (sender is not MenuFlyoutItem menuItem)
            return;

        var filePath = menuItem.Tag as string;
        if (string.IsNullOrEmpty(filePath))
            return;

        var folderPath = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(folderPath))
        {
            URLHelpers.OpenURL(folderPath);
        }
    }

    private void DynamicCopy(object? sender, RoutedEventArgs e)
    {
        if (sender is not MenuFlyoutItem menuItem)
            return;
        if (menuItem.Tag is not string text)
            return;
        var topLevel = TopLevel.GetTopLevel(menuItem);
        if (topLevel is null)
        {
            DebugHelper.WriteLine("TopLevel is null");
            return;
        }
        topLevel.Clipboard?.SetTextAsync(text);
    }
}
