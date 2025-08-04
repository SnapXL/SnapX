using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using SnapX.Avalonia.ViewModels;
using SnapX.Core;
using SnapX.Core.Job;
using SnapX.Core.Upload;
using SnapX.Core.Utils;
using SnapX.Core.Utils.Extensions;
using SnapX.Core.Utils.Native;
using Image = SixLabors.ImageSharp.Image;
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
    private Image? _image;
    private Stream? _imageStream;
    private Rect _imageBounds;
    private List<Window> windowsHiddenByUs = [];
    public RegionSelectorWindow(RegionSelectorViewModel vm)
    {
        DataContext = vm;
        InitializeComponent();
        _selectionRect = this.FindControl<Rectangle>("SelectionRect");
        _dimmingOverlay = this.FindControl<Rectangle>("DimmingOverlay");
        _infoBox = this.FindControl<TextBox>("InfoBox");
        _canvas = this.FindControl<Canvas>("Canvas");

        // var workingArea = Screens.All
        //     .Select(screen => screen.Bounds)
        //     .Aggregate((acc, next) => acc.Union(next));
        var cursorPos = Methods.GetCursorPosition();
        var workingArea = Screens.ScreenFromPoint(new PixelPoint(cursorPos.X, cursorPos.Y))?.Bounds;
        if (workingArea != null && workingArea.HasValue)
        {
            workingArea = workingArea.Value;

            var x = workingArea.Value.X;
            var y = workingArea.Value.Y;
            var width = workingArea.Value.Width;
            var height = workingArea.Value.Height;
            DebugHelper.WriteLine($"VirtualScreen details: X is {x} Y is {y} Width is {width}  Height is {height}");
            Position = new PixelPoint(x, y);
            // _dimmingOverlay.Width = width;
            // _dimmingOverlay.Height = height;
            // Width = width;
            // height = width;
            _canvas.Width = width;
            _canvas.Height = height;
            var viewBox = _canvas.Parent as Viewbox;
            viewBox.Width = width;
            viewBox.Height = height;
        }

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
    private static void ShowErrorDialog(Exception ex, string? userMessage = null)
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
            Text = userMessage ?? Lang.FailedToScreenshot,
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

        void CancelAutoCloseOnInteraction(object? s, EventArgs e) => autoCloseCts.Cancel();

        messageText.PointerPressed += CancelAutoCloseOnInteraction;
        messageText.PointerReleased += CancelAutoCloseOnInteraction;
        errorDetails.PointerPressed += CancelAutoCloseOnInteraction;
        errorDetails.PointerReleased += CancelAutoCloseOnInteraction;

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
                // Ignored: user interacted with the dialog
            }
        }, autoCloseCts.Token);

        if (App.MyMainWindow != null)
        {
            App.MyMainWindow.Show(); // in case it was hidden
            dialog.ShowDialog(App.MyMainWindow);
        }
        else
        {
            dialog.Show();
        }
    }
    private void OnPointerReleased(object? Sender, PointerReleasedEventArgs E)
    {
        _isSelecting = false;
        _selectionRect.IsVisible = false;
        _dimmingOverlay.IsVisible = false;
        _infoBox.IsVisible = false;
        var selectedRegion = _imageBounds.Intersect(new Rect(_selectionRect.Bounds.X, _selectionRect.Bounds.Y, _selectionRect.Bounds.Width, _selectionRect.Bounds.Height));
        DebugHelper.WriteLine($"RegionSelectorWindow.OnPointerReleased: Region: {selectedRegion}");
        try
        {
            _ = Task.Run(() =>
            {
                if (_imageStream == null)
                {
                    DebugHelper.WriteLine("RegionSelectorWindow.OnPointerReleased: _imageStream is null");
                    return;
                }
                _image.Mutate(Context => Context.Crop(new SixLabors.ImageSharp.Rectangle((int)selectedRegion.X, (int)selectedRegion.Y, (int)selectedRegion.Width, (int)selectedRegion.Height)));

                UploadManager.RunImageTask(_image, TaskSettings.GetDefaultTaskSettings());
            });
        }
        catch (Exception ex)
        {
           ShowErrorDialog(ex);
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
    private readonly Dictionary<Window, WindowBase?> _ownershipMap = new();

    private void OnOpened(object? Sender, EventArgs EventArgs)
    {
        foreach (var win in App.MyMainWindow?.OwnedWindows.Where(w => w != this && w.IsVisible) ?? [])
        {
            _ownershipMap[win] = win.Owner; // Save original owner
            win.Hide();
            windowsHiddenByUs.Add(win);
        }
        if (App.MyMainWindow != null && App.MyMainWindow.IsVisible)
        {
            _ownershipMap[App.MyMainWindow] = App.MyMainWindow.Owner;
            App.MyMainWindow.Hide(); // Hide makes it lose relationship with child windows.
            windowsHiddenByUs.Add(App.MyMainWindow);
        }
        // Screenshotting can sometimes take time and block the UI thread.
        // It can also fail, so, we have to handle it gracefully.
        try
        {
            _image = Task.Factory.StartNew(() =>
                    TaskHelpers.GetScreenshot()
                        .CaptureActiveMonitor().GetAwaiter().GetResult(),
                TaskCreationOptions.LongRunning
            ).ConfigureAwait(false).GetAwaiter().GetResult();
        }
        catch (Exception ex)
        {
            ShowErrorDialog(ex);
        }

        if (_image == null) return;
        _imageStream = new MemoryStream();
        _image.SaveAsPng(_imageStream);
        _imageStream.Position = 0;
        _imageBounds = new Rect(_image.Bounds.X, _image.Bounds.Y, _image.Bounds.Width, _image.Bounds.Height);
        DebugHelper.WriteLine($"_imageStream {_imageStream.Length} (Readable? {_imageStream.CanRead}) bytes raw image bounds {_image.Bounds}");
        try
        {
            Background = new ImageBrush
            {
                Source = new Bitmap(_imageStream),
                Stretch = Stretch.UniformToFill,
            };
        }
        catch (Exception ex)
        {
            ShowErrorDialog(ex);
        }
        _image.Dispose();
    }
    List<Window> TopoSortWindows(IEnumerable<Window> windows)
    {
        var result = new List<Window>();
        var visited = new HashSet<Window>();

        void Visit(Window w)
        {
            if (!visited.Add(w))
                return;

            foreach (var child in w.OwnedWindows)
            {
                if (windowsHiddenByUs.Contains(child))
                    Visit(child);
            }

            result.Add(w);
        }

        foreach (var w in windows)
            Visit(w);

        result.Reverse(); // owners before owned
        return result;
    }
    private void OnClosed(object? Sender, EventArgs E)
    {
        _imageStream?.Dispose();
        _imageStream = null;
        var sortedWindows = TopoSortWindows(windowsHiddenByUs);

        foreach (var win in sortedWindows)
        {
            if (_ownershipMap.TryGetValue(win, out var owner) && owner?.IsVisible == true)
            {
                win.Show(owner as Window);
            }
            else
            {
                win.Show();
            }
        }
        _ownershipMap.Clear();
        windowsHiddenByUs.Clear();
    }
    private void OnLostFocus(object? Sender, RoutedEventArgs E)
    {
        if (Sender is Window window) window.Focus();
    }
}

