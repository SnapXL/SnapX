namespace SnapX.Core.SharpCapture.Linux.DBus;

public class PropertyChanges<TProperties>(TProperties Properties, string[] Invalidated, string[] Changed)
{
    public TProperties Properties { get; } = Properties;
    public string[] Invalidated { get; } = Invalidated;
    public string[] Changed { get; } = Changed;
    public bool HasChanged(string property) => Array.IndexOf(Changed, property) != -1;
    public bool IsInvalidated(string property) => Array.IndexOf(Invalidated, property) != -1;
}
