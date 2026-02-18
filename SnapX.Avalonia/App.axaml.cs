using System.Reflection;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Layout;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Styling;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using FluentAvalonia.UI.Controls;
using FluentAvalonia.UI.Windowing;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using SnapX.Avalonia.ViewModels;
// using SnapX.Avalonia.ViewModels.Settings;
using SnapX.Avalonia.Views;
using SnapX.Avalonia.Views.Settings;
using SnapX.Avalonia.Views.Settings.Views;
using SnapX.Core;
using SnapX.Core.Capture;
using SnapX.Core.Job;
using SnapX.Core.Upload;
using SnapX.Core.Utils;
using SnapX.Core.Utils.Extensions;
using SnapX.Core.Utils.Native;

namespace SnapX.Avalonia;

public partial class App : Application
{
    public App()
    {
        DataContext = this;
    }

    public static SnapXAvalonia SnapX { get; private set; } = null!;
    public static MainWindow? MyMainWindow { get; private set; }

    // There is no limit of what chaos could occur if two settings windows exist.
    // We must keep track of it.
    public static SettingsWindow? MySettingsWindow { get; set; }
    public static string TrayTitle => $"SnapX v{SimpleVersion()}";

    private static Lock _windowLock = new();

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

    private void ShowErrorDialog(string? title, Exception ex)
    {
        var stackPanel = new StackPanel { Orientation = Orientation.Vertical, Spacing = 3 };
        var buttonPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 5,
            HorizontalAlignment = HorizontalAlignment.Right
        };

        stackPanel.Children.Add(
            new SelectableTextBlock
            {
                Text = ex.GetType() + ": " + ex.Message,
                FontWeight = FontWeight.Bold
                // Padding = new Thickness(10)
            }
        );
        stackPanel.Children.Add(
            new SelectableTextBlock
            {
                Text = ex.StackTrace,
                FontWeight = FontWeight.SemiLight
                // Padding = new Thickness(10),
            }
        );
        var innerException = ex.InnerException;
        if (innerException != null)
        {
            stackPanel.Children.Add(
                new SelectableTextBlock
                {
                    Text = innerException.GetType() + ": " + innerException.Message,
                    FontWeight = FontWeight.Bold
                    // Padding = new Thickness(10)
                }
            );
            stackPanel.Children.Add(
                new SelectableTextBlock
                {
                    Text = innerException.StackTrace,
                    FontWeight = FontWeight.SemiLight
                    // Padding = new Thickness(10),
                }
            );
        }

        var version = Assembly.GetExecutingAssembly().GetName().Version;
        var semver = version.Major + "." + version.Minor + "." + version.Revision;
        stackPanel.Children.Add(
            new SelectableTextBlock
            {
                Text = GetType().Assembly.GetName().Name + ": " + semver,
                FontWeight = FontWeight.SemiLight,
                FontSize = 16,
                FontFamily = new FontFamily("Consolas"),
                // Padding = new Thickness(10),
                HorizontalAlignment = HorizontalAlignment.Left
            }
        );

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
        reportButton.Click += (sender, e) => OnReportErrorClicked(reportButton, ex);

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

        copyButton.Click += (sender, e) => CopyErrorToClipboard(copyButton, ex.ToString());

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
        if (newIssueURL == null)
            return;
        URLHelpers.OpenURL(newIssueURL);
    }

    private void CopyErrorToClipboard(Control Sender, string? errorMessage)
    {
        var topLevel = TopLevel.GetTopLevel(Sender);
        if (topLevel is null)
        {
            DebugHelper.WriteLine("TopLevel is null");
            return;
        }

        topLevel.Clipboard?.SetTextAsync(errorMessage);
    }

    private async void OnReportErrorClicked(Button button, Exception ex)
    {
        var originalButtonContent = CreateContentCopy(button.Content!);

        try
        {
            if (!FeatureFlags.DisableTelemetry && Core.SnapXL.TelemetryHandler is null)
            {
                Core.SnapXL.InitTelemetryServices();
                SentrySdk.CaptureException(ex);

                DebugHelper.WriteLine("Error reported to Sentry successfully.");
            }
            else
            {
                DebugHelper.WriteLine(
                    "Error has likely already been sent to Sentry as telemetry is not disabled! :heart:"
                );
            }

            button.Content = "✓ Reported";
            button.IsEnabled = false;

            await Task.Delay(TimeSpan.FromSeconds(3));
        }
        catch (Exception taskEx)
        {
            DebugHelper.WriteLine($"Error during exception reporting: {taskEx.Message}");
        }
        finally
        {
            button.Content = originalButtonContent;
        }
    }

    private object CreateContentCopy(object content)
    {
        if (content == null)
            return null;

        return content switch
        {
            string str => new string(str.ToCharArray()),
            ICloneable cloneable => cloneable.Clone(),
            _ => content // For other types, we have to hope they're immutable
        };
    }

    private void Shutdown()
    {
        try
        {
            if (SnapX != null)
            {
                _pollingCts?.Cancel();
                var shutdownTask = Task.Run(() => SnapX.shutdown());

                if (!shutdownTask.Wait(TimeSpan.FromSeconds(10)))
                {
                    Console.Error.WriteLine(
                        "SnapX shutdown timed out after 10 seconds, continuing exit."
                    );
                }
            }
        }
        catch (Exception e)
        {
            Console.Error.WriteLine(e);
            Console.Error.WriteLine("Error shutting down SnapX.Core, continuing shut down.");
        }

        MyMainWindow?.Close();
        MyMainWindow = null;

        Environment.Exit(0);
    }

    public void ListenForEvents()
    {
        Core.SnapXL.EventAggregator.Subscribe<NeedClipboardCopyEvent>(HandleClipboardCopyEvent);
        Core.SnapXL.EventAggregator.Subscribe<ErrorMessageEvent>(HandleErrorMessageEvent);
    }

    private async void HandleClipboardCopyEvent(NeedClipboardCopyEvent @event)
    {
        try
        {
            DebugHelper.WriteLine("HandleClipboardCopyEvent called");
            var clipboard = await GetClipboardAsync();

            var dataObject = new DataObject();

            if (@event.HasText)
            {
                dataObject.Set(DataFormats.Text, @event.Text!);
            }

            if (@event.HasImage)
            {
                dataObject.Set(
                    DataFormat.Bitmap.Identifier,
                    SnapX.ConvertImageSharpImgToAvalonia(@event.Image!)
                );
            }

            foreach (var format in @event.AdditionalFormats)
            {
                if (format.Value is string strValue)
                {
                    dataObject.Set(format.Key, strValue);
                }
                else if (format.Value is IImage imageValue)
                {
                    dataObject.Set(format.Key, imageValue);
                }
                else
                {
                    dataObject.Set(format.Key, format.Value.ToString());
                }
            }

            if (@event.CustomData != null)
            {
                if (@event.CustomData is string customString)
                {
                    dataObject.Set("CustomData", customString);
                }
                else
                {
                    var json = JsonHelpers.SerializeToString(@event.CustomData);
                    dataObject.Set("CustomData", json);
                }
            }

            await clipboard.SetDataObjectAsync(dataObject);
            @event.MarkAsHandled();
        }
        catch (Exception ex)
        {
            // Silent fail - core will handle fallback
        }
    }

    private async void HandleErrorMessageEvent(ErrorMessageEvent @event)
    {
        await Dispatcher.UIThread.InvokeAsync(async () =>
        {
            try
            {
                var textBlock = new SelectableTextBlock
                {
                    Text = @event.FullError
                        ? @event.Exception.ToString()
                        : @event.Exception.Message,
                    TextWrapping = TextWrapping.Wrap,
                    MaxWidth = 600
                };

                var scrollViewer = new ScrollViewer
                {
                    Content = textBlock,
                    MaxHeight = 400,
                    HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled
                };

                var dialog = new ContentDialog
                {
                    Title = $"Error in {@event.Context}",
                    Content = scrollViewer,
                    CloseButtonText = "Close",
                    DefaultButton = ContentDialogButton.Close,
                    PrimaryButtonText = @event.FullError ? "Copy" : null
                };
                TaskHelpers.PlayNotificationSoundAsync(NotificationSound.Error);
                var result = await dialog.ShowAsync();

                if (result == ContentDialogResult.Primary)
                {
                    var topLevel = TopLevel.GetTopLevel(
                        MyMainWindow is not null ? MyMainWindow : dialog
                    );
                    await topLevel?.Clipboard?.SetTextAsync(@event.Exception.ToString());
                }
            }
            catch (Exception ex)
            {
                // Fallback to console if the UI is in a state where dialogs can't open
                DebugHelper.Logger?.Error("Critical: Could not open FluentAvalonia ContentDialog.");
                DebugHelper.Logger?.Error(ex.ToString());
            }
        });
    }

    private static async Task<IClipboard> GetClipboardAsync()
    {
        if (Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var window =
                desktop.Windows.FirstOrDefault(w => w.IsActive) ?? desktop.Windows.FirstOrDefault();
            if (window != null)
            {
                return window.Clipboard;
            }
        }

        return await GetOrCreateClipboardWindowAsync();
    }

    private static async Task<IClipboard> GetOrCreateClipboardWindowAsync()
    {
        lock (_windowLock)
        {
            if (!MyMainWindow?.IsVisible ?? false)
            {
                DebugHelper.WriteLine("Creating persistent clipboard window");
                var openedTcs = new TaskCompletionSource<bool>();

                MyMainWindow.Opened += (s, e) =>
                {
                    DebugHelper.WriteLine($"{nameof(MyMainWindow)}: opened");
                    openedTcs.TrySetResult(true);
                };

                Task.Run(async () =>
                {
                    try
                    {
                        var openedTask = openedTcs.Task;
                        var timeoutTask = Task.Delay(500);

                        var completedTask = await Task.WhenAny(openedTask, timeoutTask);

                        if (completedTask == openedTask)
                        {
                            DebugHelper.WriteLine("Window opened successfully");
                        }
                        else
                        {
                            DebugHelper.WriteLine("Window opened timed out, but continuing");
                            openedTcs.TrySetResult(false);
                        }
                    }
                    catch (Exception ex)
                    {
                        DebugHelper.WriteLine($"Error waiting for window open: {ex.Message}");
                    }
                });
                MyMainWindow.Show();
            }

            return MyMainWindow.Clipboard;
        }
    }
    CancellationTokenSource? _pollingCts = null;

    // ReSharper disable once AsyncVoidMethod
    public override void OnFrameworkInitializationCompleted()
    {
        // Crashes must be contained, AT ALL COSTS!
        Dispatcher.UIThread.UnhandledException += (s, e) =>
        {
            e.Handled = true;
            var ex = e.Exception;

            ex.ShowError(true, "UI Dispatcher Critical Error");
        };
        TaskScheduler.UnobservedTaskException += (s, e) =>
        {
            e.SetObserved();
            e.Exception.ShowError(true, "Unobserved Task Exception");
        };
        var locator = new ViewLocator();
        DataTemplates.Add(locator);
        var services = new ServiceCollection();
        ConfigureServices(services);

        var provider = services.BuildServiceProvider();

        Ioc.Default.ConfigureServices(provider);
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
                        if (sigintReceived)
                            return;
                        sigintReceived = true;
                        SnapX.shutdown();
                        _pollingCts?.Cancel();

                        // desktop.Shutdown();
                    };

                    Console.CancelKeyPress += (_, ea) =>
                    {
                        DebugHelper.WriteLine("Received SIGINT (Ctrl+C)");
                        if (sigintReceived)
                            return;
                        ea.Cancel = true;
                        sigintReceived = true;
                        SnapX.shutdown();
                        try
                        {
                            desktop.Shutdown();
                        }
                        catch
                        {
                            // Silence at once
                        }
                        _pollingCts?.Cancel();
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
                        SnapX.start(desktop.Args ?? []);
                        var CLIManager = SnapX.GetCLIManager();
                        CLIManager.UseCommandLineArgs().GetAwaiter().GetResult();
                    }
                    catch (Exception ex)
                    {
                        errorStarting = true;
                        DebugHelper.WriteException(ex);
                        ShowErrorDialog(Lang.SnapXFailedToStart, ex);
                    }

                    if (errorStarting)
                        return;
                    ListenForEvents();
                    DebugHelper.WriteLine("Internal Startup time: {0} ms", SnapX.getStartupTime());

                    var logoBitmap = new Bitmap(
                        AssetLoader.Open(new Uri("avares://snapx-ui/SnapX_Logo.png"))
                    );
                    if (SnapX.GetConfiguration().ShowTray)
                    {
                        var trayIcon = new TrayIcon
                        {
                            Icon = new WindowIcon(logoBitmap),
                            ToolTipText = Core.SnapXL.AppName,
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
                        var windowMenu = new NativeMenu();
                        // Never called on Linux!
                        // @see https://github.com/AvaloniaUI/Avalonia/issues/8076
                        windowMenu.NeedsUpdate += (sender, e) => { PopulateWindowMenu(windowMenu); };
                        var windowPicker = new NativeMenuItem(Lang.UI_Dropdown_Window)
                        {
                            Menu = windowMenu
                        };
                        void StartPolling(NativeMenu windowMenu, NativeMenu screenMenu)
                        {
                            DebugHelper.WriteLine("SnapX has started polling for window changes and display changes. This is because Avalonia on Linux/FreeBSD does not support the NeedsUpdate event for the tray menu.");
                            _pollingCts?.Cancel();
                            _pollingCts = new CancellationTokenSource();

                            _ = Task.Run(async () =>
                            {
                                using var timer = new PeriodicTimer(TimeSpan.FromSeconds(1));

                                while (await timer.WaitForNextTickAsync(_pollingCts.Token))
                                {
                                    await Dispatcher.UIThread.InvokeAsync(() =>
                                    {

                                        PopulateWindowMenu(windowMenu);
                                        PopulateMonitorMenu(screenMenu);
                                    }, DispatcherPriority.Background);
                                }
                            }, _pollingCts.Token);
                        }

                        void PopulateWindowMenu(NativeMenu menu)
                        {
                            try
                            {
                                var windows = Methods.GetWindowList();
                                var windowsById = windows.ToDictionary(w => w.Handle, w => w);

                                for (var i = menu.Items.Count - 1; i >= 0; i--)
                                {
                                    var item = (NativeMenuItem)menu.Items[i];
                                    if (item.CommandParameter is not IntPtr handle || !windowsById.ContainsKey(handle))
                                    {
                                        menu.Items.RemoveAt(i);
                                    }
                                }

                                foreach (var window in windows)
                                {
                                    var existingItem = menu.Items.Cast<NativeMenuItem>()
                                        .FirstOrDefault(item => item.CommandParameter is IntPtr handle && handle == window.Handle);

                                    if (existingItem != null)
                                    {
                                        if (existingItem.Header != window.Title) existingItem.Header = window.Title;
                                        continue;
                                    }

                                    var nativeWindowItem = new NativeMenuItem(window.Title)
                                    {
                                        Icon = logoBitmap,
                                        ToolTip = window.ProcessName,
                                        CommandParameter = window.Handle
                                    };

                                    nativeWindowItem.Click += (Sender, EA) =>
                                    {
                                        Task.Run(async () =>
                                        {
                                            var capturedImage = await Methods.CaptureWindow(window).ConfigureAwait(false);
                                            if (capturedImage != null)
                                            {
                                                UploadManager.RunImageTask(
                                                    capturedImage,
                                                    TaskSettings.GetDefaultTaskSettings()
                                                );
                                            }
                                        });
                                    };

                                    windowMenu.Add(nativeWindowItem);
                                }
                            }
                            catch (Exception ex)
                            {
                                ShowErrorDialog(Lang.SnapXFailedToStart, ex);
                            }
                        }

                        capture.Menu.Items.Add(windowPicker);
                        var screens = SnapXResources.graphicsInfo?.Monitors;
                        var screenMenu = new NativeMenu();
                        var monitorPicker = new NativeMenuItem(Lang.UI_Dropdown_Monitor)
                        {
                            Menu = screenMenu
                        };
                        monitorPicker.Menu.NeedsUpdate += (sender, e) => { PopulateMonitorMenu(screenMenu); };

                        void PopulateMonitorMenu(NativeMenu menu)
                        {
                            try
                            {
                                var currentScreens = screens?.Select((s, idx) => (s, idx)).ToList() ?? [];
                                var screensByName = currentScreens.ToDictionary(pair => pair.s.Name, pair => pair);

                                for (var i = menu.Items.Count - 1; i >= 0; i--)
                                {
                                    var item = (NativeMenuItem)menu.Items[i];
                                    if (item.CommandParameter is not string screenName || !screensByName.ContainsKey(screenName))
                                    {
                                        menu.Items.RemoveAt(i);
                                    }
                                }

                                foreach (var (screen, i) in currentScreens)
                                {
                                    var header = $"{i}: {screen.Name} {screen.Resolution} (X: {screen.Position?.X ?? 0}, Y: {screen.Position?.Y ?? 0})";
                                    var existingItem = menu.Items.Cast<NativeMenuItem>()
                                        .FirstOrDefault(item => item.CommandParameter is string name && name == screen.Name);

                                    if (existingItem != null)
                                    {
                                        if (existingItem.Header != header)
                                        {
                                            existingItem.Header = header;
                                        }
                                        continue;
                                    }

                                    var item = new NativeMenuItem(header)
                                    {
                                        CommandParameter = screen.Name
                                    };

                                    item.Click += (s, ev) =>
                                    {
                                        Task.Run(async () =>
                                        {
                                            var capturedImage = await Methods.CaptureScreen(screen.Name).ConfigureAwait(false);

                                            if (capturedImage != null)
                                            {
                                                UploadManager.RunImageTask(
                                                    capturedImage,
                                                    TaskSettings.GetDefaultTaskSettings()
                                                );
                                            }
                                        });
                                    };

                                    menu.Items.Add(item);
                                }
                            }
                            catch (Exception ex)
                            {
                                ShowErrorDialog(Lang.SnapXFailedToStart, ex);
                            }
                        }

                        PopulateWindowMenu(windowMenu);
                        PopulateMonitorMenu(screenMenu);
                        if (OperatingSystem.IsLinux() || OperatingSystem.IsFreeBSD())
                            StartPolling(windowMenu, screenMenu);
                        capture.Menu.Items.Add(monitorPicker);
                        var regionCaptureMenuItem = new NativeMenuItem(Lang.UI_Dropdown_Region);
                        regionCaptureMenuItem.Click += (_, _) => { new RegionSelectorWindow().Show(); };
                        capture.Menu.Items.Add(regionCaptureMenuItem);
                        // capture.Menu.Items.Add(new NativeMenuItem("Region (Light)"));
                        // capture.Menu.Items.Add(new NativeMenuItem("Region (Transparent)"));
                        menu.Items.Add(capture);
                        var uploadFile = new NativeMenuItem("Upload File...");
                        uploadFile.Click += (_, _) =>
                        {
                            Core.SnapXL.EventAggregator.Publish(
                                new NeedFileOpenerEvent
                                {
                                    Title = "SnapX | Upload File",
                                    Multiselect = true
                                }
                            );
                        };
                        var uploadFolder = new NativeMenuItem("Upload Folder...");
                        uploadFolder.Click += (_, _) =>
                        {
                            Core.SnapXL.EventAggregator.Publish(
                                new NeedFileOpenerEvent
                                {
                                    Title = "SnapX | Upload Folder",
                                    Multiselect = false,
                                    FolderPicker = true
                                }
                            );
                        };
                        var uploadText = new NativeMenuItem("Upload text...");
                        uploadText.Click += (_, _) =>
                        {
                            var textBoxWindow = new Window();
                            textBoxWindow.Title = "SnapX | Upload Text";
                            var stackPanel = new StackPanel();
                            stackPanel.Margin = new Thickness(10);
                            textBoxWindow.Content = stackPanel;
                            var textBox = new TextBox();
                            textBox.MaxWidth = 450;
                            textBox.TextWrapping = TextWrapping.Wrap;
                            textBox.MinHeight = 150;
                            stackPanel.Children.Add(textBox);
                            var uploadButton = new Button();
                            uploadButton.Content = "Upload";
                            uploadButton.VerticalAlignment = VerticalAlignment.Bottom;

                            uploadButton.Click += (_, _) =>
                            {
                                UploadManager.UploadText(textBox.Text);
                                textBoxWindow.Close();
                            };
                            stackPanel.Children.Add(uploadButton);
                            var cancelButton = new Button();
                            cancelButton.Content = "Cancel";
                            cancelButton.VerticalAlignment = VerticalAlignment.Bottom;
                            cancelButton.Click += (_, _) => textBoxWindow.Close();
                            stackPanel.Children.Add(cancelButton);

                            textBoxWindow.Width = 500;
                            textBoxWindow.Height = 800;
                            textBoxWindow.Show();
                        };
                        // new NativeMenuItem("Upload from clipboard..."),
                        var shortenURL = new NativeMenuItem("Shorten URL...");
                        menu.Items.Add(
                            new NativeMenuItem("Upload")
                            {
                                Menu = new NativeMenu
                                {
                                uploadFile,
                                uploadFolder,
                                uploadText,
                                shortenURL
                                }
                            }
                        );
                        var captureFullscreenMenuItem = new NativeMenuItem(Lang.UI_Capture_Fullscreen);
                        captureFullscreenMenuItem.Click += NativeMenuItem_Capture_Fullscreen_OnClick;
                        var captureActiveWindowMenuItem = new NativeMenuItem("Capture active window");
                        captureActiveWindowMenuItem.Click +=
                            NativeMenuItem_Workflows_CaptureActiveWindow_OnClick;
                        var captureActiveScreenMenuItem = new NativeMenuItem("Capture active screen");
                        captureActiveScreenMenuItem.Click +=
                            NativeMenuItem_Workflows_CaptureActiveScreen_OnClick;
                        var workflows = new NativeMenuItem("Workflows")
                        {
                            Menu =
                            [
                                captureFullscreenMenuItem,
                            captureActiveScreenMenuItem,
                            captureActiveWindowMenuItem
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

                    if (SnapX.isSilent())
                        return;
                    if (SnapX.GetCLIManager().IsCommandExist("video"))
                    {
                        throw new NotImplementedException("LibVLC is removed from SnapX.Avalonia");
                    }

                    var Window = new MainWindow(vm);

                    Window.Show();
                    DebugHelper.WriteLine("MainWindow startup time: {0} ms", SnapX.getStartupTime());

                    MyMainWindow = Window;
                    desktop.MainWindow = Window;
                    // MyMainWindow.Closed += (_, _) =>
                    // {
                    //     MyMainWindow = null;
                    // };
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

    public static void CreateOrOpenSettingsWindowStatic()
    {
        if (MySettingsWindow is null)
        {
            var settingsWindow = Design.IsDesignMode
                ? Activator.CreateInstance<SettingsWindow>()
                : Ioc.Default.GetService<SettingsWindow>();
            if (settingsWindow is null)
            {
                DebugHelper.WriteLine("Failed to create about window, got null back from IoC");
                return;
            }

            MySettingsWindow = settingsWindow;
            settingsWindow.Closed += (_, _) => MySettingsWindow = null;
        }

        if (MyMainWindow is not null && MyMainWindow.IsVisible)
        {
            MySettingsWindow.Show(MyMainWindow);
            MySettingsWindow.Focus();
            MySettingsWindow.Activate();
        }
        else
        {
            MySettingsWindow.ShowAsDialog = false;
            MySettingsWindow.Show();
            MySettingsWindow.Focus();
            MySettingsWindow.Activate();
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

        if (MyMainWindow is not null && MyMainWindow.IsVisible)
        {
            aboutWindow.Show(MyMainWindow);
            aboutWindow.Focus();
            aboutWindow.Activate();
        }
        else
        {
            aboutWindow.ShowAsDialog = false;
            aboutWindow.Show();
            aboutWindow.Focus();
            aboutWindow.Activate();
        }
    }

    private void NativeMenuAboutSnapXClick(object? Sender, EventArgs E)
    {
        CreateAboutWindowStatic();
    }

    public static void ConfigureServices(IServiceCollection services)
    {
        services.AddLogging(loggingBuilder => loggingBuilder.AddSerilog(dispose: true));

        services.AddTransient<MainViewModel>();
        services.AddTransient<MainWindow>();
        services.AddTransient<SettingsWindow>();
        services.AddTransient<SettingsMainView>();
        services.AddTransient<SettingsMainViewVM>();
        services.AddTransient<CustomUploaderView>();
        services.AddSingleton<CustomUploaderVM>();
        services.AddSingleton<ImportExportVM>();
        services.AddTransient<ImportExportView>();
        // services.AddSingleton<ScreenRecordOptionsVM>();
        // services.AddTransient<ScreenRecordOptionsView>();
        services.AddSingleton<CoreUploaderVM>();
        services.AddTransient<BuiltInUploaderSettingsView>();
        services.AddSingleton<DatabaseVM>();
        services.AddSingleton<SqliteConnection>(sp => SnapX.GetDB());
        services.AddTransient<DatabaseView>();
        services.AddTransient<SettingsHomePageView>();
        services.AddSingleton<SettingsHomePageViewVM>();

        services.AddTransient<AboutWindow>();
        services.AddSingleton<AboutWindowViewModel>();

        services.AddTransient<HomePageView>();
        services.AddSingleton<HomePageViewModel>();
        services.AddTransient<NotImplemented>();
        services.AddSingleton<NotImplementedVM>();


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

    private async void NativeMenuItem_Capture_Fullscreen_OnClick(object? Sender, EventArgs E)
    {
        await Task.Factory.StartNew(
            () => { new CaptureFullscreen().Capture(); },
            CancellationToken.None,
            TaskCreationOptions.LongRunning,
            TaskScheduler.Default
        );
    }

    private async void NativeMenuItem_Workflows_CaptureActiveScreen_OnClick(
        object? Sender,
        EventArgs E
    )
    {
        await Task.Factory.StartNew(
            () => { new CaptureActiveMonitor().Capture(); },
            CancellationToken.None,
            TaskCreationOptions.LongRunning,
            TaskScheduler.Default
        );
    }

    private async void NativeMenuItem_Workflows_CaptureActiveWindow_OnClick(
        object? Sender,
        EventArgs E
    )
    {
        await Task.Factory.StartNew(
            () => { new CaptureActiveWindow().Capture(); },
            CancellationToken.None,
            TaskCreationOptions.LongRunning,
            TaskScheduler.Default
        );
    }

    private void NativeMenuItem_Open_OnClick(object? Sender, EventArgs E)
    {
        if (MyMainWindow is null || !MyMainWindow.IsLoaded)
        {
            var mainWindow = Design.IsDesignMode
                ? Activator.CreateInstance<MainWindow>()
                : Ioc.Default.GetService<MainWindow>();
            if (mainWindow is null)
            {
                DebugHelper.WriteLine("Failed to create main window, got null back from IoC");
                return;
            }

            MyMainWindow = mainWindow;
            MyMainWindow.Show();
        }

        if (!MyMainWindow?.IsVisible ?? true)
            MyMainWindow?.Show();
        MyMainWindow?.Focus();
        MyMainWindow?.Activate();
        if (MyMainWindow != null)
        {
            // MyMainWindow.Closed += (_, _) => MyMainWindow = null;
        }
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
