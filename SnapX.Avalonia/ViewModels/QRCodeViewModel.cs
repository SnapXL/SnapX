using System.Text;
using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using DotNext.Threading;
using FluentAvalonia.UI.Controls;
using SixLabors.ImageSharp.Formats.Png;
using SnapX.Core.Job;
using SnapX.Core.Utils;
using SnapX.Core.Utils.Extensions;
using SnapX.Core.Utils.Miscellaneous;
using ZXing.Common;

namespace SnapX.Avalonia.ViewModels;

public partial class QRCodeViewModel : ViewModelBase
{
    private readonly AsyncLock _qrCodeLock = new();
    private CancellationTokenSource _cts = new();

    [ObservableProperty]
    private Bitmap? _qrImage;

    [ObservableProperty]
    private Geometry? _qrGeometry;

    [ObservableProperty]
    private string _qrText = Links.GitHub;

    [ObservableProperty]
    private int _desiredSize = 64;
    [ObservableProperty]
    private string _qrTransform = "scale(1)";
    [ObservableProperty]
    private double _qrOpacity = 1.0;
    partial void OnDesiredSizeChanged(int value)
    {
        if (value < 64)
        {
            DesiredSize = 64;
            return;
        }

        _ = TriggerRegeneration(150);
    }
    public void Initialize() => _ = TriggerRegeneration(0);

    partial void OnQrTextChanged(string value) => _ = TriggerRegeneration(200);

    private async Task TriggerRegeneration(int delayMs)
    {
        await _cts.CancelAsync();
        _cts = new CancellationTokenSource();

        try
        {
            await Task.Delay(delayMs, _cts.Token);
            await RegenerateQRCodeAsync(QrText, _cts.Token);
        }
        catch (TaskCanceledException) { }
    }

    public async Task RegenerateQRCodeAsync(string text, CancellationToken ct = default)
    {
        using (await _qrCodeLock.AcquireAsync(ct))
        {
            QrTransform = "scale(0.95)";
            QrOpacity = 0.6;
            try
            {
                var (generatedImg, matrix) = await Task.Factory.StartNew(
                    () => TaskHelpers.GenerateQRCodeWithMatrix(text, DesiredSize),
                    ct,
                    TaskCreationOptions.LongRunning,
                    TaskScheduler.Default
                );

                if (generatedImg is null || matrix is null)
                {
                    await HandleGenerationFailure(text);
                    return;
                }

                using (generatedImg)
                {

                    var geo = GenerateQRGeometry(matrix);

                    using var stream = new MemoryStream();
                    await generatedImg.SaveAsync(stream, new PngEncoder(), ct);
                    stream.Position = 0;
                    var bitmap = new Bitmap(stream);

                    await Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        QrGeometry = geo;
                        QrImage = bitmap;
                    });
                }
            }
            catch (TaskCanceledException)
            {
            }
            catch (Exception ex)
            {
                ex.ShowError();
            }
            finally
            {
                QrTransform = "scale(1)";
                QrOpacity = 1;
            }
        }
    }

    private StreamGeometry GenerateQRGeometry(BitMatrix matrix)
    {
        var geometry = new StreamGeometry();
        using (var context = geometry.Open())
        {
            for (int y = 0; y < matrix.Height; y++)
            {
                for (int x = 0; x < matrix.Width; x++)
                {
                    if (matrix[x, y])
                    {
                        int startX = x;
                        // Find how many consecutive horizontal bits we can merge
                        while (x + 1 < matrix.Width && matrix[x + 1, y])
                        {
                            x++;
                        }

                        int endX = x + 1;

                        context.BeginFigure(new Point(startX, y), true);
                        context.LineTo(new Point(endX, y));
                        context.LineTo(new Point(endX, y + 1));
                        context.LineTo(new Point(startX, y + 1));
                        context.EndFigure(true);
                    }
                }
            }
        }
        return geometry;
    }

    private async Task HandleGenerationFailure(string text)
    {
        int byteCount = Encoding.UTF8.GetByteCount(text);
        bool isTooBig = byteCount > 2952;

        var errorMessages = new[]
        {
            "The pixels went on strike. We're negotiating.",
            "QR matrix collapsed. Even the squares are tired.",
            "Something went wrong, but at least your CPU didn't melt.",
            "The generator looked at your request and decided to take a nap.",
            "Instruction unclear: Square became a circle. Aborting.",
            "Your data is too powerful for this tiny window."
        };

        var randomJoke = errorMessages[Random.Shared.Next(errorMessages.Length)];

        await Dispatcher.UIThread.InvokeAsync(async () =>
        {
            var dialog = new ContentDialog()
            {
                Title = isTooBig ? "Content too large" : "Generation Failed",
                Content = isTooBig
                    ? $"This content is {byteCount} bytes. The limit for a QR code is 2952 bytes."
                    : $"{randomJoke} (Generation failed)",
                PrimaryButtonText = "Dismiss",
                SecondaryButtonText = "See Why",
                DefaultButton = ContentDialogButton.Primary
            };

            if (await dialog.ShowAsync() == ContentDialogResult.Secondary)
                URLHelpers.OpenURL("https://www.qrcode.com/en/about/version.html");
        });
    }
}
