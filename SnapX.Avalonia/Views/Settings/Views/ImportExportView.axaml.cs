using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using SnapX.Avalonia.ViewModels;

namespace SnapX.Avalonia.Views.Settings.Views;

public partial class ImportExportView : UserControl
{
    private readonly ImportExportVM _vm;
    public ImportExportView(ImportExportVM vm)
    {
        _vm = vm;
        DataContext = _vm;
        InitializeComponent();
    }

    public ImportExportView() : this(new ImportExportVM())
    {

    }
}

