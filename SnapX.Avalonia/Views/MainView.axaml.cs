using Avalonia.Controls;
using Avalonia.Interactivity;

namespace SnapX.Avalonia.Views;

public partial class MainView : UserControl
{
    public MainView()
    {
        InitializeComponent();
    }
    private void ClickAboutButton(object? Sender, RoutedEventArgs E) => new AboutWindow().Show();

}
