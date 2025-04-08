// See https://aka.ms/new-console-template for more information
#pragma warning disable CA1416 // I am aware
using Avalonia;
using Avalonia.Dialogs;
using SnapX.Avalonia;

Console.WriteLine("Initializing Avalonia");
BuildAvaloniaApp()
    .StartWithClassicDesktopLifetime(args);

AppBuilder BuildAvaloniaApp()
    => AppBuilder.Configure<App>()
        .UsePlatformDetect()
        .UseManagedSystemDialogs()
        .WithInterFont()
        .With(new X11PlatformOptions()
        {
            // Fixes poor performance on my NVIDIA RTX 3060 Laptop GPU using Region Selector on Fedora KDE Wayland
            RenderingMode = [X11RenderingMode.Vulkan, X11RenderingMode.Egl, X11RenderingMode.Glx, X11RenderingMode.Software],
            UseRetainedFramebuffer = true,
            // I see white rectangle when using CommandBar on Fedora KDE Wayland. Maybe this will fix it?
            OverlayPopups = true,
        })
        .With(new AvaloniaNativePlatformOptions
        {
            // I see a big white rectangle on macOS when the CommandBar is first opened. Perhaps this will fix it?
            OverlayPopups = true
        })
        .With(new Win32PlatformOptions()
        {
            OverlayPopups = true,
        })
        .LogToTrace();
