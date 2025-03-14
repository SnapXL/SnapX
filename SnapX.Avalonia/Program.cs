// See https://aka.ms/new-console-template for more information
#pragma warning disable CA1416 // I am aware
using System.Collections.ObjectModel;
using Avalonia;
using Avalonia.Dialogs;
using Avalonia.Media;
using SnapX.Avalonia;

Console.WriteLine("Initializing Avalonia");
BuildAvaloniaApp()
    .StartWithClassicDesktopLifetime(args);

AppBuilder BuildAvaloniaApp()
    => AppBuilder.Configure<App>()
        .UsePlatformDetect()
        .UseManagedSystemDialogs()
        .WithInterFont()
        .LogToTrace();
