using System.Diagnostics;
using SixLabors.ImageSharp;
using SnapX.Core.ScreenCapture;

namespace SnapX.Avalonia.ViewModels.Settings;

public partial class ScreenRecordOptionsVM : ViewModelBase, IDisposable
{
    public event Action StopRequested;

    private ScreenRecordingStatus status;

    public ScreenRecordingStatus Status
    {
        get
        {
            return status;
        }
        private set
        {
            status = value;
        }
    }

    public TimeSpan Countdown { get; set; }
    public bool IsCountdown { get; private set; }
    public Stopwatch Timer { get; private set; }
    public ManualResetEvent RecordResetEvent { get; set; }
    public bool ActivateWindow { get; set; } = true;
    public float Duration { get; set; } = 0;
    public bool AskConfirmationOnAbort { get; set; } = false;

    private Color borderColor = Color.Red;
    private Rectangle borderRectangle;
    private Rectangle borderRectangle0Based;
    private bool dragging;
    private Point initialLocation;
    public ScreenRecordOptionsVM() : this(new Rectangle()) { }

    public ScreenRecordOptionsVM(Rectangle regionRectangle)
    {
        borderRectangle = new Rectangle(regionRectangle.X + 1, regionRectangle.Y + 1, regionRectangle.Width, regionRectangle.Height);
        borderRectangle0Based = new Rectangle(0, 0, borderRectangle.Width, borderRectangle.Height);
        Timer = new Stopwatch();
        RecordResetEvent = new ManualResetEvent(false);
    }


    public void Dispose()
    {
        if (RecordResetEvent != null)
        {
            RecordResetEvent.Dispose();
        }
    }
}
