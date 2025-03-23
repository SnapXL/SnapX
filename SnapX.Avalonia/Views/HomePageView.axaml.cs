using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using SnapX.Avalonia.ViewModels;
using SnapX.CommonUI;
using SnapX.Core;
using SnapX.Core.Job;

namespace SnapX.Avalonia;

public partial class HomePageView : UserControl
{
    public HomePageView()
    {
        // DataContext = this;
        InitializeComponent();
    }
    private void OnSizeChanged(object sender, SizeChangedEventArgs e)
    {
        var width = e.NewSize.Width;
        var height = e.NewSize.Height;

        var thumb = this.FindControl<Grid>("ThumbnailView")!;

        // Calculate the number of columns and rows based on width and height
        var numColumns = (int)(width / 200);  // Assuming each cell should be 200px wide
        var numRows = (int)(height / 100);    // Assuming each cell should be 100px high

        numColumns = Math.Max(1, numColumns);
        numRows = Math.Max(1, numRows);

        thumb.ColumnDefinitions.Clear();
        for (int i = 0; i < numColumns; i++)
        {
            thumb.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        }

        thumb.RowDefinitions.Clear();
        for (int i = 0; i < numRows; i++)
        {
            thumb.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        }
    }

    private void PopupFlyoutBase_OnOpening(object? Sender, EventArgs E)
    {
        DebugHelper.WriteLine("PopupFlyoutBase_OnOpening");
    }
}
