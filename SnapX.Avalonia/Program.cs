// See https://aka.ms/new-console-template for more information
#pragma warning disable CA1416 // I am aware
using System.Reflection;
using Avalonia;
using SnapX.Avalonia;
#if BROWSER
using Avalonia.Browser;
#else
using Avalonia.Dialogs;
using Avalonia.Media;
#endif

internal static class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        if (args.Length != 0 && (args[0] == "--version" || args[0] == "-v"))
        {
            var informationalVersion = Assembly.GetExecutingAssembly()
                .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
                .InformationalVersion ?? "Unknown";

            Console.WriteLine(informationalVersion);
            return;
        }

        BuildAvaloniaApp()
#if !BROWSER
            .StartWithClassicDesktopLifetime(args);
#else
            .StartBrowserAppAsync("out");
#endif
    }

    // ReSharper disable once MemberCanBePrivate.Global
    public static AppBuilder BuildAvaloniaApp()
    {
        var builder = AppBuilder.Configure<App>();
#if BROWSER
        return builder;
#else
        if (OperatingSystem.IsLinux() || OperatingSystem.IsFreeBSD())
        {
            builder = builder.With(new FontManagerOptions
            {
                FontFallbacks = new List<FontFallback>
                {
                    new() { FontFamily = "Noto Sans" },
                    new() { FontFamily = "Roboto" },
                    new() { FontFamily = "Adwaita Sans" },
                    new() { FontFamily = "Helvetica Neue" },
                    new() { FontFamily = "Open Sans" },
                    new() { FontFamily = "Segoe UI" },
                    new() { FontFamily = "Inter" } // kept for compatibility
                }
            });
        }

        builder = builder.LogToTrace();

        var x11Options = new X11PlatformOptions
        {
            RenderingMode = [X11RenderingMode.Vulkan, X11RenderingMode.Egl, X11RenderingMode.Glx, X11RenderingMode.Software],
            UseRetainedFramebuffer = true,
            OverlayPopups = true
        };

        if (OperatingSystem.IsFreeBSD() || Environment.GetEnvironmentVariable("SNAPX_PRETEND_FREEBSD") is not null)
        {
            builder = builder
                .UseSkia()
                .UseX11()
                .With(x11Options);
        }
        else
        {
            builder = builder
                .UsePlatformDetect()
                .UseManagedSystemDialogs()
                .With(x11Options)
                .With(new AvaloniaNativePlatformOptions { OverlayPopups = true })
                .With(new Win32PlatformOptions { OverlayPopups = true });
        }

        return builder;
#endif
    }
}
