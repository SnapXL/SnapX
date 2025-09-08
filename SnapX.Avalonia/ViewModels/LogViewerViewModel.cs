using CommunityToolkit.Mvvm.ComponentModel;

namespace SnapX.Avalonia.ViewModels;

public partial class LogViewerViewModel : ViewModelBase
{
    [ObservableProperty] public string startupText = $"Startup path: {SnapX.Core.SnapX.ShortenPath(Environment.CurrentDirectory)}";
}
