using SnapX.Core;
using Gtk;
using SnapX.Core.Utils;

namespace SnapX.GTK4.Views;


public class MainWindow : Window
{
    private MainWindow(Gtk.Builder builder, string name) : base(new Gtk.Internal.ApplicationWindowHandle(builder.GetPointer(name), false))
    {
        builder.Connect(this);

        // Do any initialization, or connect signals here.
        var welcomeLabel = builder.GetObject("Label") as Gtk.Label;
        welcomeLabel.SetLabel(Lang.WelcomeMessage);
    }


    public MainWindow(Gtk.Application application) : this(new Gtk.Builder("MainWindow.ui"), "main_window")
    {
        Application = application;
    }

    private void OnButtonClicked(object sender, EventArgs e)
    {
        Console.WriteLine("Button Clicked!");
    }
}
