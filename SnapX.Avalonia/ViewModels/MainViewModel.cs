using System.Collections.ObjectModel;
using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using SnapX.Avalonia.Models;

namespace SnapX.Avalonia.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    public MainViewModel(IMessenger messenger)
    {
        Items = new ObservableCollection<ListItemTemplate>(_templates);

        SelectedListItem = Items.First(vm => vm.ModelType == typeof(HomePageViewModel));
    }

    private readonly List<ListItemTemplate> _templates =
    [
        new(typeof(HomePageViewModel), "HomeRegular", "Home"),
        // new(typeof(ButtonPageViewModel), "CursorHoverRegular", "Buttons"),
        // new(typeof(TextPageViewModel), "TextNumberFormatRegular", "Text"),
        // new(typeof(ValueSelectionPageViewModel), "CalendarCheckmarkRegular", "Value Selection"),
        // new(typeof(ImagePageViewModel), "ImageRegular", "Images"),
        // new(typeof(GridPageViewModel), "GridRegular", "Grids"),
        // new(typeof(DragAndDropPageViewModel), "TapDoubleRegular", "Drang And Drop"),
        // new(typeof(LoginPageViewModel), "LockRegular", "Login Form"),
        // new(typeof(ChartsPageViewModel), "PollRegular", "Charts"),
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

        var vm = Design.IsDesignMode
            ? Activator.CreateInstance(value.ModelType)
            : Ioc.Default.GetService(value.ModelType);

        if (vm is not ViewModelBase vmb) return;

        CurrentPage = vmb;
    }

    public ObservableCollection<ListItemTemplate> Items { get; }


    [RelayCommand]
    private void TriggerPane()
    {
        IsPaneOpen = !IsPaneOpen;
    }
}
