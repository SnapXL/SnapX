using Avalonia.Controls;
using Avalonia.Interactivity;
using FluentAvalonia.UI.Windowing;
using SnapX.Avalonia.ViewModels;
using SnapX.Core;
using SnapX.Core.History;
using SnapX.Core.Utils;

namespace SnapX.Avalonia.Views;

public partial class OCR : AppWindow
{
    private OCRViewModel _ocrViewModel;
    private HistoryItem? _item;


    public OCR(HistoryItem? item, OCRViewModel viewModel)
    {
        DataContext = viewModel;
        _ocrViewModel = viewModel;
        _item = item;
        InitializeComponent();
        // LanguageSelector = this.FindControl<ComboBox>("LanguageSelector");
        // LanguageSelector!.ItemsSource = viewModel.LanguageDisplayNames;
        // LanguageSelector.Items = _languages;
        // LanguageSelector.SelectedIndex = 0;
        //
        // LoadImage();
        // RunOCR(_languages[0]);
    }
    public OCR() : this(null, new OCRViewModel())
    {
    }
    public OCR(HistoryItem item) : this(item, new OCRViewModel())
    {
    }

    private async void LanguageSelector_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        DebugHelper.WriteLine($"{nameof(LanguageSelector_SelectionChanged)} triggered");

        if (LanguageSelector?.SelectedIndex is not (>= 0 and var index)) return;

        _ocrViewModel.SelectedLanguageIndex = index;

        var code = _ocrViewModel.GetLanguageCode(index);
        await RunOCRAsync(code);
    }

    private async Task RunOCRAsync(string languageCode)
    {
        DebugHelper.WriteLine($"{nameof(RunOCRAsync)} triggered");
        var textBox = this.FindControl<TextBox>("ResultText")!;
        textBox.Text = Lang.Processing;
        var result = await _ocrViewModel.RunOCRAsync(_item, languageCode);
        if (SingleLine?.IsChecked ?? false) result = result.Replace("\r", "").Replace("\n", "");
        textBox.Text = result;
    }

    private async void Control_OnLoaded(object? Sender, RoutedEventArgs E)
    {
        if (LanguageSelector?.SelectedIndex is not (>= 0 and var index))
        {
            DebugHelper.WriteLine($"WTF! Selected index is still invalid.");
            return;
        }

        _ocrViewModel.SelectedLanguageIndex = index;
        var code = _ocrViewModel.GetLanguageCode(index);
        await RunOCRAsync(code);
    }

    private void CopyResult_Click(object? Sender, RoutedEventArgs E)
    {
        Clipboard?.SetTextAsync(ResultText.Text);
    }
}

