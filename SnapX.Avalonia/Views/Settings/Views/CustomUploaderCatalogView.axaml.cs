using Avalonia.Controls;
using SnapX.Avalonia.ViewModels;

namespace SnapX.Avalonia.Views.Settings.Views;


public partial class CustomUploaderCatalogView : UserControl
{
    private readonly CustomUploaderCatalogVM _vm;

    public CustomUploaderCatalogView(CustomUploaderCatalogVM viewModel)
    {
        InitializeComponent();
        _vm = viewModel;
        DataContext = _vm;
    }
    private void CatalogList_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (DataContext is CustomUploaderCatalogVM vm && sender is ListBox lb)
        {
            // Tell the VM which items are currently selected
            var selected = lb.SelectedItems.Cast<UploaderInfo>().ToList();
            vm.UpdateSelection(selected);
        }
    }
}
