// See https://aka.ms/new-console-template for more information
#pragma warning disable CA1416 // I am aware
using Avalonia;
using Avalonia.Dialogs;
using Avalonia.Media;
using SnapX.Avalonia;

var snapx = new SnapXAvalonia();

snapx.loadApplicationSettingsPartial();

BuildAvaloniaApp()
    .StartWithClassicDesktopLifetime(args);

static AppBuilder BuildAvaloniaApp()
{
    var builder = AppBuilder.Configure<App>()
        .WithInterFont();

    if (OperatingSystem.IsLinux() || OperatingSystem.IsFreeBSD())
    {
        builder = builder.WithSystemFontSource(new Uri("fonts:Inter", UriKind.Absolute));
        builder = builder.With(new FontManagerOptions
        {
            DefaultFamilyName = "fonts:Inter#Inter",
            FontFallbacks = new List<FontFallback>
            {
                new()
                {
                    FontFamily = "fonts:Inter#Inter"
                },
                new()
                {
                    FontFamily = "Noto Sans"
                },
                new()
                {
                    FontFamily = "Segoe UI"
                },
                new()
                {
                    FontFamily = "Roboto"
                },
                new()
                {
                    FontFamily = "Adwaita Sans"
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

    if (OperatingSystem.IsFreeBSD())
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
