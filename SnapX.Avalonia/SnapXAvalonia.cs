using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using SnapX.Avalonia.ViewModels;
using SnapX.Avalonia.Views;
using SnapX.Avalonia.Views.About;
using SnapX.Avalonia.Views.Settings;
using SnapX.Avalonia.Views.Settings.Views;
using SnapX.CommonUI.ViewModels;

namespace SnapX.Avalonia;

public class SnapXAvalonia() : SnapX.Core.SnapX(BuildServices())
{
    private static IServiceProvider BuildServices()
    {
        var services = new ServiceCollection();
        Core.SnapX.ConfigureServices(services);
        ConfigureServices(services);
        var provider = services.BuildServiceProvider();

        try
        {
            Ioc.Default.ConfigureServices(provider);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to configure services");
        }
        Ioc.Default.AddStaticLogging();

        return services.BuildServiceProvider();
    }
    public static void ConfigureServices(IServiceCollection services)
    {
        services.AddTransient<MainViewModel>();
        services.AddSingleton<MainWindow>();
        services.AddTransient<SettingsWindow>();
        services.AddTransient<SettingsMainView>();
        services.AddTransient<SettingsMainViewVM>();
        services.AddTransient<SettingsHomePageView>();
        services.AddTransient<SettingsHomePageViewVM>();

        services.AddTransient<AboutWindow>();
        services.AddSingleton<AboutWindowViewModel>();

        services.AddTransient<HomePageView>();
        services.AddSingleton<HomePageViewModel>();

        services.AddSingleton<IMessenger>(WeakReferenceMessenger.Default);
    }
}
