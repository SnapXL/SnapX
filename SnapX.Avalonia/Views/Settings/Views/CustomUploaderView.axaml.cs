using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using SnapX.Avalonia.Converters;
using SnapX.Avalonia.ViewModels;
using SnapX.Core.Upload.Custom;

namespace SnapX.Avalonia.Views.Settings.Views;

public partial class CustomUploaderView : UserControl
{
    private readonly CustomUploaderVM _vm;

    public CustomUploaderView(CustomUploaderVM viewModel)
    {
        InitializeComponent();
        _vm = viewModel;
        DataContext = _vm;
    }

    private async void CustomUploaderView_Initialized(object? Sender, EventArgs E)
    {
        await _vm.InitializeAsync();
    }
    private void CustomUploaderView_Detached(object? sender, VisualTreeAttachmentEventArgs e)
    {
        _vm.Cleanup();
    }

    private void OnPointerEntered(object? sender, PointerEventArgs e)
    {
        if (sender is TextBox textBox && textBox.Effect is BlurEffect blur)
        {
            blur.Radius = 0;
        }
    }

    private void OnPointerExited(object? sender, PointerEventArgs e)
    {
        if (sender is TextBox textBox && textBox.Effect is BlurEffect blur)
        {
            if (textBox.DataContext is HeaderItem item)
            {
                var converter = new HeaderSecurityBlurConverter();
                var radius = converter.Convert(
                    item.Key,
                    typeof(double),
                    null,
                    System.Globalization.CultureInfo.CurrentCulture
                );
                blur.Radius = (double)(radius ?? 0.0);
            }
        }
    }
}
