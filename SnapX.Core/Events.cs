using SixLabors.ImageSharp;
using SnapX.Core.Job;
using Xdg.Directories;

namespace SnapX.Core;

public class NeedFileOpenerEvent
{
    public string Directory { get; set; } = UserDirectory.PicturesDir;
    public string? FileName { get; set; }
    public List<string>? AcceptedExtensions { get; set; }
    public string? Title { get; set; } = SnapX.AppName;
    public bool Multiselect { get; set; } = false;
    public bool FolderPicker { get; set; }
    public TaskSettings TaskSettings { get; set; }
}

public record ErrorMessageEvent(Exception Exception, string Context, bool FullError);

public class NeedMainWindowHandle
{
    // The subscriber will fill this property
    public IntPtr ResultHandle { get; set; } = IntPtr.Zero;
}

public class NeedRegionCaptureEvent { }

public class NeedClipboardCopyEvent
{
    public string? Text { get; set; }

    public NeedClipboardCopyEvent(string text)
    {
        Text = text;
    }
    public NeedClipboardCopyEvent(Image img)
    {
        Image = img;
    }
    public NeedClipboardCopyEvent(Image img, string? filename = null)
    {
        Image = img;
        FileName = filename;
    }


    public Image? Image { get; set; }
    public string FileName { get; set; }
    public object? CustomData { get; set; }
    public Dictionary<string, object> AdditionalFormats { get; } = new();

    public bool HasText => !string.IsNullOrEmpty(Text);
    public bool HasImage => Image != null;

    public bool Handled { get; set; }

    public void MarkAsHandled()
    {
        Handled = true;
    }
}

public class EventAggregator
{
    private readonly List<Tuple<Type, Action<object>>> _subscriptions = [];

    public void Subscribe<TEvent>(Action<TEvent> action)
    {
        _subscriptions.Add(
            Tuple.Create<Type, Action<object>>(typeof(TEvent), (o) => action((TEvent)o))
        );
    }

    public void Publish<TEvent>(TEvent @event)
    {
        foreach (var subscription in _subscriptions.Where(s => s.Item1 == typeof(TEvent)))
        {
            subscription.Item2(@event);
        }
    }
}
