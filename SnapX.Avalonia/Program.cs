// See https://aka.ms/new-console-template for more information
#pragma warning disable CA1416 // I am aware
using System.Reflection;
using Avalonia;
using Avalonia.Dialogs;
using Avalonia.Media;
using SnapX.Avalonia;

if (args.Length != 0 && (args[0] == "--version" || args[0] == "-v"))
{
    var informationalVersion = Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "Unknown";
    Console.WriteLine(informationalVersion);
    return;
}

BuildAvaloniaApp()
    .StartWithClassicDesktopLifetime(args);

static AppBuilder BuildAvaloniaApp()
{
    var builder = AppBuilder.Configure<App>();

    if (OperatingSystem.IsLinux() || OperatingSystem.IsFreeBSD())
    {
        builder = builder.With(new FontManagerOptions
        {
            FontFallbacks = new List<FontFallback>
            {
                new()
                {
                    FontFamily = "Noto Sans"
                },
                new()
                {
                    FontFamily = "Roboto"
                },
                new()
                {
                    FontFamily = "Adwaita Sans"
                },
                new()
                {
                    FontFamily = "Helvetica Neue"
                },
                new()
                {
                    FontFamily = "Open Sans"
                },
                new()
                {
                    FontFamily = "Segoe UI"
                },
                new()
                {
                    // The Inter font is still used as a fallback for compatability.
                    FontFamily = "Inter"
                }
            }
        });
    }

    builder = builder.LogToTrace();

    var x11Options = new X11PlatformOptions
    {
        // Fixes poor performance on my NVIDIA RTX 3060 Laptop GPU using Region Selector on Fedora KDE Wayland
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
}
