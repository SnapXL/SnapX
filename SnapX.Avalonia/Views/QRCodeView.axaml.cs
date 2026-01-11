using Avalonia.Controls;
using Avalonia.Input.Platform;
using Avalonia.Interactivity;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using DotNext.Threading;
using FluentAvalonia.UI.Windowing;
using SixLabors.ImageSharp.Formats.Png;
using SnapX.Avalonia.Views.Controls;
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

    private async void SaveImageButtonPressed(object? sender, RoutedEventArgs e)
    {
        var topLevel = GetTopLevel(this);
        if (topLevel == null)
            return;

        var file = await topLevel.StorageProvider.SaveFilePickerAsync(
            new FilePickerSaveOptions
            {
                Title = "Save QR Code",
                DefaultExtension = ".png",
                SuggestedFileName = $"QRCode_{DateTime.Now:yyyyMMdd_HHmmss}.png",
                FileTypeChoices = [new FilePickerFileType("PNG Image") { Patterns = ["*.png"] }],
            }
        );

        if (file != null)
        {
            string filePath = file.Path.LocalPath;

            image.Save(filePath, 100);
        }
    }

    private void UploadImageButtonPressed(object? Sender, RoutedEventArgs RoutedEventArgs)
    {
        using var stream = new MemoryStream();
        image.Save(stream, 100);
        stream.Position = 0;
        UploadManager.UploadImageStream(stream, "QRCode" + Sender + ".png");
    }

    private async void TextBox_OnTextChanged(object? sender, TextChangedEventArgs e)
    {
        await _cts.CancelAsync();

        _cts = new CancellationTokenSource();
        var qrText = this.FindControl<TextBox>("QRText")?.Text;
        var qrImgHolder = this.FindControl<SmartImage>("QRImage")!;
        try
        {
            await Task.Delay(200, _cts.Token);
            var qrImg = await RegenerateQRCodeAsync(qrText ?? Links.GitHub, _cts.Token);
            if (qrImg != null)
            {
                qrImgHolder.Source = qrImg;
                image = qrImg;
            }
        }
        catch (TaskCanceledException)
        {
            // Swallow
        }
    }

    private async void NumericUpDown_OnValueChanged(
        object? Sender,
        NumericUpDownValueChangedEventArgs E
    )
    {
        size = (int)(E.NewValue ?? E.OldValue ?? 64);
        if (size == null || size <= 0)
            size = 64;
        await _cts.CancelAsync();

        _cts = new CancellationTokenSource();
        var qrText = this.FindControl<TextBox>("QRText")?.Text;
        var qrImgHolder = this.FindControl<SmartImage>("QRImage")!;
        try
        {
            await Task.Delay(50, _cts.Token);

            var qrImg = await RegenerateQRCodeAsync(qrText, _cts.Token);
            if (qrImg != null)
            {
                qrImgHolder.Source = qrImg;
                image = qrImg;
            }
        }
        catch (TaskCanceledException)
        {
            // Swallow
        }
    }

    private async Task<Bitmap?> RegenerateQRCodeAsync(string text, CancellationToken ct = default)
    {
        using (await _qrCodeLock.AcquireAsync(ct))
        {
            DebugHelper.WriteLine($"Size : {size}");
            DebugHelper.WriteLine($"Text: {text}");
            try
            {
                using var generatedImg = await Task.Factory.StartNew(
                    () => TaskHelpers.GenerateQRCode(text, size),
                    ct,
                    TaskCreationOptions.LongRunning,
                    TaskScheduler.Default
                );
                if (generatedImg is null)
                    return null;
                using var stream = new MemoryStream();
                await generatedImg.SaveAsync(stream, new PngEncoder(), ct);
                stream.Position = 0;

                DebugHelper.WriteLine($"Generated QR Code: {generatedImg}");
                DebugHelper.WriteLine($"Stream bytes: {stream.Length}");
                return new Bitmap(stream);
            }
            catch (Exception ex)
            {
                DebugHelper.WriteLine($"Error generating QR Code: {ex}");
                return null;
            }
        }
    }
}
