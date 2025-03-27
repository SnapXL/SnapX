using System.Windows.Input;
using Avalonia.Controls;
using Avalonia.Interactivity;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FluentAvalonia.UI.Controls;
using SnapX.Avalonia.ViewModels;
using SnapX.Core;
using SnapX.Core.Job;
using SnapX.Core.Media;
using SnapX.Core.Upload;

namespace SnapX.Avalonia.Views;

public partial class MainView : UserControl
{

    public MainView()
    {
        InitializeComponent();
        _splitButton = this.FindControl<SplitButton>("CaptureSplitButton");
        _splitButton.Command = ExecuteSelectedCaptureActionCommand;
        selectedAction = _splitButton?.Content as string;
        _flyout = _splitButton.Flyout as FAMenuFlyout;
        if (_flyout != null)
        {
            foreach (var item in _flyout.Items)
            {
                if (item is ToggleMenuFlyoutItem menuItem)
                {
                    menuItem.Command = SelectCaptureActionCommand;
                    menuItem.CommandParameter = menuItem.Text;

                }
            }
        }

    }
    private SplitButton _splitButton;
    private FAMenuFlyout _flyout;
    private string? selectedAction;

    [RelayCommand]
    private void ExecuteSelectedCaptureAction()
    {
        var action = selectedAction ?? _splitButton.Content as string;
        DebugHelper.WriteLine($"Executing: {action}");
        SixLabors.ImageSharp.Image? img = null;
        switch (action)
        {
            case "Region":
                new RegionSelectorWindow(new RegionSelectorViewModel()).Show();
                break;
            case "Region (Light)":
                new RegionSelectorWindow(new RegionSelectorViewModel()).Show();
                break;
            case "Region (Transparent)":
                new RegionSelectorWindow(new RegionSelectorViewModel()).Show();
                break;
            case "Window":
                img = TaskHelpers.GetScreenshot(TaskSettings.GetDefaultTaskSettings()).CaptureActiveWindow();
                break;
            case "Monitor":
                img = TaskHelpers.GetScreenshot(TaskSettings.GetDefaultTaskSettings()).CaptureActiveMonitor();
                break;
        }
        if (img != null) UploadManager.RunImageTask(img, TaskSettings.GetDefaultTaskSettings());
    }
    [RelayCommand]
    private void SelectCaptureAction(string action)
    {
        DebugHelper.WriteLine($"Selecting: {action}");
        selectedAction = action;
        _splitButton.Content = action;
            if (_flyout != null)
        {
            foreach (var item in _flyout.Items)
            {
                if (item is ToggleMenuFlyoutItem menuItem)
                {
                    menuItem.IsChecked = menuItem.Text == action;
                }
            }
        }
        ExecuteSelectedCaptureActionCommand.Execute(this);
    }
}
