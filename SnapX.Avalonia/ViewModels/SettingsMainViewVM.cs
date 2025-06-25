using Avalonia.Collections;
using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.DependencyInjection;
using SnapX.Avalonia.Models;

namespace SnapX.Avalonia.ViewModels;

public partial class SettingsMainViewVM : ViewModelBase
{
    [ObservableProperty]
    private bool _isPaneOpen = true;

    [ObservableProperty]
    private ViewModelBase _currentPage = new SettingsHomePageViewVM();
    [ObservableProperty]
    private ListItemTemplate? _selectedListItem;
    public AvaloniaList<ListItemTemplate> Items { get; }

    public SettingsMainViewVM()
    {
        _currentPage = new SettingsHomePageViewVM();
        Items = new AvaloniaList<ListItemTemplate>(_templates);

        SelectedListItem = Items.First(vm => vm.ModelType == typeof(SettingsHomePageViewVM));
    }
    partial void OnSelectedListItemChanged(ListItemTemplate? value)
    {
        if (value is null) return;
#pragma warning disable IL2072 // The code works, leave me alone
        var vm = Design.IsDesignMode
            ? Activator.CreateInstance(value.ModelType)
            : Ioc.Default.GetService(value.ModelType);
#pragma warning restore IL2072

        if (vm is not ViewModelBase vmb) return;

        CurrentPage = vmb;
    }
    private readonly List<ListItemTemplate> _templates =
    [
        new(typeof(SettingsHomePageViewVM), "HomeRegular", "Home"),
    ];
}
