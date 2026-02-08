using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using FluentAvalonia.UI.Controls;
using FluentAvalonia.UI.Windowing;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using SnapX.Avalonia.ViewModels;
using SnapX.Core.Upload;
using SnapX.Core.Utils;
using SnapX.Core.Utils.Extensions;
using ZXing;
using ZXing.Common;
using Image = Avalonia.Controls.Image;

namespace SnapX.Avalonia.Views;

public partial class QRCodeView : AppWindow
{
    private QRCodeViewModel ViewModel => (QRCodeViewModel)DataContext!;

    public QRCodeView()
    {
        InitializeComponent();
        DataContext = new QRCodeViewModel();

        ViewModel.DesiredSize = (int)QRSize.Value;

        AddHandler(DragDrop.DragOverEvent, DragOver);
        AddHandler(DragDrop.DragEnterEvent, DragEnter);
        AddHandler(DragDrop.DropEvent, Drop);
    }

    private void InitVM(object? Sender, EventArgs Args)
    {
        ViewModel.Initialize();
    }

    private void CopyImageButtonPressed(object? Sender, RoutedEventArgs RoutedEventArgs)
    {
        if (ViewModel.QrImage != null)
        {
            Clipboard.SetBitmapAsync(ViewModel.QrImage);
        }
    }

    private async void SaveImageButtonPressed(object? sender, RoutedEventArgs e)
    {
        try
        {
            var topLevel = GetTopLevel(this);
            if (topLevel == null || ViewModel.QrImage == null)
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
                ViewModel.QrImage.Save(file.Path.LocalPath, 100);
            }
        }
        catch (Exception ex)
        {
            ex.ShowError();
        }
    }

    private void UploadImageButtonPressed(object? Sender, RoutedEventArgs RoutedEventArgs)
    {
        if (ViewModel.QrImage == null) return;

        using var stream = new MemoryStream();
        ViewModel.QrImage.Save(stream, 100);
        stream.Position = 0;
        UploadManager.UploadImageStream(stream, "QRCode.png");
    }

    private async void DoDrag(Action<DataObject> factory, PointerEventArgs e, DragDropEffects effects)
    {
        try
        {
            var dragData = new DataObject();
            factory(dragData);
            await DragDrop.DoDragDrop(e, dragData, effects);
        }
        catch (Exception ex)
        {
            ex.ShowError();
        }
    }

    private void DragEnter(object? Sender, DragEventArgs e) { }

    private void DragOver(object? Sender, DragEventArgs e) { }

    private async void Drop(object? Sender, DragEventArgs e)
    {
        try
        {
            if (e.Source is Control)
            {
                e.DragEffects &= DragDropEffects.Move;
            }
            else
            {
                e.DragEffects &= DragDropEffects.Copy;
            }
            if (e.Data.Contains(DataFormats.Text))
            {
                ViewModel.QrText = e.Data.GetText();
            }
            else if (e.Data.Contains(DataFormats.Files))
            {
                var files = e.Data.GetFiles() ?? Array.Empty<IStorageItem>();
                foreach (var file in files)
                {
                    try
                    {
                        using var img = await SixLabors.ImageSharp.Image.LoadAsync<Rgba32>(file.Path.LocalPath);
                        await ScanImage(img, () =>
                        {
                            Drop(Sender, e);
                            return Task.CompletedTask;
                        });
                    }
                    catch (Exception ex)
                    {
                        ex.ShowError();
                    }
                }
            }
        }
        catch (Exception ex)
        {
            ex.ShowError();
        }
    }

    private async void ScanRegionButtonPressed(object? Sender, RoutedEventArgs E)
    {
        try
        {
            var scanImage = await RegionSelectorWindow.SelectRegionAsync();
            if (scanImage is null) return;

            await ScanImage(scanImage, () =>
            {
                ScanRegionButtonPressed(Sender, E);
                return Task.CompletedTask;
            });

            Activate();
            Focus();
        }
        catch (Exception ex)
        {
            ex.ShowError();
        }
    }

    private async Task ScanImage(SixLabors.ImageSharp.Image scanImage, Func<Task>? onRetry = null)
    {
        using var grayImage = scanImage.CloneAs<L8>();
        var width = grayImage.Width;
        var height = grayImage.Height;
        scanImage.Dispose();

        var reader = new ZXing.ImageSharp.BarcodeReader<L8>
        {
            AutoRotate = true,
            Options = new DecodingOptions
            {
                TryHarder = true,
                TryInverted = true,
                PossibleFormats = new List<BarcodeFormat> {
                    BarcodeFormat.QR_CODE, BarcodeFormat.DATA_MATRIX, BarcodeFormat.AZTEC,
                    BarcodeFormat.PDF_417, BarcodeFormat.CODE_128, BarcodeFormat.CODE_39
                }
            },
        };

        Result[]? results = reader.DecodeMultiple(grayImage);

        if (results is null || results.Length == 0)
        {
            if (width >= 600 || height >= 600)
            {
                // ITS HUNTING SEASON, BOYS
                results = ScanInTiles(grayImage, reader);
            }
        }

        if (results is null || results.Length == 0)
        {
            await Dispatcher.UIThread.InvokeAsync(async () =>
            {
                var dialog = new ContentDialog
                {
                    Title = "No barcode detected",
                    Content = new SelectableTextBlock
                    {
                        Text = "We couldn't find a code in that area. Try larger selection or higher contrast.",
                        TextWrapping = TextWrapping.Wrap
                    },
                    PrimaryButtonText = "Try Again",
                    CloseButtonText = "Dismiss"
                };
                if (await dialog.ShowAsync(this) == ContentDialogResult.Primary && onRetry != null)
                {
                    await onRetry();
                }
            });
            return;
        }

        foreach (var barcode in results)
        {
            await Dispatcher.UIThread.InvokeAsync(async () => await ShowResult(barcode));
        }
    }

    private async Task ShowResult(Result resultData)
    {
        var content = resultData.Text;
        var rawBytes = resultData.RawBytes;

        bool isBase45 = content.StartsWith("HC1:");
        bool isBase64 = !isBase45 && content.Length > 20 && (content.Length % 4 == 0)
                        && System.Text.RegularExpressions.Regex.IsMatch(content, @"^[a-zA-Z0-9\+/]*={0,2}$");

        bool hasReplacementChars = content.Contains('\uFFFD');

        bool hasControlChars = content.Any(c => char.IsControl(c) && c != '\r' && c != '\n' && c != '\t');

        bool isBinary = isBase45 || isBase64 || hasReplacementChars || hasControlChars;
        bool isUrl = !isBinary && Uri.TryCreate(content, UriKind.Absolute, out var uriResult);
        Bitmap? qrImage = null;
        if (rawBytes != null)
        {
            try
            {
                using var ms = new MemoryStream(rawBytes);
                qrImage = new Bitmap(ms);
            }
            catch { /* Not a valid image format */ }
        }

        var contentStack = new StackPanel { Spacing = 10, Width = 400 };

        if (qrImage != null)
        {
            contentStack.Children.Add(new Image()
            {
                Source = qrImage,
                MaxHeight = 300,
                HorizontalAlignment = HorizontalAlignment.Center
            });
        }

        contentStack.Children.Add(new SelectableTextBlock
        {
            Text = isBinary ? $"Binary payload: {rawBytes!.Length} bytes." : content,
            TextWrapping = TextWrapping.Wrap,
            FontSize = 14
        });

        var dialog = new ContentDialog
        {
            Title = isUrl ? "External Link Found" : isBinary ? "Binary Data Found" : "Text Content Found",
            Content = contentStack,
            CloseButtonText = "Dismiss",
            DefaultButton = ContentDialogButton.Close
        };

        if (isBinary && qrImage == null)
        {
            dialog.PrimaryButtonText = "Save Binary";
        }
        else
        {
            dialog.PrimaryButtonText = isUrl ? "Open Link" : "Copy Text";
            dialog.SecondaryButtonText = isUrl ? "Copy URL" : null;
        }

        var dialogResult = await dialog.ShowAsync(this);
        var topLevel = TopLevel.GetTopLevel(this);

        if (dialogResult == ContentDialogResult.Primary)
        {
            if (isBinary && qrImage == null)
            {
                var storageFile = await topLevel!.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions { Title = "Save Binary QR Content" });
                if (storageFile != null)
                {
                    await using var stream = await storageFile.OpenWriteAsync();
                    await stream.WriteAsync(rawBytes);
                }
            }
            else if (isUrl && content is not null)
            {
                URLHelpers.OpenURL(content);
            }
            else if (topLevel?.Clipboard is not null)
            {
                await topLevel.Clipboard.SetTextAsync(content);
            }
        }
        else if (dialogResult == ContentDialogResult.Secondary && isUrl && topLevel?.Clipboard is not null)
        {
            await topLevel.Clipboard.SetTextAsync(content);
        }
    }

    private Result[]? ScanInTiles(Image<L8> fullImage, ZXing.ImageSharp.BarcodeReader<L8> reader)
    {
        var tileSize = 600;
        var overlap = 100;
        var allResults = new List<Result>();
        for (var y = 0; y < fullImage.Height; y += (tileSize - overlap))
        {
            for (var x = 0; x < fullImage.Width; x += (tileSize - overlap))
            {
                var width = Math.Min(tileSize, fullImage.Width - x);
                var height = Math.Min(tileSize, fullImage.Height - y);
                var tileRect = new Rectangle(x, y, width, height);
                using var tile = fullImage.Clone();
                tile.Mutate(ctx => ctx.Crop(tileRect));

                var tileResult = reader.DecodeMultiple(tile);
                if (tileResult != null) allResults.AddRange(tileResult);
            }
        }
        return allResults.Count > 0 ? allResults.ToArray() : null;
    }

    private async void QrClipboard_Click(object? Sender, RoutedEventArgs E)
    {
        try
        {
            var clipboard = GetTopLevel(this)?.Clipboard;
            if (clipboard is null) return;

            using var data = await clipboard.TryGetDataAsync();
            if (data == null) return;

            var bitmap = await data.TryGetValueAsync(DataFormat.Bitmap);
            if (bitmap != null)
            {
                using (bitmap)
                {
                    using var ms = new MemoryStream();
                    bitmap.Save(ms);
                    ms.Position = 0;
                    using var img = await SixLabors.ImageSharp.Image.LoadAsync<Rgba32>(ms);
                    await ScanImage(img, () => { QrClipboard_Click(Sender, E); return Task.CompletedTask; });
                }
            }

            var files = await data.TryGetFilesAsync();
            if (files == null) return;
            {
                foreach (var file in files)
                {
                    try
                    {
                        using var img = await SixLabors.ImageSharp.Image.LoadAsync<Rgba32>(file.Path.LocalPath);
                        await ScanImage(img, () =>
                        {
                            QrClipboard_Click(Sender, E);
                            return Task.CompletedTask;
                        });
                    }
                    catch (Exception ex)
                    {
                        ex.ShowError();
                    }
                }
            }
        }
        catch (Exception ex)
        {
            await new ContentDialog
            {
                Title = "Error",
                Content = new SelectableTextBlock
                {
                    Text = ex.ToString(),
                    TextWrapping = TextWrapping.Wrap
                },
                CloseButtonText = "OK"
            }.ShowAsync(this);
        }
    }

    private async void QrFile_Click(object? Sender, RoutedEventArgs E)
    {
        try
        {
            var topLevel = GetTopLevel(this);
            if (topLevel is null) return;

            var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "Select Images",
                AllowMultiple = true,
                FileTypeFilter = [FilePickerFileTypes.ImageAll]
            });

            if (files is not { Count: > 0 }) return;

            await Parallel.ForEachAsync(files, async (file, ct) =>
            {
                await Task.Factory.StartNew(async () =>
                {
                    try
                    {
                        await using var stream = await file.OpenReadAsync();
                        using var img = await SixLabors.ImageSharp.Image.LoadAsync<Rgba32>(stream, ct);
                        await ScanImage(img, () =>
                        {
                            QrFile_Click(Sender, E);
                            return Task.CompletedTask;
                        });
                    }
                    catch { /* Handle */ }
                }, ct, TaskCreationOptions.LongRunning, TaskScheduler.Default).Unwrap();
            });
        }
        catch (Exception ex)
        {
            ex.ShowError();
        }
    }
    private async void SelectRegionDelay_Click(object? Sender, RoutedEventArgs E)
    {
        try
        {
            if (Sender is MenuItem { CommandParameter: string param } source)
            {
                var delay = param == "Default" ? (int)App.SnapX.GetConfiguration().DefaultTaskSettings.CaptureSettings.ScreenshotDelay : int.Parse(param);
                await Task.Delay(TimeSpan.FromSeconds(delay));
                RegionSplitButton.Tag = delay;

                if (param == "Default")
                {
                    RegionText.Text = "Scan from Region";
                    RegionIcon.Symbol = FluentIcons.Common.Symbol.ScreenCut;
                    source.Click += ScanRegionButtonPressed;
                }
                else
                {
                    RegionText.Text = $"Scan from Region ({delay}s Delay)";
                    RegionIcon.Symbol = FluentIcons.Common.Symbol.Clock;
                    source.Click += SelectRegionDelay_Click;
                }
                ScanRegionButtonPressed(null, E);
            }
        }
        catch (Exception ex)
        {
            ex.ShowError();
        }
    }
}
