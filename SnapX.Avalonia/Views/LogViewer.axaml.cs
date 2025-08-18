
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using FluentAvalonia.UI.Windowing;
using Serilog.Events;
using SnapX.Avalonia.ViewModels;
using SnapX.Core;
using SnapX.Core.Upload;
using SnapX.Core.Utils;

namespace SnapX.Avalonia.Views;

public partial class LogViewer : AppWindow
{
    private ScrollViewer? _scrollViewer;
    private SelectableTextBlock? _logTextBlock;
    private int _lastDisplayedLogCount;
    private DispatcherTimer _refreshTimer;
    private LogViewerViewModel _viewModel;
    public LogViewer(LogViewerViewModel viewModel)
    {
        _viewModel = viewModel;
        InitializeComponent();
        _refreshTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(10)
        };
        _refreshTimer.Tick += (_, _) => RefreshLogs();
    }

    public LogViewer() : this(new LogViewerViewModel())
    {
    }


    private void AppWindow_OnLoaded(object? sender, RoutedEventArgs e)
    {
        _scrollViewer = this.FindControl<ScrollViewer>("ScrollViewer");
        _logTextBlock = this.FindControl<SelectableTextBlock>("LogTextBlock");
        _lastDisplayedLogCount = 0;
        StartupPathText.Markdown = StartupPathText.Markdown!.Replace("$shortpath", SnapX.Core.SnapX.ShortenPath(Environment.CurrentDirectory)).Replace("$path", Environment.CurrentDirectory);

        _refreshTimer.Start();
    }
    private void SearchBox_PointerEnter(object? sender, PointerEventArgs e)
    {
        if (sender is TextBox tb)
            tb.Opacity = 1; // show when hovered
    }

    private void SearchBox_PointerLeave(object? sender, PointerEventArgs e)
    {
        if (sender is TextBox tb)
            tb.Opacity = 0; // hide when not hovered
    }
    private void AppWindow_OnClosed(object? sender, EventArgs e)
    {
        _lastDisplayedLogCount = 0;
        _refreshTimer.Stop();
    }

    private void Close_Click(object? sender, RoutedEventArgs e) => Close();

    private void RefreshLogs()
    {
        if (DebugHelper.LogEvents.Count() <= _lastDisplayedLogCount) return;
        for (var i = _lastDisplayedLogCount; i < DebugHelper.LogEvents.Count(); i++)
        {
            var logEvent = DebugHelper.LogEvents.ElementAt(i);
            var timestamp = logEvent.Timestamp.ToString("hh:mm:ss");
            var level = logEvent.Level.ToString().ToUpper();
            if (level == "INFORMATION") level = "INFO";
            var message = logEvent.RenderMessage();
            if (_logTextBlock is null)
            {
                DebugHelper.WriteLine($"{nameof(RefreshLogs)}: {nameof(_logTextBlock)} is null!");
                return;
            }

            _logTextBlock.Inlines!.Add(new Run($"{timestamp} ")
            {
                Foreground = Brushes.DimGray // Use DimGray for a softer grey that works well on dark backgrounds
            });

            _logTextBlock.Inlines.Add(new Run($"[{level}] ")
            {
                Foreground = GetBrushForLevel(logEvent.Level)
            });

            _logTextBlock.Inlines.Add(new Run($"{message}\n"));

            if (logEvent.Exception != null)
            {
                _logTextBlock.Inlines.Add(new Run(logEvent.Exception + "\n")
                {
                    Foreground = Brushes.DarkRed
                });
            }
            _lastDisplayedLogCount = DebugHelper.LogEvents.Count();
        }
    }
    private IBrush GetBrushForLevel(LogEventLevel level)
    {
        return level switch
        {
            LogEventLevel.Verbose => new LinearGradientBrush
            {
                StartPoint = new RelativePoint(0, 0, RelativeUnit.Relative),
                EndPoint = new RelativePoint(1, 1, RelativeUnit.Relative),
                GradientStops = new GradientStops
            {
                new GradientStop(Color.FromRgb(60, 60, 60), 0),
                new GradientStop(Color.FromRgb(40, 40, 40), 1)
            }
            },

            LogEventLevel.Debug => new LinearGradientBrush
            {
                StartPoint = new RelativePoint(0, 0, RelativeUnit.Relative),
                EndPoint = new RelativePoint(1, 1, RelativeUnit.Relative),
                GradientStops = new GradientStops
            {
                new GradientStop(Color.FromRgb(50, 100, 150), 0),
                new GradientStop(Color.FromRgb(30, 70, 120), 1)
            }
            },

            LogEventLevel.Information => new LinearGradientBrush
            {
                StartPoint = new RelativePoint(0, 0, RelativeUnit.Relative),
                EndPoint = new RelativePoint(1, 1, RelativeUnit.Relative),
                GradientStops = new GradientStops
            {
                new GradientStop(Color.FromRgb(70, 130, 70), 0),
                new GradientStop(Color.FromRgb(40, 90, 40), 1)
            }
            },

            LogEventLevel.Warning => new LinearGradientBrush
            {
                StartPoint = new RelativePoint(0, 0, RelativeUnit.Relative),
                EndPoint = new RelativePoint(1, 1, RelativeUnit.Relative),
                GradientStops = new GradientStops
            {
                new GradientStop(Color.FromRgb(200, 160, 0), 0),
                new GradientStop(Color.FromRgb(150, 110, 0), 1)
            }
            },

            LogEventLevel.Error => new LinearGradientBrush
            {
                StartPoint = new RelativePoint(0, 0, RelativeUnit.Relative),
                EndPoint = new RelativePoint(1, 1, RelativeUnit.Relative),
                GradientStops = new GradientStops
            {
                new GradientStop(Color.FromRgb(180, 50, 50), 0),
                new GradientStop(Color.FromRgb(130, 30, 30), 1)
            }
            },

            LogEventLevel.Fatal => new LinearGradientBrush
            {
                StartPoint = new RelativePoint(0, 0, RelativeUnit.Relative),
                EndPoint = new RelativePoint(1, 1, RelativeUnit.Relative),
                GradientStops = new GradientStops
            {
                new GradientStop(Color.FromRgb(100, 0, 0), 0),
                new GradientStop(Color.FromRgb(30, 0, 0), 1)
            }
            },

            _ => new SolidColorBrush(Colors.DimGray)
        };
    }

    private void ClearButton_OnClick(object? Sender, RoutedEventArgs E)
    {
        _logTextBlock?.Inlines?.Clear();
    }

    private void OpenLogFolderButton_OnClick(object? Sender, RoutedEventArgs E)
    {
        URLHelpers.OpenURL(Core.SnapX.LogsFolder);
    }

    private void UploadLogButton_OnClick(object? Sender, RoutedEventArgs E)
    {
        UploadManager.UploadText(_logTextBlock.Inlines?.OfType<Run>()
            .Aggregate("", (current, run) => current + run.Text)!);
    }

    private void CopyButton_OnClick(object? Sender, RoutedEventArgs E)
    {
        Clipboard?.SetTextAsync(_logTextBlock.Inlines?.OfType<Run>().Aggregate("", (current, run) => current + run.Text)!);
    }

    private void LogTextBlock_OnSizeChanged(object? Sender, SizeChangedEventArgs e)
    {
        if (!e.HeightChanged || _scrollViewer is null) return;
        var shouldScrollToEnd = Math.Abs(_scrollViewer.Offset.Y - _scrollViewer.Extent.Height + _scrollViewer.Viewport.Height) < 5; // Very small tolerance!
        if (shouldScrollToEnd) _scrollViewer.ScrollToEnd();
    }
}
