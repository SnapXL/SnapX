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

AppBuilder BuildAvaloniaApp()
{
    var builder = AppBuilder.Configure<App>()
        .WithInterFont();

    if (OperatingSystem.IsLinux() || OperatingSystem.IsFreeBSD())
    {
        builder = builder.WithSystemFontSource(new Uri("fonts:Inter", UriKind.Absolute));
        builder = builder.With(new FontManagerOptions
        {
            DefaultFamilyName = "fonts:Inter#Inter",
            FontFallbacks =
            [
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
            ]
        });
    }

    builder = builder.LogToTrace();

    var useGPU = snapx.GetConfiguration().HardwareAccelerated;
    var x11Options = new X11PlatformOptions
    {
        // Fixes poor performance on my NVIDIA RTX 3060 Laptop GPU using Region Selector on Fedora KDE Wayland
        RenderingMode = [X11RenderingMode.Vulkan, X11RenderingMode.Egl, X11RenderingMode.Glx, X11RenderingMode.Software],
        UseRetainedFramebuffer = true,
        OverlayPopups = true
    };
    if (!useGPU) x11Options.RenderingMode = [X11RenderingMode.Software];

    var macOSOptions = new AvaloniaNativePlatformOptions
    {
        RenderingMode = [AvaloniaNativeRenderingMode.Metal, AvaloniaNativeRenderingMode.OpenGl, AvaloniaNativeRenderingMode.Software],
        OverlayPopups = true
    };
    if (!useGPU) macOSOptions.RenderingMode = [AvaloniaNativeRenderingMode.Software];

    var win32Options = new Win32PlatformOptions
    {
        // RenderingMode = [Win32RenderingMode.Vulkan, Win32RenderingMode.AngleEgl, Win32RenderingMode.Wgl, Win32RenderingMode.Software],
        OverlayPopups = true
    };
    if (!useGPU) win32Options.RenderingMode = [Win32RenderingMode.Software];

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
            .With(macOSOptions)
            .With(win32Options);
    }

    return builder;
}
