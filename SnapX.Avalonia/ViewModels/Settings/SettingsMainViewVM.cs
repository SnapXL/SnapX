using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.DependencyInjection;
using SnapX.Core;

namespace SnapX.Avalonia.ViewModels;

public partial class SettingsMainViewVM : ViewModelBase
{
    [ObservableProperty]
    private bool _isPaneOpen = true;

    [ObservableProperty]
    private ViewModelBase _currentPage = new SettingsHomePageViewVM();
    private readonly Stack<ViewModelBase> _history = new();
    public bool CanGoBack => _history.Count > 0;
    private readonly Dictionary<string, Type> _pageFactory = [];

    public SettingsMainViewVM()
    {
        RegisterPage<SettingsHomePageViewVM>("Home");
        RegisterPage<CustomUploaderVM>("CustomUploader");
        RegisterPage<ImportExportVM>("ImportExport");

    }

    private void RegisterPage<T>(string key)
        where T : ViewModelBase
    {
        var type = typeof(T);

        // Primary key
        _pageFactory[key] = type;

        // Variants
        var variants = new[]
        {
            type.Name,
            key + "VM",
            key + "PageViewVM",
            key + "ViewModel"
        };

        foreach (var variant in variants)
        {
            _pageFactory[variant] = type;
        }
    }
    public void Navigate(string destinationTag)
    {
        DebugHelper.WriteLine(destinationTag);
        if (_pageFactory.TryGetValue(destinationTag, out var factory))
        {
            var type = factory;
            DebugHelper.WriteLine(type.ToString());
            var vm = Design.IsDesignMode
                ? Activator.CreateInstance(type)
                : Ioc.Default.GetService(type);
            if (vm is not ViewModelBase vmb)
            {
                DebugHelper.WriteLine($"Can't get ViewModelBase on {type}. Did you register it in ViewLocator & IoC?");
                return;
            }
            _history.Push(CurrentPage);
            CurrentPage = vmb;
        }
        else
        {
            // fallback, e.g. Home page
            _history.Push(CurrentPage);
            CurrentPage = new SettingsHomePageViewVM();
        }
    }
    public bool TryGetPage(string tag, out Type type)
    {
        return _pageFactory.TryGetValue(tag, out type);
    }
    public void Back()
    {
        if (!CanGoBack)
            return;

        CurrentPage = _history.Pop();
    }
}
