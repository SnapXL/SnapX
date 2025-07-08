using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.DependencyInjection;
using SnapX.CommonUI.Models;

namespace SnapX.CommonUI.ViewModels;

public partial class SettingsMainViewVM : ViewModelBase
{
    [ObservableProperty]
    private bool _isPaneOpen = true;

    [ObservableProperty]
    private ViewModelBase _currentPage = new SettingsHomePageViewVM();
    [ObservableProperty]
    private ListItemTemplate? _selectedListItem;
    public ObservableCollection<ListItemTemplate> Items { get; }

    public SettingsMainViewVM()
    {
        _currentPage = new SettingsHomePageViewVM();
        Items = new ObservableCollection<ListItemTemplate>(_templates);

        SelectedListItem = Items.First(vm => vm.ModelType == typeof(SettingsHomePageViewVM));
    }
    partial void OnSelectedListItemChanged(ListItemTemplate? value)
    {
        if (value is null) return;
        var vm = Ioc.Default.GetService(value.ModelType);

        if (vm is not ViewModelBase vmb) return;

        CurrentPage = vmb;
    }
    private readonly List<ListItemTemplate> _templates =
    [
        new(typeof(SettingsHomePageViewVM), "HomeRegular", "Home"),
    ];
}
