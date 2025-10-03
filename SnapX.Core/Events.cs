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

public class NeedRegionCaptureEvent
{

}
public class EventAggregator
{
    private readonly List<Tuple<Type, Action<object>>> _subscriptions = [];

    public void Subscribe<TEvent>(Action<TEvent> action)
    {
        _subscriptions.Add(Tuple.Create<Type, Action<object>>(typeof(TEvent), (o) => action((TEvent)o)));
    }

    public void Publish<TEvent>(TEvent @event)
    {
        foreach (var subscription in _subscriptions.Where(s => s.Item1 == typeof(TEvent)))
        {
            subscription.Item2(@event);
        }
    }
}
