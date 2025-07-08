using Avalonia.Controls;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Input;
using Serilog;
using SnapX.Avalonia.ViewModels;
using SnapX.Avalonia.Views;
using SnapX.Core.SharpCapture;
using SnapX.Core.Job;
using SnapX.Core.Utils;

namespace SnapX.Avalonia;

public partial class OSTray(App Current, ILogger Logger)
{
    public void display()
    {
           var logoBitmap = new Bitmap(AssetLoader.Open(new Uri("avares://snapx-ui/SnapX_Logo.png")));
                    var trayIcon = new TrayIcon
                    {
                        Icon = new WindowIcon(logoBitmap),
                        ToolTipText = Core.SnapX.AppName,
                        Command = OpenSnapXCommand
                    };

                    var menu = new NativeMenu();
                    menu.Opening += NativeMenu_OnOpening;
                    menu.NeedsUpdate += NativeMenu_OnNeedsUpdate;

                    var about = new NativeMenuItem(TrayTitle)
                    {
                        Icon = logoBitmap,
                        ToolTip = Lang.AboutSnapX
                    };
                    about.Click += NativeMenuItem_SnapX_OnClick;
                    menu.Items.Add(about);
                    menu.Items.Add(new NativeMenuItemSeparator());

                    var capture = new NativeMenuItem("Capture") { Menu = new NativeMenu() };
                    var full = new NativeMenuItem(Lang.UI_Capture_Fullscreen);
                    full.Click += NativeMenuItem_Capture_Fullscreen_OnClick;
                    capture.Menu.Items.Add(full);

                    var windowPicker = new NativeMenuItem(Lang.UI_Dropdown_Window)
                    {
                        Menu = new NativeMenu
                    {
                        new NativeMenuItem("SnapX UI") { Icon = logoBitmap },
                        new NativeMenuItem("Marvel Rivals") { Icon = logoBitmap },
                        new NativeMenuItem("Man of Steel (2013)") { Icon = logoBitmap }
                    }
                    };
                    capture.Menu.Items.Add(windowPicker);

                    var monitorPicker = new NativeMenuItem(Lang.UI_Dropdown_Monitor) { Menu = [] };
                    monitorPicker.Menu.NeedsUpdate += NativeMenu_OnNeedsUpdate;
                    capture.Menu.Items.Add(monitorPicker);

                    capture.Menu.Items.Add(new NativeMenuItem("Region"));
                    capture.Menu.Items.Add(new NativeMenuItem("Region (Light)"));
                    capture.Menu.Items.Add(new NativeMenuItem("Region (Transparent)"));
                    menu.Items.Add(capture);

                    menu.Items.Add(new NativeMenuItem("Upload")
                    {
                        Menu = new NativeMenu
                    {
                        new NativeMenuItem("Upload File..."),
                        new NativeMenuItem("Upload Folder..."),
                        new NativeMenuItem("Upload from clipboard..."),
                        new NativeMenuItem("Upload text..."),
                        new NativeMenuItem("Drag and drop upload..."),
                        new NativeMenuItem("Shorten URL..."),
                        new NativeMenuItem("Tweet message...")
                    }
                    });
                    var captureFullscreenMenuItem = new NativeMenuItem("Capture entire screen");
                    captureFullscreenMenuItem.Click += NativeMenuItem_Capture_Fullscreen_OnClick;
                    var captureActiveWindowMenuItem = new NativeMenuItem("Capture active window");
                    captureActiveWindowMenuItem.Click += NativeMenuItem_Workflows_CaptureActiveWindow_OnClick;
                    var captureActiveScreenMenuItem = new NativeMenuItem("Capture active screen");
                    captureActiveScreenMenuItem.Click += NativeMenuItem_Workflows_CaptureActiveScreen_OnClick;
                    var workflows = new NativeMenuItem("Workflows")
                    {
                        Menu =
                        [
                            captureFullscreenMenuItem,
                        captureActiveScreenMenuItem,
                        captureActiveWindowMenuItem,
                    ]
                    };

                    menu.Items.Add(workflows);

                    menu.Items.Add(new NativeMenuItemSeparator());

                    var open = new NativeMenuItem("Open");
                    open.Command = OpenSnapXCommand;
                    menu.Items.Add(open);

                    var quit = new NativeMenuItem("Quit");
                    quit.Click += NativeMenuItem_Quit_OnClick;
                    menu.Items.Add(quit);

                    trayIcon.Menu = menu;

                    TrayIcon.SetIcons(Current, [trayIcon]);
    }
    private void NativeMenuItem_Quit_OnClick(object? Sender, EventArgs E)
    {
        App.Shutdown();
    }
    private void NativeMenuItem_SnapX_OnClick(object? Sender, EventArgs E)
    {
        App.NativeMenuAboutSnapXClick(Sender, E);
    }

    private void NativeMenuItem_Capture_Fullscreen_OnClick(object? Sender, EventArgs E)
    {
        TaskHelpers.GetScreenshot().CaptureFullscreen();
    }

    private void NativeMenuItem_Workflows_CaptureActiveScreen_OnClick(object? Sender, EventArgs E)
    {
        new CaptureActiveMonitor().CaptureAsync(TaskSettings.GetDefaultTaskSettings());
    }

    private void NativeMenuItem_Workflows_CaptureActiveWindow_OnClick(object? Sender, EventArgs E)
    {
        new CaptureActiveWindow().Capture(TaskSettings.GetDefaultTaskSettings());
    }

    private void NativeMenuItem_Open_OnClick(object? Sender, EventArgs E)
    {
        var MyMainWindow = App.MyMainWindow;
        if (!MyMainWindow?.IsLoaded ?? false)
        {
            var vm = Ioc.Default.GetRequiredService<MainViewModel>();
            MyMainWindow = new MainWindow(vm);
            MyMainWindow.Show();
        }

        if (!MyMainWindow?.IsVisible ?? true) MyMainWindow?.Show();
        MyMainWindow?.Focus();
        MyMainWindow?.Activate();
    }

    [RelayCommand]
    private void OpenSnapX()
    {
        NativeMenuItem_Open_OnClick(this, EventArgs.Empty);
    }
    private void NativeMenu_OnNeedsUpdate(object? Sender, EventArgs E)
    {
        Logger.Information("NativeMenu_OnNeedsUpdate");
    }

    private void NativeMenu_OnOpening(object? Sender, EventArgs E)
    {
        Logger.Information("NativeMenu_OnOpening");
    }
}
