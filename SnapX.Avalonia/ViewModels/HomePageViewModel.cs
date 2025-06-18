using System.Timers;
using Avalonia.Collections;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.Input;
using SnapX.Avalonia.Models;
using SnapX.Core;
using SnapX.Core.Job;
using SnapX.Core.Utils;

namespace SnapX.Avalonia.ViewModels;

public partial class HomePageViewModel : ViewModelBase
{
    public AvaloniaList<ListTaskTemplate> SelectedTasks { get; set; } = [];
    public AvaloniaList<ListTaskTemplate> recentTasks { get; set; } =
        [];
    private System.Timers.Timer _refreshTimer;
    private bool _isRefreshing; // Guard flag to prevent concurrent refreshes
    private int _failedRefreshTasks;
    private DateTime _lastCacheTime = DateTime.MinValue;
    private List<ListTaskTemplate> _cachedTasks;
    private readonly TimeSpan _cacheDuration = TimeSpan.FromSeconds(4);

    public void InvalidateCache()
    {
        _lastCacheTime = DateTime.MinValue;
        _cachedTasks = [];
    }
    public HomePageViewModel()
    {
        // ShowContextMenuCommand = new RelayCommand<ToggleButton>(ShowContextMenu);
        _refreshTimer = new System.Timers.Timer(5000); // Refresh every 5 seconds
    }

    public async Task Initialize()
    {
        TaskManager.InitHistoryManager();
        _refreshTimer.Elapsed += OnRefreshTimerElapsed;
        _refreshTimer.AutoReset = true;
        _refreshTimer.Start();
        RefreshTasks();
    }
    public void StopTimer() => _refreshTimer.Stop();
    public void StartTimer() => _refreshTimer.Start();
    private async void OnRefreshTimerElapsed(object sender, ElapsedEventArgs e)
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
            // ConfigureAwait(false) is good practice here as it's background work.
            await RefreshTasks().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            DebugHelper.WriteException(ex);
        }
        finally
        {
            _isRefreshing = false; // Reset the flag when the refresh is complete (or fails)
        }
    }
    [RelayCommand]
    public void ContextMenuSelection(object Sender)
    {
        SelectedTasks.Add(Sender as ListTaskTemplate);
    }

    [RelayCommand]
    public void DeleteHistoryItemLocally(object Sender)
    {
        var ltt = Sender as ListTaskTemplate;
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
        var success = TaskManager.History.RemoveHistoryItem(task);
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
            // Right-click: Show context menu on the toggle button itself
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
    public async Task RefreshTasks()
    {
        var typeofVM = typeof(HomePageViewModel);

        List<ListTaskTemplate> newDesiredTasks;

        // Check cache first
        if (DateTime.Now - _lastCacheTime < _cacheDuration && _cachedTasks != null)
        {
            newDesiredTasks = _cachedTasks;
        }
        else
        {
            newDesiredTasks = await Task.Run(async () =>
            {
                var historyItems = await TaskManager.History.GetHistoryItemsAsync(30_000).ConfigureAwait(false);
                return historyItems.AsParallel()
                    .Select(task => new ListTaskTemplate(typeofVM, task))
                    .ToList();
            }).ConfigureAwait(false);

            // Update cache
            _cachedTasks = newDesiredTasks;
            _lastCacheTime = DateTime.Now;
        }

        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            if (newDesiredTasks.Count > 50_000)
            {
                recentTasks.ResetBehavior = ResetBehavior.Remove;
                recentTasks.Clear();
                recentTasks.AddRange(newDesiredTasks);
                return;
            }
            var currentTasksById = recentTasks.ToDictionary(template => template.task.Id);

            var newDesiredTaskIds = newDesiredTasks.Select(template => template.task.Id).ToHashSet();

            for (var i = recentTasks.Count - 1; i >= 0; i--)
            {
                if (!newDesiredTaskIds.Contains(recentTasks[i].task.Id))
                {
                    recentTasks.RemoveAt(i);
                }
            }

            foreach (var newItem in newDesiredTasks)
            {
                if (currentTasksById.TryGetValue(newItem.task.Id, out var existingItem))
                {
                    if (existingItem.Equals(newItem))
                    {
                        continue;
                    }
                    var index = recentTasks.IndexOf(existingItem);
                    if (index == -1) continue;
                    recentTasks.RemoveAt(index);
                    recentTasks.Insert(index, newItem);
                }
                else
                {
                    recentTasks.Add(newItem);
                }
            }
        });
    }
}
