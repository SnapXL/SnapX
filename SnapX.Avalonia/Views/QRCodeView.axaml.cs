using Avalonia.Controls;
using Avalonia.Input.Platform;
using Avalonia.Interactivity;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using DotNext.Threading;
using FluentAvalonia.UI.Windowing;
using SixLabors.ImageSharp.Formats.Png;
using SnapX.Core;
using SnapX.Core.Job;
using SnapX.Core.Upload;
using SnapX.Core.Utils.Miscellaneous;

namespace SnapX.Avalonia.Views;

public partial class QRCodeView : AppWindow
{
    private Bitmap image;
    private readonly AsyncLock _qrCodeLock = new();
    private int size;
    private CancellationTokenSource _cts = new CancellationTokenSource();
    public QRCodeView()
    {
        InitializeComponent();
        size = (int)QRSize.Value;
    }

    private void CopyImageButtonPressed(object? Sender, RoutedEventArgs RoutedEventArgs)
    {
        Clipboard.SetBitmapAsync(image);
    }

    private void SaveImageButtonPressed(object? Sender, RoutedEventArgs RoutedEventArgs)
    {
        image.Save("QRCode" + Sender + ".png", 100);
    }

    private void UploadImageButtonPressed(object? Sender, RoutedEventArgs RoutedEventArgs)
    {
        var stream = new MemoryStream();
        image.Save(stream, 100);
        stream.Position = 0;
        UploadManager.UploadImageStream(stream, "QRCode" + Sender + ".png");
    }

    private async void TextBox_OnTextChanged(object? sender, TextChangedEventArgs e)
    {
        await _cts.CancelAsync();

        _cts = new CancellationTokenSource();

        try
        {
            await Task.Delay(200, _cts.Token);

            await RegenerateQRCodeAsync(_cts.Token);
        }
        catch (TaskCanceledException)
        {
            // Swallow
        }
    }

    private async void NumericUpDown_OnValueChanged(object? Sender, NumericUpDownValueChangedEventArgs E)
    {
        size = (int)E.NewValue;
        await _cts.CancelAsync();

        _cts = new CancellationTokenSource();

        try
        {
            await Task.Delay(50, _cts.Token);

            await RegenerateQRCodeAsync(_cts.Token);
        }
        catch (TaskCanceledException)
        {
            // Swallow
        }
    }

    private async Task RegenerateQRCodeAsync(CancellationToken ct = default)
    {
        using (await _qrCodeLock.AcquireAsync(ct))
        {
            DebugHelper.WriteLine($"Size : {size}");
            DebugHelper.WriteLine($"Text: {QRText.Text}");
            var text = QRText.Text;
            var generatedImg = await Task.Run(() =>
                TaskHelpers.GenerateQRCode(text ?? Links.GitHub, size), ct);
            if (generatedImg is null) return;

            var stream = new MemoryStream();
            await generatedImg.SaveAsync(stream, new PngEncoder(), ct);
            stream.Position = 0;

            DebugHelper.WriteLine($"Generated QR Code: {generatedImg}");
            DebugHelper.WriteLine($"Stream bytes: {stream.Length}");
            Dispatcher.UIThread.Invoke(() =>
            {
                image = new Bitmap(stream);
                QRImage.Source = image;
            });
        }
    }
}

