using Avalonia.Controls;
using SnapX.Avalonia.ViewModels;

namespace SnapX.Avalonia.Views.Settings.Views;

public partial class ImportExportView : UserControl
{
    private readonly ImportExportVM _vm;
    public ImportExportView(ImportExportVM vm)
    {
        InitializeComponent();
        _vm = vm;
        DataContext = _vm;
    }

    public ImportExportView() : this(new ImportExportVM())
    {

    }
}

