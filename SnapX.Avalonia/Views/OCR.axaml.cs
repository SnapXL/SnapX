using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Media.TextFormatting;
using Avalonia.Styling;
using FluentAvalonia.UI.Windowing;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using SnapX.Avalonia.ViewModels;
using SnapX.Core;
using SnapX.Core.History;
using SnapX.Core.Utils;
using static SnapX.Core.Job.TaskHelpers;
using Point = Avalonia.Point;

namespace SnapX.Avalonia.Views;

public partial class OCR : AppWindow
{
    private OCRViewModel _ocrViewModel;
    private HistoryItem? _item;
    private SixLabors.ImageSharp.Image? _img;

    public OCR(HistoryItem? item, OCRViewModel viewModel, SixLabors.ImageSharp.Image? image = null)
    {

        DataContext = viewModel;
        _ocrViewModel = viewModel;

        _item = item;
        _img = image;
        InitializeComponent();

        // XAML will overwrite the title if it's put here
        if (_item is not null)
        {
            Title = $"OCR Result for {_item.FileName}";
        }
        else if (_img is not null)
        {
            Title = $"OCR Result for image {_img.Metadata.DecodedImageFormat?.Name}";
        }
        else
        {
            Title = "OCR Tool";
        }
        // LanguageSelector = this.FindControl<ComboBox>("LanguageSelector");
        // LanguageSelector!.ItemsSource = viewModel.LanguageDisplayNames;
        // LanguageSelector.Items = _languages;
        // LanguageSelector.SelectedIndex = 0;
        //
        // LoadImage();
        // RunOCR(_languages[0]);
    }

    public OCR()
        : this(null, new OCRViewModel()) { }
    public OCR(SixLabors.ImageSharp.Image img)
        : this(null, new OCRViewModel(), img) { }

    public OCR(HistoryItem item)
        : this(item, new OCRViewModel()) { }

    private async void LanguageSelector_SelectionChanged(
        object? sender,
        SelectionChangedEventArgs e
    )
    {
        DebugHelper.WriteLine($"{nameof(LanguageSelector_SelectionChanged)} triggered");

        if (LanguageSelector?.SelectedIndex is not (>= 0 and var index))
            return;

        _ocrViewModel.SelectedLanguageIndex = index;

        var code = _ocrViewModel.GetLanguageCode(index);
        await RunOCRAsync(code);
    }

    private async Task RunOCRAsync(string languageCode, CancellationToken cts = default)
    {
        DebugHelper.WriteLine($"{nameof(RunOCRAsync)} triggered");
        try
        {
            // WEED OUT THOSE DISPOSED OBJECTS FUCK YOU
            _ = _img?.Size;
        }
        catch (ObjectDisposedException)
        {
            _img = null;
        }
        if (_item is null && _img is null) return;
        ResultText?.Text = Lang.Processing;
        SourceImageDisplay.Source = null;
        ProgressIndicator.IsVisible = true;
        StatusText.IsVisible = true;

        var progressHandler = new Progress<OCRProgress>(update =>
        {
            var status = $"[{update.Percent}%] {update.Status}";
            DebugHelper.WriteLine($"{nameof(UpdateStatus)} : {status}");

            ResultText?.Text = status;
            ProgressIndicator.Value = update.Percent;
            ProgressIndicator.ProgressTextFormat = $"{update.Status} ({update.Percent}%)";
            StatusText.Text = status;
            if (update.Percent >= 100)
            {
                ProgressIndicator.IsVisible = false;
                StatusText.IsVisible = false;
            }
        });
        string result = string.Empty;
        try
        {
            await using var response = await _ocrViewModel.RunOCRAsync(_item, _img, languageCode, progressHandler, cts);
            result = response.FullText;
            if (response.AnnotatedImage is not null)
            {
                using var ms = new MemoryStream();


                // Calculate the scales based on the ORIGINAL size before we resize it
                double scaleX = TextOverlayCanvas.Width / response.AnnotatedImage.Width;
                double scaleY = TextOverlayCanvas.Height / response.AnnotatedImage.Height;

                // Resize the image to match the Canvas exactly otherwise text boxes aren't where they should be.
                response.AnnotatedImage.Mutate(x => x.Resize((int)TextOverlayCanvas.Width, (int)TextOverlayCanvas.Height, KnownResamplers.Bicubic));
                await response.AnnotatedImage.SaveAsWebpAsync(ms, cts).ConfigureAwait(true);

                ms.Seek(0, SeekOrigin.Begin);
                var bitmap = new Bitmap(ms);

                SourceImageDisplay.Source = bitmap;

                TextOverlayCanvas.Children.Clear();

                foreach (var line in response.Lines)
                {
                    double canvasX = line.MinX * scaleX;
                    double canvasY = line.MinY * scaleY;
                    double canvasW = line.Width * scaleX;
                    double canvasH = line.Height * scaleY;
                    // 2. Calculate rotation angle (in radians, then convert to degrees)
                    // Points are [[x,y], [x,y], [x,y], [x,y]]
                    var p1 = line.BoundingBox[0];
                    var p2 = line.BoundingBox[1];
                    double angleRad = Math.Atan2(p2[1] - p1[1], p2[0] - p1[0]);
                    double angleDeg = angleRad * (180.0 / Math.PI);
                    var fontSize = Math.Max(16, line.Height * scaleY * 0.8);
                    var fontFamily = FontFamily.Default;
                    double spacing = CalculateLetterSpacing(line.Text, canvasW, fontSize, fontFamily);
                    var overlayBox = new TextBox
                    {
                        Text = line.Text,
                        Width = canvasW,
                        Height = canvasH,
                        MinWidth = 0,
                        MinHeight = 0,
                        LetterSpacing = spacing,

                        // Visuals
                        Background = Brushes.Transparent,
                        BorderBrush = Brushes.Transparent,
                        BorderThickness = new Thickness(0),
                        Padding = new Thickness(0),
                        Margin = new Thickness(0),
                        Foreground = Brushes.Transparent,

                        // Selection & Cursor
                        SelectionForegroundBrush = Brushes.Transparent,
                        SelectionBrush = Brush.Parse("#330078d7"),
                        CaretBrush = Brushes.Transparent,
                        Cursor = new Cursor(StandardCursorType.None),

                        // Behavior
                        FocusAdorner = null,
                        IsReadOnly = true,
                        AcceptsReturn = false,
                        AcceptsTab = false,
                        TextWrapping = TextWrapping.NoWrap,
                        FontSize = fontSize,

                        RenderTransform = new RotateTransform(angleDeg),
                        // Set the transform origin to the top-left corner so it rotates around our X/Y
                        RenderTransformOrigin = new RelativePoint(0, 0, RelativeUnit.Relative),

                        // Scrollbars
                        [ScrollViewer.HorizontalScrollBarVisibilityProperty] = ScrollBarVisibility.Hidden,
                        [ScrollViewer.VerticalScrollBarVisibilityProperty] = ScrollBarVisibility.Hidden
                    };

                    // Prevent FluentAvalonia from showing hover/focus borders
                    // This forces the template to stay transparent even when the mouse is over it.
                    overlayBox.Styles.Add(new Style(x => x.Is<TextBox>())
                    {
                        Setters = {
                new Setter(BackgroundProperty, Brushes.Transparent),
                new Setter(BorderBrushProperty, Brushes.Transparent),
                new Setter(PaddingProperty, new Thickness(0)),
                new Setter(BorderThicknessProperty, new Thickness(0)),
                new Setter(FocusAdornerProperty, null),
            }
                    });

                    ToolTip.SetTip(overlayBox, new SelectableTextBlock()
                    {
                        Text = line.Text,
                        LetterSpacing = 0,
                    });

                    Canvas.SetLeft(overlayBox, canvasX);
                    Canvas.SetTop(overlayBox, canvasY);

                    TextOverlayCanvas.Children.Add(overlayBox);
                }
            }
            if (SingleLine?.IsChecked ?? false)
            {
                result = result.Replace("\r", "").Replace("\n", "");
            }
            ResultText?.Text = result;
        }
        catch (Exception ex)
        {
            ResultText?.Text = ex.ToString();
            DebugHelper.Logger?.Error(ex.ToString());
        }
        finally
        {
            OCRGrid?.IsEnabled = true;
            ProgressIndicator.IsVisible = false;
            StatusText.IsVisible = false;
            ResultText?.IsReadOnly = false;
        }
    }
    private double CalculateLetterSpacing(string text, double targetWidth, double fontSize, FontFamily fontFamily)
    {
        if (string.IsNullOrEmpty(text) || text.Length < 2) return 0;

        var typeface = new Typeface(fontFamily);

        var layout = new TextLayout(
            text,
            typeface,
            fontSize,
            Brushes.Black,
            TextAlignment.Left);

        var naturalWidth = layout.Width;
        var totalEmptySpace = targetWidth - naturalWidth;

        // Distribute space across gaps
        var spacing = totalEmptySpace / (text.Length - 1);

        // Prevent extreme negative spacing if the box is too small
        return Math.Max(spacing, -fontSize * 0.5);
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
    private async void SelectRegion_Click(object? Sender, RoutedEventArgs E)
    {
        var capturedImage = await RegionSelectorWindow.SelectRegionAsync();
        if (capturedImage is null)
        {
            DebugHelper.WriteLine("OCR Tool did not get back a image from the RegionSelectorWindow");
            return;
        }
        _img = capturedImage;
        if (LanguageSelector?.SelectedIndex is not (>= 0 and var index))
            return;
        Title = $"OCR Result for image {_img.Metadata.DecodedImageFormat?.Name}";
        var code = _ocrViewModel.GetLanguageCode(index);
        await RunOCRAsync(code);
    }
    private void OnToggleView(object? sender, RoutedEventArgs e)
    {
        var isImageMode = Equals(sender, ShowImageBtn);

        ImageViewer.IsVisible = isImageMode;
        ResultText.IsVisible = !isImageMode;

        // Update Button Styles (Optional: highlights the active mode)
        ShowImageBtn.Classes.Set("accent", isImageMode);
        ShowTextBtn.Classes.Set("accent", !isImageMode);
    }
    private Point _lastPoint;
    private bool _isPanning;
    private void ZoomContainer_OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        // Only pan with the Right mouse button (or Middle),
        // so Left click can still be used for text selection in your TextBoxes.
        var properties = e.GetCurrentPoint(ZoomContainer).Properties;
        if (properties.IsRightButtonPressed)
        {
            _isPanning = true;
            _lastPoint = e.GetPosition(this); // Get position relative to the window/view
            e.Pointer.Capture(ZoomContainer);
        }
    }

    private void ZoomContainer_OnPointerMoved(object? sender, PointerEventArgs e)
    {
        if (_isPanning && Parent is ScrollViewer sv)
        {
            var currentPoint = e.GetPosition(this);
            var delta = _lastPoint - currentPoint;

            // Move the scrollbars by the distance the mouse moved
            sv.Offset = new Vector(sv.Offset.X + delta.X, sv.Offset.Y + delta.Y);

            _lastPoint = currentPoint;
        }
    }

    private void ZoomContainer_OnPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        _isPanning = false;
        e.Pointer.Capture(null);
    }
    private void ZoomContainer_OnPointerWheelChanged(object? Sender, PointerWheelEventArgs e)
    {
        if (ZoomContainer.RenderTransform is not ScaleTransform st) return;
        // 1.1 for up, 0.9 for down
        double zoomFactor = e.Delta.Y > 0 ? 1.1 : 0.9;

        st.ScaleX *= zoomFactor;
        st.ScaleY *= zoomFactor;

        // Don't let them zoom out too far or in too close
        st.ScaleX = Math.Clamp(st.ScaleX, 0.1, 10.0);
        st.ScaleY = Math.Clamp(st.ScaleY, 0.1, 10.0);

        e.Handled = true;
    }
    private void SingleLine_OnTapped(object? Sender, TappedEventArgs E)
    {
        if (SingleLine?.IsChecked ?? false)
        {
            ResultText.Text = ResultText.Text?.Replace("\r", "").Replace("\n", "");
        }
    }
}
