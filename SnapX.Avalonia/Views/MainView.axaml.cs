using Avalonia.Controls;
using CommunityToolkit.Mvvm.Input;
using FluentAvalonia.UI.Controls;
using SnapX.Avalonia.ViewModels;
using SnapX.Core;
using SnapX.Core.Job;
using SnapX.Core.Upload;
using Image = SixLabors.ImageSharp.Image;

namespace SnapX.Avalonia.Views;

public partial class MainView : UserControl
{
    private readonly FAMenuFlyout _flyout;
    private readonly SplitButton _splitButton;
    private readonly Label _captureRegionLabel;
    private string? selectedAction;

    public MainView()
    {
        InitializeComponent();
        _splitButton = this.FindControl<SplitButton>("CaptureSplitButton");
        _captureRegionLabel = this.FindControl<Label>("RegionCaptureLabel");
        _splitButton.Command = ExecuteSelectedCaptureActionCommand;
        selectedAction = _captureRegionLabel?.Content as string;
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

    [RelayCommand]
    private void ExecuteSelectedCaptureAction()
    {
        var action = selectedAction ?? _captureRegionLabel.Content as string;
        DebugHelper.WriteLine($"Executing: {action}");
        Image? img = null;
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
                img = TaskHelpers.GetScreenshot(TaskSettings.GetDefaultTaskSettings()).CaptureActiveMonitor().ConfigureAwait(false).GetAwaiter().GetResult();
                break;
        }

        if (img != null) UploadManager.RunImageTask(img, TaskSettings.GetDefaultTaskSettings());
    }
    [RelayCommand]
    private void SelectCaptureAction(string action)
    {
        DebugHelper.WriteLine($"Selecting: {action}");
        selectedAction = action;
        _captureRegionLabel.Content = action;
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
