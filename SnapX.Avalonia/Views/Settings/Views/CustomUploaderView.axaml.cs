using Avalonia.Controls;
using SnapX.Avalonia.ViewModels;

namespace SnapX.Avalonia.Views.Settings.Views;

public partial class CustomUploaderView : UserControl
{
    private readonly CustomUploaderVM _vm;
    public CustomUploaderView(CustomUploaderVM viewModel)
    {
        _vm = viewModel;
        DataContext = _vm;
        InitializeComponent();
    }

    public CustomUploaderView() : this(new CustomUploaderVM())
    {

    }

    private void CustomUploaderView_Initialized(object? Sender, EventArgs E)
    {
        _ = _vm.InitializeAsync();
    }
}

