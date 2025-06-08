using System.Collections.ObjectModel;
using Avalonia.Collections;
using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using SnapX.Avalonia.Models;

namespace SnapX.Avalonia.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    public AvaloniaList<ListItemTemplate> Items { get; }

    public MainViewModel(IMessenger messenger)
    {
        Items = new AvaloniaList<ListItemTemplate>(_templates);

        SelectedListItem = Items.First(vm => vm.ModelType == typeof(HomePageViewModel));
    }

    private readonly List<ListItemTemplate> _templates =
    [
        new(typeof(HomePageViewModel), "HomeRegular", "Home"),
    ];

    public MainViewModel() : this(new WeakReferenceMessenger()) { }

    [ObservableProperty]
    private bool _isPaneOpen = true;

    [ObservableProperty]
    private ViewModelBase _currentPage = new HomePageViewModel();

    [ObservableProperty]
    private ListItemTemplate? _selectedListItem;
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

    [RelayCommand]
    private void TriggerPane()
    {
        IsPaneOpen = !IsPaneOpen;
    }

    [RelayCommand]
    private void OpenAboutWindow()
    {
        App.CreateAboutWindowStatic();
    }
    [RelayCommand]
    private void OpenSettingsWindow()
    {
        App.CreateOrOpenSettingsWindowStatic();
    }
}
