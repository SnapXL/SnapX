using System.Collections.ObjectModel;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.Hosting;
using SnapX.Avalonia.Models;
using SnapX.Core;
using SnapX.Core.History;
using SnapX.Core.Job;

namespace SnapX.Avalonia.ViewModels;

public partial class HomePageViewModel : ViewModelBase
{
    public Queue<RecentTask> recentTasks { get; set; }

    public ObservableCollection<ListTaskTemplate> SelectedTasks { get; set; } = new();
    public ICommand ToggleSelectionCommand { get; }
    public ObservableCollection<ListTaskTemplate> RecentTaskss { get; set; } =
        new();
    private System.Timers.Timer _refreshTimer;

    public HomePageViewModel()
    {
        RefreshTasks();
        ToggleSelectionCommand = new RelayCommand<string>(ToggleSelection);
        // ShowContextMenuCommand = new RelayCommand<ToggleButton>(ShowContextMenu);
        _refreshTimer = new System.Timers.Timer(5000); // Refresh every 5 seconds
        _refreshTimer.Elapsed += (s, e) => RefreshTasks();
        _refreshTimer.AutoReset = true;
        _refreshTimer.Start();
    }
    [RelayCommand]
    public void ContextMenuSelection(object Sender)
    {
        SelectedTasks.Add(Sender as ListTaskTemplate);
    }
    private void ToggleSelection(object parameter)
    {
        if (parameter is not ListTaskTemplate item) return;

        var topLevel = TopLevel.GetTopLevel(App.MyMainWindow);
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
    }

    private void OnPointerPressed(object sender, PointerPressedEventArgs e)
    {
        if (sender is ToggleButton button && button.DataContext is HomePageViewModel vm)
        {
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
    }

    private void RefreshTasks()
    {
        recentTasks = TaskManager.RecentManager.Tasks;
        var taskTemplates = recentTasks.Select(task =>
                new ListTaskTemplate(typeof(HomePageViewModel), task))
            .ToList();
        foreach (var taskTemplate in taskTemplates.Where(taskTemplate => !RecentTaskss.Any(existing => existing.Equals(taskTemplate))))
        {
            RecentTaskss.Add(taskTemplate);
        }
        // DebugHelper.WriteLine($"{TaskManager.RecentManager.Tasks.Count} Recent Tasks local recentTasks {recentTasks.Count} final {RecentTaskss.Count}");

    }
}
