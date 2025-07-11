using System.Reflection;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Layout;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Styling;
using CommunityToolkit.Mvvm.DependencyInjection;
using FluentAvalonia.UI.Windowing;
using SnapX.Avalonia.ViewModels;
using SnapX.Avalonia.Views;
using SnapX.Avalonia.Views.About;
using SnapX.Avalonia.Views.Settings;
using SnapX.Core;
using SnapX.Core.Interfaces;
using SnapX.Core.Utils;
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

    public override void Initialize()
    {
        SnapX = new SnapXAvalonia();
        // SnapX.setQualifier(" UI");
        AvaloniaXamlLoader.Load(this);
        AppDomain.CurrentDomain.UnhandledException += (_, Args) =>
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
        var version = Assembly.GetExecutingAssembly().GetName().Version;
        var semver = version.Major + "." + version.Minor + "." + version.Revision;
        stackPanel.Children.Add(new SelectableTextBlock
        {
            Text = GetType().Assembly.GetName().Name + ": " + semver,
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

    private void CopyErrorToClipboard(string? errorMessage)
    {
        Clipboard.CopyText(errorMessage);
    }

    private void OnReportErrorClicked()
    {
        // For now, do nothing when the button is clicked
        // This is where Sentry comes in
        Console.WriteLine("Report Error button clicked. No action yet. If you have telemetry enabled, (it is by default) it will have already been sent to Sentry.");
    }

    public static void Shutdown()
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
                        Shutdown();

                        // desktop.Shutdown();
                    };

                    Console.CancelKeyPress += (_, ea) =>
                    {
                        DebugHelper.WriteLine("Received SIGINT (Ctrl+C)");
                        if (sigintReceived) return;
                        ea.Cancel = true;
                        sigintReceived = true;
                        Shutdown();
                        desktop.Shutdown();
                    };
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

                    if (SnapX.isSilent()) return;
                    if (SnapX.GetCLIManager().IsCommandExist("video"))
                    {
                        throw new NotImplementedException("LibVLC is removed from SnapX.Avalonia");
                    }
                    var Window = new MainWindow(vm);

                    Window.Show();
                    DebugHelper.WriteLine("MainWindow startup time: {0} ms", SnapX.getStartupTime());

                    MyMainWindow = Window;
                    desktop.MainWindow = Window;
                    var tray = new OSTray(this, Ioc.Default.GetRequiredService<ILoggerService>());
                    tray.display();
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
        var settingsWindow = Design.IsDesignMode
            ? Activator.CreateInstance<SettingsWindow>()
            : Ioc.Default.GetService<SettingsWindow>();
        if (settingsWindow is null)
        {
            DebugHelper.WriteLine("Failed to create about window, got null back from IoC");
            return;
        }
        if (MyMainWindow is not null && MyMainWindow.IsVisible) settingsWindow.Show(MyMainWindow);
        else
        {
            settingsWindow.ShowAsDialog = false;
            settingsWindow.Show();
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
    public void NativeMenuAboutSnapXClick(object? Sender, EventArgs E)
    {
        CreateAboutWindowStatic();
    }
    public static void CreateSettingsWindowStatic()
    {
        var settingsWindow = Design.IsDesignMode
            ? Activator.CreateInstance<SettingsWindow>()
            : Ioc.Default.GetService<SettingsWindow>();
        if (settingsWindow is null)
        {
            DebugHelper.WriteLine("Failed to create about window, got null back from IoC");
            return;
        }

        if (MyMainWindow is not null && MyMainWindow.IsVisible) settingsWindow.Show(MyMainWindow);
        else
        {
            settingsWindow.ShowAsDialog = false;
            settingsWindow.Show();
        }
    }
}
