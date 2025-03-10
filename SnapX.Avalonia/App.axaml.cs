using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Layout;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Styling;
using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Messaging;
using FluentAvalonia.UI.Windowing;
using LibVLCSharp.Shared;
using Microsoft.Extensions.DependencyInjection;
using SnapX.Avalonia.ViewModels;
using SnapX.Avalonia.Views;
using SnapX.Core;
using SnapX.Core.Utils;
using SnapX.Core.Utils.Native;
#if DEBUG
using HotAvalonia;
#endif

namespace SnapX.Avalonia;

public class App : Application
{
    public static SnapXAvalonia SnapX { get; set; }
    public override void Initialize()
    {

        SnapX = new SnapXAvalonia();
        // SnapX.setQualifier(" UI");
		#if DEBUG
		this.EnableHotReload();
		#endif
        AvaloniaXamlLoader.Load(this);
        AppDomain.CurrentDomain.UnhandledException += (Sender, Args) =>
        {
            ShowErrorDialog(Lang.UnhandledException, Args.ExceptionObject as Exception);
        };
        // for macOS
        Current!.Name = Core.SnapX.AppName;
#if DEBUG
        Current.AttachDevTools();
#endif

        // Default logic doesn't auto-detect windows theme anymore in designer
        // to stop light mode, force here
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
            HorizontalAlignment = HorizontalAlignment.Right,


        };

        stackPanel.Children.Add(new SelectableTextBlock
        {
            Text = ex.GetType() + ": " + ex.Message,
            FontWeight = FontWeight.Bold,
            // Padding = new Thickness(10)
        });
        stackPanel.Children.Add(new SelectableTextBlock
        {
            Text = ex.StackTrace,
            FontWeight = FontWeight.SemiLight,
            // Padding = new Thickness(10),
        });
        var innerException = ex.InnerException;
        if (innerException != null)
        {
            stackPanel.Children.Add(new SelectableTextBlock
            {
                Text = innerException.GetType() + ": " + innerException.Message,
                FontWeight = FontWeight.Bold,
                // Padding = new Thickness(10)
            });
            stackPanel.Children.Add(new SelectableTextBlock
            {
                Text = innerException.StackTrace,
                FontWeight = FontWeight.SemiLight,
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
            HorizontalAlignment = HorizontalAlignment.Left,
        });

        var reportButton = new Button
        {
            Content = Lang.ReportErrorToSentry,
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
            Padding = new Thickness(6),
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

    private void CopyErrorToClipboard(string errorMessage) => Clipboard.CopyText(errorMessage);

    private void OnReportErrorClicked()
    {
        // For now, do nothing when the button is clicked
        // This is where Sentry comes in
        Console.WriteLine("Report Error button clicked. No action yet.");
    }

    public override void OnFrameworkInitializationCompleted()
    {
        var locator = new ViewLocator();
        DataTemplates.Add(locator);
        var services = new ServiceCollection();
        ConfigureServices(services);

        var provider = services.BuildServiceProvider();

        Ioc.Default.ConfigureServices(provider);
        var vm = Ioc.Default.GetRequiredService<MainViewModel>();

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var sigintReceived = false;
            desktop.ShutdownRequested += (_, _) =>
            {
                sigintReceived = true;
                DebugHelper.WriteLine("Received Shutdown from Avalonia");
                SnapX.shutdown();
                desktop.Shutdown();
            };

            Console.CancelKeyPress += (_, ea) =>
            {
                ea.Cancel = true;
                sigintReceived = true;

                DebugHelper.WriteLine("Received SIGINT (Ctrl+C)");
                SnapX.shutdown();
                desktop.Shutdown();
            };
            AppDomain.CurrentDomain.ProcessExit += (_, _) =>
            {
                if (!sigintReceived)
                {
                    DebugHelper.WriteLine("Received SIGTERM");
                    SnapX.shutdown();
                    desktop.Shutdown();
                }
                else
                {
                    DebugHelper.WriteLine("Received SIGTERM, ignoring it because already processed SIGINT");
                }
            };
            var errorStarting = false;
            // DebugHelper.Logger.Debug($"Avalonia Args: {desktop.Args}");
            try
            {
                Task.Run(() => SnapX.start(desktop.Args ?? [])).GetAwaiter().GetResult();
                var CLIManager = SnapX.GetCLIManager();
                Task.Run(() => CLIManager.UseCommandLineArgs().GetAwaiter().GetResult()).ConfigureAwait(false).GetAwaiter().GetResult();
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
                var vlc = new LibVLC(enableDebugLogs: false);
                DebugHelper.WriteLine($"VLC Version: {vlc.Version}");
                var MediaPlayer = new MediaPlayer(vlc);
                var input = new Uri("https://ftp.nluug.nl/pub/graphics/blender/demo/movies/ToS/ToS-4k-1920.mov");

                var media = new Media(vlc, input);
                MediaPlayer.EnableHardwareDecoding = true;
                MediaPlayer.Play(media);
                MediaPlayer.Stopped += async(Sender, Args) =>
                {
                    media.Dispose();
                    vlc.Dispose();
                };
            }
            var Window = new MainWindow(vm);

            Window.Show();
            desktop.MainWindow = Window;

        }
        else if (ApplicationLifetime is ISingleViewApplicationLifetime singleView)
        {
            if (SnapX.isSilent()) return;
            var mv = new MainWindow();
            mv.Show();
            singleView.MainView = mv;
        }
    }

    private void NativeMenuAboutSnapXClick(object? Sender, EventArgs E) => new AboutWindow().Show();

    public static void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton<MainViewModel>();
        services.AddTransient<HomePageViewModel>();

        services.AddTransient<MainWindow>();
        services.AddTransient<AboutWindow>();
        services.AddTransient<HomePageView>();
        services.AddSingleton<IMessenger>(WeakReferenceMessenger.Default);

    }

}
