using System.Diagnostics.CodeAnalysis;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.DependencyInjection;
using SnapX.Avalonia.ViewModels;
using SnapX.Avalonia.Views;
using SnapX.Avalonia.Views.Settings;
using SnapX.Avalonia.Views.Settings.Views;
using SnapX.CommonUI.ViewModels;

namespace SnapX.Avalonia;

public class ViewLocator : IDataTemplate
{
    private readonly Dictionary<Type, Func<Control?>> _locator = new();

    public ViewLocator()
    {
        RegisterViewFactory<MainViewModel, MainWindow>();
        RegisterViewFactory<HomePageViewModel, HomePageView>();
        RegisterViewFactory<RegionSelectorViewModel, RegionSelectorWindow>();
        RegisterViewFactory<SettingsMainViewVM, SettingsWindow>();
        RegisterViewFactory<SettingsHomePageViewVM, SettingsHomePageView>();

    }

    public Control Build(object? data)
    {
        if (data is null)
        {
            return new TextBlock { Text = "No VM provided" };
        }

        _locator.TryGetValue(data.GetType(), out var factory);

        return factory?.Invoke() ?? new TextBlock { Text = $"VM Not Registered: {data.GetType()}" };
    }

    public bool Match(object? data)
    {
        return data is ObservableObject;
    }

    private void RegisterViewFactory<TViewModel, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] TView>()
        where TViewModel : class
        where TView : Control
        => _locator.Add(
            typeof(TViewModel),
            Design.IsDesignMode
                ? Activator.CreateInstance<TView>
                : Ioc.Default.GetService<TView>);
}
