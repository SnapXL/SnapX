using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.LogicalTree;
using CommunityToolkit.Mvvm.Input;
using FluentAvalonia.UI.Controls;
using SnapX.Avalonia.ViewModels;
using SnapX.Core;
using SnapX.Core.Job;
using SnapX.Core.Upload;
using SnapX.Core.Utils;
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
    private async Task ExecuteSelectedCaptureAction()
    {
        var action = selectedAction ?? _captureRegionLabel.Content as string;
        DebugHelper.WriteLine($"Executing: {action}");
        Image? img = null;
        var actionMap = new Dictionary<string, Func<Task>>
        {
            [Lang.UI_Dropdown_Region] = async () =>
            {
                await Task.Delay(5000);
                new RegionSelectorWindow(new RegionSelectorViewModel()).Show();
            },
            [Lang.UI_Dropdown_RegionLight] = async () =>
            {
                new RegionSelectorWindow(new RegionSelectorViewModel()).Show();
            },
            [Lang.UI_Dropdown_RegionTransparent] = async () =>
            {
                new RegionSelectorWindow(new RegionSelectorViewModel()).Show();
            },
            [Lang.UI_Dropdown_Window] = async () =>
            {
                await Task.Delay(5000);
                img = await Task.Run(() =>
                    TaskHelpers.GetScreenshot(TaskSettings.GetDefaultTaskSettings()).CaptureActiveWindow()
                );
            },
            [Lang.UI_Dropdown_Monitor] = async () =>
            {
                await Task.Delay(5000);
                img = await Task.Run(() =>
                    TaskHelpers.GetScreenshot(TaskSettings.GetDefaultTaskSettings()).CaptureActiveMonitor()
                );
            }
        };
        if (action != null && actionMap.TryGetValue(action, out var func))
        {
            await func();
        }
        else
        {
            DebugHelper.WriteLine("No matching action found.");
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

        ExecuteSelectedCaptureActionCommand.ExecuteAsync(this);
    }

    private void AboutItem_Pressed(object? Sender, PointerPressedEventArgs E)
    {
        App.CreateAboutWindowStatic();
    }

    private void SettingsItem_Pressed(object? Sender, PointerPressedEventArgs E)
    {
        App.CreateSettingsWindowStatic();
    }
    private void FindURLOnDescendant(ILogical control)
    {
        foreach (var child in control.GetLogicalChildren())
        {
            var toolTip = child.FindLogicalDescendantOfType<ToolTip>(true);
            if (toolTip is null)
            {
                FindURLOnDescendant(child);
            }

            var url = toolTip?.Content as string ?? string.Empty;
            if (!string.IsNullOrEmpty(url)) URLHelpers.OpenURL(url);
        }
    }

    private void DynamicURL_OnPointerPressed(object? Sender, PointerPressedEventArgs E)
    {
        DebugHelper.WriteLine($"{nameof(DynamicURL_OnPointerPressed)}: {Sender} {E.Source}");
        if (Sender is Control control)
        {
            // The ToolTip class has a storage of loaded tooltips, however, when a user clicks without hovering for a second the button didn't work.
            // So I added the second if-clause.
            if (ToolTip.GetTip(control) is string url)
            {
                URLHelpers.OpenURL(url);
                return;
            }

            FindURLOnDescendant(control);
        }
        else
        {
            DebugHelper.WriteLine(
                $"{nameof(DynamicURL_OnPointerPressed)} called with {Sender} which is not a Control!!");
        }
    }

    private void OpenDebugLog(object? Sender, PointerPressedEventArgs E)
    {
        var window = new LogViewer();
        window.Show(App.MyMainWindow!);
    }

    private void MainView_OnInit(object? Sender, EventArgs E)
    {
    }
}
