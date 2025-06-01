using System.Collections.ObjectModel;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using CommunityToolkit.Mvvm.Input;
using SnapX.Avalonia.Models;
using SnapX.Core;
using SnapX.Core.History;
using SnapX.Core.Job;

namespace SnapX.Avalonia.ViewModels;

public partial class HomePageViewModel : ViewModelBase
{
    public IEnumerable<HistoryItem> recentTasks { get; set; }

    public ObservableCollection<ListTaskTemplate> SelectedTasks { get; set; } = [];
    public ObservableCollection<ListTaskTemplate> RecentTaskss { get; set; } =
        [];
    private System.Timers.Timer _refreshTimer;

    public HomePageViewModel()
    {
        // ShowContextMenuCommand = new RelayCommand<ToggleButton>(ShowContextMenu);
        _refreshTimer = new System.Timers.Timer(5000); // Refresh every 5 seconds
    }

    public void Initialize()
    {
        _refreshTimer.Elapsed += (s, e) => RefreshTasks();
        _refreshTimer.AutoReset = true;
        _refreshTimer.Start();
        RefreshTasks().GetAwaiter().GetResult();
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
        DebugHelper.WriteLine($"File {task.FileName} MUST BE DELETED!!");
        File.Delete(task.FilePath);
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

    private async Task RefreshTasks()
    {
        TaskManager.InitHistoryManager();
        var HistoryItems = await TaskManager.History.GetHistoryItemsAsync();

        var taskTemplates = HistoryItems
            .Select(task => new ListTaskTemplate(typeof(HomePageViewModel), task))
            .ToList();

        foreach (var taskTemplate in taskTemplates.Where(taskTemplate => !RecentTaskss.Any(existing => existing.Equals(taskTemplate))))
        {
            RecentTaskss.Add(taskTemplate);
        }
    }

}
