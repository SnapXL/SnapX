using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace SnapX.Core.Upload.Custom;

public class HeaderItem : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    public string Key
    {
        get;
        set
        {
            if (field == value)
                return;
            field = value;
            OnPropertyChanged();
        }
    } = "";

    public string Value
    {
        get;
        set
        {
            if (field == value)
                return;
            field = value;
            OnPropertyChanged();
        }
    } = "";

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
