using Avalonia.Controls;
using Avalonia.Interactivity;
using SnapX.CommonUI.ViewModels;

namespace SnapX.Avalonia.Views.Controls;

public partial class ChangelogControl : UserControl
{
    public ChangelogViewModel vm;
    public ChangelogControl()
    {
        InitializeComponent();
        vm = new ChangelogViewModel();
        DataContext = vm;
    }

    private async void Control_OnLoaded(object? Sender, RoutedEventArgs E)
    {
        await vm.LoadCommand.ExecuteAsync(this).ConfigureAwait(false);
    }
}

