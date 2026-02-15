using Avalonia.Controls;
using SixLabors.ImageSharp;
using SnapX.Avalonia.ViewModels.Settings;

namespace SnapX.Avalonia.Views.Settings.Views;

public partial class ScreenRecordOptionsView : UserControl
{
    private readonly ScreenRecordOptionsVM _vm;
    public ScreenRecordOptionsView(ScreenRecordOptionsVM viewModel)
    {
        _vm = viewModel;
        DataContext = _vm;
        InitializeComponent();
    }

    public ScreenRecordOptionsView() : this(new ScreenRecordOptionsVM(new Rectangle()))
    {
        InitializeComponent();
    }

}

