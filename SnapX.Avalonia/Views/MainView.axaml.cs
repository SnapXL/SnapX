using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;
using CommunityToolkit.Mvvm.Input;
using FluentAvalonia.UI.Controls;
using SnapX.Avalonia.ViewModels;
using SnapX.Avalonia.Views.Controls;
using SnapX.Core;
using SnapX.Core.Job;
using SnapX.Core.ScreenCapture;
using SnapX.Core.Upload;
using SnapX.Core.Utils;
using SnapX.Core.Utils.Extensions;
using SnapX.Core.Utils.Miscellaneous;
using Image = SixLabors.ImageSharp.Image;

namespace SnapX.Avalonia.Views;

public partial class MainView : UserControl
{
    private string? selectedAction;
    private TimeSpan? delay;

    public MainView()
    {
        InitializeComponent();
        var _flyout = CaptureSplitButton;
        if (_flyout != null)
        {
            foreach (var item in _flyout.MenuItems)
            {
                if (item is NavigationViewItem menuItem)
                {
                    if (menuItem.Tag != null)
                        return;
                    if (menuItem.Name == "DelayMenuItem")
                        continue;
                    menuItem.PointerPressed += (Sender, Args) =>
                        SelectCaptureActionCommand.Execute(menuItem.Content as string);
                }
            }
        }
    }

    [RelayCommand]
    private async Task ExecuteSelectedCaptureAction(string? theAction = null)
    {
        var action = selectedAction ?? theAction;
        DebugHelper.WriteLine($"Executing: {action}");
        Image? img = null;
        var actionMap = new Dictionary<string, Func<Task>>
        {
            [Lang.UI_Capture_Fullscreen] = async () =>
            {
                img = await Task.Run(
                    () =>
                        TaskHelpers
                            .GetScreenshot(TaskSettings.GetDefaultTaskSettings())
                            .CaptureFullscreen()
                );
            },
            [Lang.UI_Dropdown_Region] = async () =>
            {
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
                img = await Task.Run(
                    () =>
                        TaskHelpers
                            .GetScreenshot(TaskSettings.GetDefaultTaskSettings())
                            .CaptureActiveWindow()
                );
            },
            [Lang.UI_Dropdown_Monitor] = async () =>
            {
                img = await Task.Run(
                    () =>
                        TaskHelpers
                            .GetScreenshot(TaskSettings.GetDefaultTaskSettings())
                            .CaptureActiveMonitor()
                );
            },
            [Lang.UI_Dropdown_ScreenRecording] = async () =>
            {
                // var rect = new RegionSelectorWindow(new RegionSelectorViewModel()).Show();
                // var
                TaskHelpers.StartScreenRecording(
                    ScreenRecordOutput.FFmpeg,
                    ScreenRecordStartMethod.Region
                );
            },
        };
        if (action != null && actionMap.TryGetValue(action, out var func))
        {
            if (delay != null && delay.HasValue)
                await Task.Delay((int)delay.Value.TotalMilliseconds);
            await func();
        }
        else
        {
            DebugHelper.WriteLine("No matching action found.");
        }

        if (img != null)
            UploadManager.RunImageTask(img, TaskSettings.GetDefaultTaskSettings());
    }

    [RelayCommand]
    private async Task ExecuteSelectedTool(string action)
    {
        var actionMap = new Dictionary<string, Func<Task>>
        {
            ["QR Code"] = async () =>
            {
                var qrWindow = new QRCodeView();
                qrWindow.Show();
            },
        };

        if (action != null && actionMap.TryGetValue(action, out var func))
        {
            await func();
        }
        else
        {
            DebugHelper.WriteLine("No matching tool found.");
        }
    }

    private void DelayOption_Checked(object? sender, RoutedEventArgs e)
    {
        DebugHelper.WriteLine("DelayOption_Checked");
        if (sender is not NavigationViewItem item)
            return;
        if (item.Tag is null)
            return;

        delay = TimeSpan.FromSeconds(long.Parse(item.Tag as string));
        Core.SnapX.Settings.DefaultTaskSettings.CaptureSettings.ScreenshotDelay = (decimal)
            delay.Value.TotalSeconds;
        var DelayMenuItem = this.FindControl<NavigationViewItem>("DelayMenuItem");
        if (DelayMenuItem == null || DelayMenuItem.MenuItems == null)
            return;

        long targetSeconds = (long)delay.Value.TotalSeconds;

        foreach (var menuItem in DelayMenuItem.MenuItems.Cast<NavigationViewItem>())
        {
            if (menuItem.Tag is string tag && long.TryParse(tag, out long tagValue))
            {
                if (tagValue == targetSeconds)
                {
                    // menuItem.IsSelected = true;
                    var content = menuItem.Content as string;
                    if (!content.StartsWith("✓ "))
                        menuItem.Content = "✓ " + content;
                }
                else
                {
                    if (menuItem.Content is string content && content.StartsWith("✓ "))
                    {
                        menuItem.Content = content.Substring(2);
                    }
                }
            }
        }
        Core.SnapX.Settings.SaveAsync();
    }

    [RelayCommand]
    private void SelectCaptureAction(string action)
    {
        DebugHelper.WriteLine($"Selecting: {action}");
        selectedAction = action;

        ExecuteSelectedCaptureActionCommand.ExecuteAsync(action);
    }

    private void AboutItem_Pressed(object? Sender, PointerPressedEventArgs E)
    {
        App.CreateAboutWindowStatic();
    }

    private void SettingsItem_Pressed(object? Sender, PointerPressedEventArgs E)
    {
        App.CreateOrOpenSettingsWindowStatic();
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
            if (!string.IsNullOrEmpty(url))
                URLHelpers.OpenURL(url);
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
                $"{nameof(DynamicURL_OnPointerPressed)} called with {Sender} which is not a Control!!"
            );
        }
    }

    private void OpenDebugLog(object? Sender, PointerPressedEventArgs E)
    {
        var window = new LogViewer();
        window.Show(App.MyMainWindow!);
    }

    private void MainView_OnInit(object? Sender, EventArgs E)
    {
        delay = TimeSpan.FromSeconds(
            (long)Core.SnapX.Settings.DefaultTaskSettings.CaptureSettings.ScreenshotDelay
        );

        var MainNavView = this.FindControl<NavigationView>("MainNavView");
        if (MainNavView != null)
        {
            MainNavView.Loaded -= MainNavView_Loaded_SetSelection;
            MainNavView.Loaded += MainNavView_Loaded_SetSelection;
        }
    }

    private void MainNavView_Loaded_SetSelection(object? sender, RoutedEventArgs e)
    {
        if (sender is not NavigationView MainNavView)
            return;

        MainNavView.Loaded -= MainNavView_Loaded_SetSelection;

        var DelayMenuItem = MainNavView.FindControl<NavigationViewItem>("DelayMenuItem");
        if (DelayMenuItem == null || DelayMenuItem.MenuItems == null)
            return;

        long targetSeconds = (long)delay.Value.TotalSeconds;

        foreach (var item in DelayMenuItem.MenuItems.Cast<NavigationViewItem>())
        {
            if (item.Tag is string tag && long.TryParse(tag, out long tagValue))
            {
                if (tagValue == targetSeconds)
                {
                    item.IsSelected = true;
                    var content = item.Content as string;
                    item.Content = "✓ " + content;
                    return;
                }
            }
        }
    }

    private void DonateButtonPressed(object? sender, PointerPressedEventArgs e)
    {
        var donationMenu = new Donation();
        var dialog = new ContentDialog
        {
            Title = Lang.KeepSnapXOpenAndFree,
            Content = donationMenu,
            IsPrimaryButtonEnabled = true,
            PrimaryButtonText = Lang.CountMeIn,
            IsSecondaryButtonEnabled = true,
            SecondaryButtonText = Lang.MaybeLater,
            DefaultButton = ContentDialogButton.Primary,
            PrimaryButtonCommand = donationMenu.PrimaryClickCommand,
            FullSizeDesired = true,
        };
        if (App.MyMainWindow != null)
            dialog.ShowAsync(App.MyMainWindow);
        else
            dialog.ShowAsync();
    }

    private async void DynamicDebugPressed(object? Sender, PointerPressedEventArgs E)
    {
        try
        {
            if (Sender is not NavigationViewItem navigationViewItem)
                return;
            var target = navigationViewItem.Content as string;
            if (string.IsNullOrEmpty(target))
                return;
            DebugHelper.WriteLine($"{nameof(DynamicDebugPressed)}: {target}");
            var actionMap = new Dictionary<string, Func<Task>>
            {
                [Lang.UI_Debug_TestImageUpload] = async () =>
                {
                    UploadManager.UploadImage(
                        await WebHelpers.DownloadImageAsync(
                            "https://github.com/SnapXL/SnapX/blob/v0.3.0/.github/Linux.png?raw=true"
                        )
                    );
                },
                [Lang.UI_Debug_TestTextUpload] = async () =>
                {
                    UploadManager.UploadText(
                        "This is a test text upload from SnapX, a fork of ShareX"
                    );
                },
                [Lang.UI_Debug_TestFileUpload] = async () =>
                {
                    UploadManager.DownloadAndUploadFile(
                        "https://raw.githubusercontent.com/SnapXL/SnapX/830fc50125e7af3e760b2ff908635d97e2464695/.github/Progress.md"
                    );
                },
                [Lang.UI_Debug_TestURLShortener] = async () =>
                {
                    UploadManager.ShortenURL(Links.Website);
                },
                [Lang.UI_Debug_TestURLSharing] = async () =>
                {
                    UploadManager.ShareURL(Links.Website);
                },
            };
            if (actionMap.TryGetValue(target, out var func))
            {
                await func();
            }
            else
            {
                DebugHelper.WriteLine("No matching action found.");
            }
        }
        catch (Exception e)
        {
            e.ShowError();
        }
    }

    private void ToolClicked(object? Sender, PointerPressedEventArgs E)
    {
        ExecuteSelectedToolCommand.Execute((Sender as NavigationViewItem).Content);
    }
}
