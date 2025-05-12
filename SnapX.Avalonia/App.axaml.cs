using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Layout;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Styling;
using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using FluentAvalonia.UI.Windowing;
using LibVLCSharp.Shared;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using SnapX.Avalonia.ViewModels;
using SnapX.Avalonia.Views;
using SnapX.Core;
using SnapX.Core.Capture;
using SnapX.Core.Job;
using SnapX.Core.Utils;
using SnapX.Core.Utils.Native;

namespace SnapX.Avalonia;

public partial class App : Application
{
    public App()
    {
        DataContext = this;
    }

    public static SnapXAvalonia SnapX { get; private set; }
    public static MainWindow? MyMainWindow { get; private set; }
    public static string TrayTitle => $"SnapX v{SimpleVersion()}";

    private static string SimpleVersion()
    {
        var version = Version.Parse(Helpers.GetApplicationVersion());
        var versionString = $"{version.Major}.{version.Minor}.{version.Revision}";
        if (version.Build > 0)
            versionString += $".{version.Build}";
        return versionString;
    }

    public override void Initialize()
    {
        SnapX = new SnapXAvalonia();
        // SnapX.setQualifier(" UI");
        AvaloniaXamlLoader.Load(this);
        AppDomain.CurrentDomain.UnhandledException += (Sender, Args) =>
        {
            ShowErrorDialog(Lang.UnhandledException, Args.ExceptionObject as Exception);
        };
#if DEBUG
        Current.AttachDevTools();
#endif

        // Default logic doesn't auto-detect windows theme anymore in designer
        if (Design.IsDesignMode)
        {
            RequestedThemeVariant = ThemeVariant.Dark;
        }
    }

    private void ShowErrorDialog(string title, Exception ex)
    {
        // Create the dialog content with a button
        var stackPanel = new StackPanel
        {
            Orientation = Orientation.Vertical,
            Spacing = 3
        };
        var buttonPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 5,
            HorizontalAlignment = HorizontalAlignment.Right
        };

        stackPanel.Children.Add(new SelectableTextBlock
        {
            Text = ex.GetType() + ": " + ex.Message,
            FontWeight = FontWeight.Bold
            // Padding = new Thickness(10)
        });
        stackPanel.Children.Add(new SelectableTextBlock
        {
            Text = ex.StackTrace,
            FontWeight = FontWeight.SemiLight
            // Padding = new Thickness(10),
        });
        var innerException = ex.InnerException;
        if (innerException != null)
        {
            stackPanel.Children.Add(new SelectableTextBlock
            {
                Text = innerException.GetType() + ": " + innerException.Message,
                FontWeight = FontWeight.Bold
                // Padding = new Thickness(10)
            });
            stackPanel.Children.Add(new SelectableTextBlock
            {
                Text = innerException.StackTrace,
                FontWeight = FontWeight.SemiLight
                // Padding = new Thickness(10),
            });
        }

        stackPanel.Children.Add(new SelectableTextBlock
        {
            Text = GetType().Assembly.GetName().Name + ": " + GetType().Assembly.GetName().Version,
            FontWeight = FontWeight.SemiLight,
            FontSize = 16,
            FontFamily = new FontFamily("Consolas"),
            // Padding = new Thickness(10),
            HorizontalAlignment = HorizontalAlignment.Left
        });

        var reportButton = new Button
        {
            Content = Lang.ReportErrorToDeveloper,
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(0, 5, 0, 0),
            Background = Brushes.DodgerBlue,
            Foreground = Brushes.White,
            BorderBrush = Brushes.DodgerBlue,
            BorderThickness = new Thickness(1),
            Padding = new Thickness(10),
            FontWeight = FontWeight.Bold,
            CornerRadius = new CornerRadius(5)
        };
        reportButton.Click += (sender, e) => OnReportErrorClicked();

        var githubButton = new Button
        {
            Content = Lang.CreateGitHubIssue,
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(0, 5, 0, 0),
            Background = Brushes.Green,
            // Foreground = Brushes.White,
            // BorderBrush = Brushes.IndianRed,
            BorderThickness = new Thickness(1),
            FontSize = 16,
            Padding = new Thickness(10),
            FontWeight = FontWeight.Bold,
            CornerRadius = new CornerRadius(5)
        };
        githubButton.Click += (sender, e) => OnGitHubButtonClicked(ex);


        var copyButton = new Button
        {
            Content = Lang.CopyErrorToClipboard,
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(0, 5, 0, 0),
            // Background = Brushes.Green,
            // Foreground = Brushes.White,
            // BorderBrush = Brushes.Green,
            Background = Brushes.SlateGray,
            BorderThickness = new Thickness(1),
            Padding = new Thickness(10),
            FontWeight = FontWeight.Bold,
            CornerRadius = new CornerRadius(5)
        };

        // Copy error to clipboard when clicked
        copyButton.Click += (sender, e) => CopyErrorToClipboard(ex.ToString());


        buttonPanel.Children.Add(reportButton);
        buttonPanel.Children.Add(githubButton);
        buttonPanel.Children.Add(copyButton);
        stackPanel.Children.Add(buttonPanel);

        // Create and show the error dialog with the formatted message
        var dialog = new AppWindow
        {
            Title = title,
            Content = stackPanel,
            SizeToContent = SizeToContent.WidthAndHeight,
            MinWidth = 400,
            MaxWidth = 1920,
            Padding = new Thickness(6)
            // Background = new ImageBrush()
            // {
            //     Source = new Bitmap(Assembly.GetExecutingAssembly().GetManifestResourceStream("SnapX.Avalonia.SnapX_Logo.png")!),
            //     Stretch = Stretch.UniformToFill
            // }
        };

        dialog.Show();
    }

    private void OnGitHubButtonClicked(Exception ex)
    {
        var newIssueURL = Helpers.GitHubIssueReport(ex);
        if (newIssueURL == null) return;
        URLHelpers.OpenURL(newIssueURL);
    }

    private void CopyErrorToClipboard(string errorMessage)
    {
        Clipboard.CopyText(errorMessage);
    }

    private void OnReportErrorClicked()
    {
        // For now, do nothing when the button is clicked
        // This is where Sentry comes in
        Console.WriteLine("Report Error button clicked. No action yet.");
    }

    private void Shutdown()
    {
        try
        {
            SnapX?.shutdown();
        }
        catch (Exception e)
        {
            Console.Error.WriteLine(e);
            Console.Error.WriteLine($"Error shutting down SnapX.Core, continuing shut down.");
        }
        Environment.Exit(0);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        var locator = new ViewLocator();
        DataTemplates.Add(locator);
        var services = new ServiceCollection();
        ConfigureServices(services);

        var provider = services.BuildServiceProvider();

        Ioc.Default.ConfigureServices(provider);
        Ioc.Default.AddStaticLogging();
        var vm = Ioc.Default.GetRequiredService<MainViewModel>();

        switch (ApplicationLifetime)
        {
            case IClassicDesktopStyleApplicationLifetime desktop:
                {
                    var sigintReceived = false;
                    desktop.ShutdownMode = ShutdownMode.OnExplicitShutdown;
                    desktop.ShutdownRequested += (_, _) =>
                    {
                        DebugHelper.WriteLine("Received Shutdown from Avalonia");
                        if (sigintReceived) return;
                        sigintReceived = true;
                        SnapX.shutdown();

                        // desktop.Shutdown();
                    };

                    Console.CancelKeyPress += (_, ea) =>
                    {
                        DebugHelper.WriteLine("Received SIGINT (Ctrl+C)");
                        if (sigintReceived) return;
                        ea.Cancel = true;
                        sigintReceived = true;
                        SnapX.shutdown();
                        desktop.Shutdown();
                    };
                    // AppDomain.CurrentDomain.ProcessExit += (o, _) =>
                    // {
                    //     if (!sigintReceived)
                    //     {
                    //         sigintReceived = true;
                    //         DebugHelper.WriteLine("Received SIGTERM");
                    //         SnapX.shutdown();
                    //     }
                    //     else
                    //     {
                    //         DebugHelper.WriteLine("Received SIGTERM, ignoring it because already processed SIGINT");
                    //     }
                    // };
                    var errorStarting = false;
                    // DebugHelper.Logger.Debug($"Avalonia Args: {desktop.Args}");
                    try
                    {
                        Task.Run(() => SnapX.start(desktop.Args ?? [])).GetAwaiter().GetResult();
                        var CLIManager = SnapX.GetCLIManager();
                        Task.Run(() => CLIManager.UseCommandLineArgs().GetAwaiter().GetResult()).ConfigureAwait(false)
                            .GetAwaiter().GetResult();
                    }
                    catch (Exception ex)
                    {
                        errorStarting = true;
                        DebugHelper.WriteException(ex);
                        ShowErrorDialog(Lang.SnapXFailedToStart, ex);
                    }

                    if (errorStarting) return;
                    DebugHelper.WriteLine("Internal Startup time: {0} ms", SnapX.getStartupTime());

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

                    if (SnapX.isSilent()) return;
                    if (SnapX.GetCLIManager().IsCommandExist("video"))
                    {
                        var vlc = new LibVLC(false);
                        DebugHelper.WriteLine($"VLC Version: {vlc.Version}");
                        var MediaPlayer = new MediaPlayer(vlc);
                        var input = new Uri("https://ftp.nluug.nl/pub/graphics/blender/demo/movies/ToS/ToS-4k-1920.mov");

                        var media = new Media(vlc, input);
                        MediaPlayer.EnableHardwareDecoding = true;
                        MediaPlayer.Play(media);
                        MediaPlayer.Stopped += async (Sender, Args) =>
                        {
                            media.Dispose();
                            vlc.Dispose();
                        };
                    }

                    var Window = new MainWindow(vm);

                    Window.Show();
                    DebugHelper.WriteLine("MainWindow startup time: {0} ms", SnapX.getStartupTime());

                    MyMainWindow = Window;
                    desktop.MainWindow = Window;
                    break;
                }
            case ISingleViewApplicationLifetime singleView when SnapX.isSilent():
                return;
            case ISingleViewApplicationLifetime singleView:
                {
                    var mv = new MainWindow(vm);
                    mv.Show();
                    MyMainWindow = mv;
                    singleView.MainView = mv;
                    break;
                }
        }
    }

    public static void CreateAboutWindowStatic()
    {
        var aboutWindow = Design.IsDesignMode
            ? Activator.CreateInstance<AboutWindow>()
            : Ioc.Default.GetService<AboutWindow>();
        if (aboutWindow is null)
        {
            DebugHelper.WriteLine("Failed to create about window, got null back from IoC");
            return;
        }

        if (MyMainWindow is not null && MyMainWindow.IsVisible) aboutWindow.Show(MyMainWindow);
        else
        {
            aboutWindow.ShowAsDialog = false;
            aboutWindow.Show();
        }
    }


    private void NativeMenuAboutSnapXClick(object? Sender, EventArgs E)
    {
        CreateAboutWindowStatic();
    }

    public static void ConfigureServices(IServiceCollection services)
    {
        services.AddLogging(loggingBuilder =>
            loggingBuilder.AddSerilog(dispose: true));


        services.AddTransient<MainViewModel>();
        services.AddSingleton<MainWindow>();
        services.AddTransient<AboutWindow>();

        services.AddTransient<HomePageView>();
        services.AddTransient<HomePageViewModel>();

        services.AddSingleton<IMessenger>(WeakReferenceMessenger.Default);
    }

    private void NativeMenuItem_Quit_OnClick(object? Sender, EventArgs E)
    {
        Shutdown();
    }

    private void NativeMenuItem_SnapX_OnClick(object? Sender, EventArgs E)
    {
        NativeMenuAboutSnapXClick(Sender, E);
    }

    private void NativeMenuItem_Capture_Fullscreen_OnClick(object? Sender, EventArgs E)
    {
        TaskHelpers.GetScreenshot().CaptureFullscreen();
    }

    private void NativeMenuItem_Workflows_CaptureActiveScreen_OnClick(object? Sender, EventArgs E)
    {
        new CaptureActiveMonitor().Capture(TaskSettings.GetDefaultTaskSettings());
    }

    private void NativeMenuItem_Workflows_CaptureActiveWindow_OnClick(object? Sender, EventArgs E)
    {
        new CaptureActiveWindow().Capture(TaskSettings.GetDefaultTaskSettings());
    }

    private void NativeMenuItem_Open_OnClick(object? Sender, EventArgs E)
    {
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
        DebugHelper.WriteLine("NativeMenu_OnNeedsUpdate");
    }

    private void NativeMenu_OnOpening(object? Sender, EventArgs E)
    {
        DebugHelper.WriteLine("NativeMenu_OnOpening");
    }
}
