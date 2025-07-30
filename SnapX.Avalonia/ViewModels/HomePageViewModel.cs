using System.Timers;
using Avalonia.Collections;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.Input;
using SnapX.Avalonia.Extensions;
using SnapX.Avalonia.Views;
using SnapX.CommonUI.Models;
using SnapX.CommonUI.ViewModels;
using SnapX.Core;
using SnapX.Core.Job;
using SnapX.Core.Upload;
using SnapX.Core.Utils;

namespace SnapX.Avalonia.ViewModels;

public partial class HomePageViewModel : ViewModelBase
{
    public AvaloniaList<ListTaskTemplate> SelectedTasks { get; set; } = [];
    public AvaloniaList<ListTaskTemplate> recentTasks { get; set; } = [];
    private readonly System.Timers.Timer _refreshTimer = new(5000); // Refresh every 5 seconds
    private bool _isRefreshing;
    private int _failedRefreshTasks;
    public void InvalidateCache()
    {
    }
    // ShowContextMenuCommand = new RelayCommand<ToggleButton>(ShowContextMenu);

    public Task Initialize()
    {
        TaskManager.InitHistoryManager();
        _refreshTimer.Elapsed += OnRefreshTimerElapsed;
        _refreshTimer.AutoReset = true;
        _refreshTimer.Start();
        _ = RefreshTasks();
        return Task.CompletedTask;
    }
    public void StopTimer() => _refreshTimer.Stop();
    public void StartTimer() => _refreshTimer.Start();
    private async void OnRefreshTimerElapsed(object sender, ElapsedEventArgs e)
    {
        try
        {
            if (_isRefreshing)
            {
                DebugHelper.WriteLine("Previous timer run already in progress. Skipping this timer tick.");
                // Apply more conservative _refreshTimer interval when we know that there's a bunch of tasks.
                if (recentTasks.Count > 3000) _refreshTimer.Interval = 10_000;
                if (_failedRefreshTasks > 15) _refreshTimer.Interval = 30_000;
                if (_failedRefreshTasks > 10) _refreshTimer.Interval = 20_000;
                if (_failedRefreshTasks > 5) _refreshTimer.Interval = 10_000;
                if (_failedRefreshTasks > 19) _refreshTimer.Interval = 60_000;
                // Fuck it, give up.
                if (_failedRefreshTasks > 20) _refreshTimer.Stop();
                _failedRefreshTasks++;
                return;
            }

            _isRefreshing = true;
            try
            {
                await RefreshTasks().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                DebugHelper.WriteException(ex);
            }
            finally
            {
                _isRefreshing = false;
            }
        }
        catch (Exception ex)
        {
            DebugHelper.WriteException(ex, "Error while refreshing tasks");
        }
    }
    [RelayCommand]
    public void ContextMenuSelection(object Sender)
    {
        if (Sender is not ListTaskTemplate Template) return;
        SelectedTasks.Add(Template);
    }

    [RelayCommand]
    public void DeleteHistoryItemLocally(object Sender)
    {
        if (Sender is not ListTaskTemplate ltt) return;
        var task = ltt.task;
        if (string.IsNullOrWhiteSpace(task.FilePath))
        {
            DebugHelper.WriteLine($"DeleteHistoryItemLocally called with a invalid file path: '{task.FilePath}'. The task file name is '{task.FileName}'");
            return;
        }
        DebugHelper.WriteLine($"Deleting file {task.FilePath}");
        try
        {
            File.Delete(task.FilePath);
        }
        catch (Exception ex)
        {
            DebugHelper.WriteException(ex);
        }

        task.FilePath = null;
        TaskManager.History.UpdateHistoryItem(task);
    }
    [RelayCommand]
    public void RemoveHistoryItem(object Sender)
    {
        if (Sender is not ListTaskTemplate ltt) return;
        var task = ltt.task;
        DebugHelper.WriteLine($"Removing {task.FilePath ?? task.FileName} (Id: {task.Id}) from history");
        var success = TaskManager.History.RemoveHistoryItems([task]);
        var status = success ? "Success" : "Failure";
        DebugHelper.WriteLine($"{status} removing history item {task.FilePath ?? task.FileName}");
    }
    [RelayCommand]
    private void ToggleSelection(object parameter)
    {
        if (parameter is not ListTaskTemplate item) return;

        // var topLevel = TopLevel.GetTopLevel(App.MyMainWindow);
        // var isCtrlPressed = topLevel?.InputManager?.KeyboardDevice?.Modifiers.HasFlag(KeyModifiers.Control) ?? false;
        //
        // if (!isCtrlPressed)
        // {
        //     SelectedTasks.Clear();
        // }

        if (SelectedTasks.Contains(item))
        {
            SelectedTasks.Remove(item);
        }
        else
        {
            SelectedTasks.Add(item);
        }
        DebugHelper.WriteLine(string.Join("\n", SelectedTasks.Select(t => t.ToString())));
    }

    private void OnPointerPress(object sender, PointerPressedEventArgs e)
    {
        if (sender is not ToggleButton { DataContext: HomePageViewModel vm } button) return;
        var item = button.Tag as string;

        if (e.GetCurrentPoint(button).Properties.IsRightButtonPressed)
        {
            // Right-click: Show a context menu on the toggle button itself
            vm.ContextMenuSelectionCommand.Execute(button);
        }
        else if (e.GetCurrentPoint(button).Properties.IsLeftButtonPressed)
        {
            // Left-click: Toggle selection
            vm.ToggleSelectionCommand.Execute(item);
        }
    }

    [RelayCommand]
    public void OpenURL(object parameter)
    {
        DebugHelper.WriteLine("OpenURL");
        if (parameter is not string url) return;
        URLHelpers.OpenURL(url);
    }
    [RelayCommand]
    public void OCRImage(object Sender)
    {
        DebugHelper.WriteLine("OCRImage");
        if (Sender is not ListTaskTemplate ltt) return;
        DebugHelper.WriteLine("OCRImage 2");
        var OcrWindow = new OCR(ltt.task);
        OcrWindow.Show();
    }

    [RelayCommand]
    public void DownloadButton(object Sender)
    {
        if (Sender is not ListTaskTemplate ltt) return;
        var taskSettings = TaskSettings.GetDefaultTaskSettings();
        var url = ltt.task.URL ?? ltt.task.ThumbnailURL;

        var task = WorkerTask.CreateDownloadTask(url, false, taskSettings);

        if (task != null)
        {
            TaskManager.Start(task);
        }
    }
    [RelayCommand]
    public void UploadButton(object Sender)
    {
        if (Sender is not ListTaskTemplate ltt) return;
        if (ltt.task.FilePath is null)
        {
            DebugHelper.WriteLine("UploadButton called with a null path");
            return;
        }
        UploadManager.UploadFile(ltt.task.FilePath);
    }

    public async Task RefreshTasks()
    {
        var typeofVM = typeof(HomePageViewModel);

        var historyItems = await TaskManager.History.GetHistoryItemsAsync().ConfigureAwait(false);

        var tasks = historyItems
            .Select(task => new ListTaskTemplate(typeofVM, task))
            .OrderByDescending(item => item.task.Id)
            .ToList();

        List<ListTaskTemplate> toAdd = [];
        List<ListTaskTemplate> toUpdate = [];
        List<int> toRemove;

        {
            var currentTasksById = recentTasks.ToDictionary(t => t.task.Id);
            var newTaskIds = tasks.Select(t => t.task.Id).ToHashSet();

            toRemove = recentTasks
                .Where(t => !newTaskIds.Contains(t.task.Id))
                .Select(t => t.task.Id)
                .ToList();

            foreach (var newItem in tasks)
            {
                if (currentTasksById.TryGetValue(newItem.task.Id, out var existing))
                {
                    if (!existing.Equals(newItem))
                        toUpdate.Add(newItem);
                }
                else
                {
                    toAdd.Add(newItem);
                }
            }
        }
        // Warning: Computations on the UIThread are precious.
        await Dispatcher.UIThread.InvokeAsync(() =>
        {

            for (var i = recentTasks.Count - 1; i >= 0; i--)
            {
                if (toRemove.Contains(recentTasks[i].task.Id))
                    recentTasks.RemoveAt(i);
            }

            foreach (var item in toUpdate)
            {
                var index = recentTasks.FindIndex(t => t.task.Id == item.task.Id);
                if (index == -1) continue;
                recentTasks.RemoveAt(index);
                recentTasks.Insert(index, item);
            }

            foreach (var item in toAdd)
            {
                recentTasks.Insert(0, item);
            }
        }).GetTask().ConfigureAwait(false);
    }
}
