using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using SixLabors.ImageSharp;
using SnapX.Avalonia.ViewModels;
using SnapX.Core;
using SnapX.Core.Job;
using SnapX.Core.Upload;
using SnapX.Core.Utils;
using Point = Avalonia.Point;
using Rectangle = Avalonia.Controls.Shapes.Rectangle;

namespace SnapX.Avalonia.Views;

public partial class RegionSelectorWindow : Window
{
    private Point _startPoint;
    private bool _isSelecting;

    private readonly Rectangle _selectionRect;
    private readonly Rectangle _dimmingOverlay;
    private readonly TextBox _infoBox;
    private readonly Canvas _canvas;
    // private readonly SixLabors.ImageSharp.Image _image;
    private Stream? _imageStream;
    private Rect _imageBounds;
    private List<Window> windowsHiddenByUs = new();
    public RegionSelectorWindow(RegionSelectorViewModel vm)
    {
        DataContext = vm;
        InitializeComponent();
        _selectionRect = this.FindControl<Rectangle>("SelectionRect");
        _dimmingOverlay = this.FindControl<Rectangle>("DimmingOverlay");
        _infoBox = this.FindControl<TextBox>("InfoBox");
        _canvas = this.FindControl<Canvas>("Canvas");

        var allScreens = Screens.All;
        var firstScreen = allScreens.FirstOrDefault();

        var (x, y, width, height) = CaptureHelpers.GetActiveScreenWorkingArea();
        Position = new PixelPoint(x, y);
        _dimmingOverlay.Width = width;
        _dimmingOverlay.Height = height;
        _canvas.Width = width;
        _canvas.Height = height;

    }
    public RegionSelectorWindow() : this(new RegionSelectorViewModel()) { }
    private void OnPointerPressed(object? Sender, PointerPressedEventArgs E)
    {
        _startPoint = E.GetPosition(this);
        _isSelecting = true;

        _selectionRect.Width = 0;
        _selectionRect.Height = 0;
        _selectionRect.Margin = new Thickness(_startPoint.X, _startPoint.Y, 0, 0);

        _infoBox.IsVisible = true;

    }

    private void OnPointerReleased(object? Sender, PointerReleasedEventArgs E)
    {
        _isSelecting = false;
        _selectionRect.IsVisible = false;
        _dimmingOverlay.IsVisible = false;
        _infoBox.IsVisible = false;
        _imageBounds.Intersect(new Rect(_selectionRect.Bounds.X, _selectionRect.Bounds.Y, _selectionRect.Bounds.Width, _selectionRect.Bounds.Height));
        DebugHelper.WriteLine($"RegionSelectorWindow.OnPointerReleased: Region: {_selectionRect.Bounds}");
        try
        {
            var img = TaskHelpers.GetScreenshot(TaskSettings.GetDefaultTaskSettings()).CaptureRectangle(new SixLabors.ImageSharp.Rectangle((int)_selectionRect.Bounds.X, (int)_selectionRect.Bounds.Y, (int)_selectionRect.Bounds.Width, (int)_selectionRect.Bounds.Height));
            if (img != null)
            {
                UploadManager.RunImageTask(img, TaskSettings.GetDefaultTaskSettings());
            }
        }
        catch (Exception ex)
        {
            DebugHelper.WriteException(ex);
    var dialog = new Window
    {
        Title = Lang.Error,
        Width = 800,
        Height = 450,
        CanResize = false,
        WindowStartupLocation = WindowStartupLocation.CenterOwner
    };

    var autoCloseCts = new CancellationTokenSource();

    var messageText = new TextBlock
    {
        Text = Lang.FailedToScreenshot,
        FontWeight = FontWeight.Bold,
        Margin = new Thickness(0, 0, 0, 10)
    };

    var errorDetails = new ScrollViewer
    {
        Margin = new Thickness(2, 0, 0, 10),
        Content = new TextBlock
        {
            Text = ex.ToString(),
            TextWrapping = TextWrapping.Wrap
        }
    };

    var okButton = new Button
    {
        Content = Lang.Ok,
        HorizontalAlignment = HorizontalAlignment.Right,
        Margin = new Thickness(5, 10, 0, 0)
    };
    okButton.Click += (_, _) =>
    {
        autoCloseCts.Cancel();
        dialog.Close();
    };

    errorDetails.PointerPressed += (_, _) => autoCloseCts.Cancel();
    errorDetails.PointerReleased += (_, _) => autoCloseCts.Cancel();
    messageText.PointerPressed += (_, _) => autoCloseCts.Cancel();
    messageText.PointerReleased += (_, _) => autoCloseCts.Cancel();

    dialog.Content = new StackPanel
    {
        Margin = new Thickness(10),
        Children =
        {
            messageText,
            errorDetails,
            new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                Children = { okButton },
                Spacing = 10
            }
        }
    };

    _ = Task.Run(async () =>
    {
        try
        {
            await Task.Delay(TimeSpan.FromSeconds(20), autoCloseCts.Token);
            await Dispatcher.UIThread.InvokeAsync(() => dialog.Close());
        }
        catch (TaskCanceledException)
        {
            // User clicked a button to prevent auto-close or already closed it
        }
    });
            if (App.MyMainWindow != null)
            {
                App.MyMainWindow?.Show();
                dialog.ShowDialog(App.MyMainWindow);
            }
            else
            {
                dialog.Show();
            }
        }
        App.MyMainWindow?.Show();
        Close();
    }

    private void OnPointerMoved(object? Sender, PointerEventArgs E)
    {
        if (!_isSelecting) return;
        var endPoint = E.GetPosition(this);
        var x = Math.Min(_startPoint.X, endPoint.X);
        var y = Math.Min(_startPoint.Y, endPoint.Y);
        var width = Math.Abs(_startPoint.X - endPoint.X);
        var height = Math.Abs(_startPoint.Y - endPoint.Y);
        _selectionRect.Width = width;
        _selectionRect.Height = height;
        _selectionRect.Margin = new Thickness(x, y, 0, 0);
        // UpdateDimmingOverlay(x, y, width, height);

        _infoBox.Text = $"X: {x}, Y: {y}, Width: {width}, Height: {height}";
        _infoBox.Margin = new Thickness(x, y - 30, 0, 0);

    }
    private void UpdateDimmingOverlay(double x, double y, double width, double height)
    {
        var overlay = new DrawingGroup();
        using (var context = overlay.Open())
        {
            var perimeter = _canvas.Bounds;
            context.FillRectangle(Brushes.Black, perimeter);
            context.FillRectangle(Brushes.Transparent, _selectionRect.Bounds.Intersect(perimeter));
        }

        _dimmingOverlay.Fill = new DrawingBrush(overlay);
    }
    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        DebugHelper.WriteLine($"{sender}.OnKeyDown: Key: {e.Key}");
        switch (e.Key)
        {
            case Key.Enter:
                OnPointerReleased(this, null);
                break;
            case Key.Escape:
                Close();
                break;
        }
    }

    private void OnOpened(object? Sender, EventArgs EventArgs)
    {
        if (App.MyMainWindow != null && App.MyMainWindow.IsVisible)
        {
            App.MyMainWindow.Hide();
            windowsHiddenByUs.Add(App.MyMainWindow);
        }
        foreach (var win in App.MyMainWindow?.OwnedWindows.Where(w => w != null && w != this && w.IsVisible))
        {
            win.Hide();
            windowsHiddenByUs.Add(win);
        }
        // Screenshotting is synchronous that blocks the UI thread. FUCK YOU
        var image = Task.Factory.StartNew(() =>
                TaskHelpers.GetScreenshot(TaskSettings.GetDefaultTaskSettings())
                    .CaptureActiveMonitor(),
            TaskCreationOptions.LongRunning
        ).GetAwaiter().GetResult().GetAwaiter().GetResult();
        // Convert ImageSharp image to Avalonia Bitmap via a MemoryStream
        _imageStream = new MemoryStream();
        image.SaveAsPng(_imageStream);
        _imageStream.Position = 0;
        _imageBounds = new Rect(image.Bounds.X, image.Bounds.Y, image.Bounds.Width, image.Bounds.Height);
        image.Dispose();
        DebugHelper.WriteLine($"_imageStream {_imageStream.Length} (Readable? {_imageStream.CanRead}) bytes raw image bounds {image.Bounds}");
        // Take a fullscreen screenshot then use it as background instead of transparency if light taskSetting is set.
        // If the application has already taken the fullscreen screenshot, crop it with the region captured.
        // Screenshotting is an expensive operation!
        try
        {
            Background = new ImageBrush()
            {
                Source = new Bitmap(_imageStream),
                Stretch = Stretch.UniformToFill
            };
        }
        catch (Exception e)
        {
            DebugHelper.WriteException(e);
        }
        _imageStream.Dispose();
        _imageStream = null;
    }

    private void OnClosed(object? Sender, EventArgs E)
    {
        _imageStream?.Dispose();
        _imageStream = null;
        foreach (var win in windowsHiddenByUs) { win.Show(); }
        windowsHiddenByUs.Clear();
    }
    private void OnLostFocus(object? Sender, RoutedEventArgs E)
    {
        if (Sender is Window window) window.Focus();
    }
}

