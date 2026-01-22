using Avalonia.Controls.Notifications;
using Avalonia.Controls.Primitives;
using CommunityToolkit.Mvvm.Messaging;
using FluentAvalonia.UI.Windowing;

namespace SnapX.Avalonia.Views.Settings;

public record NotificationMessage(string Title, string Message, NotificationType Type);

public partial class SettingsWindow : AppWindow
{
    public SettingsWindow()
    {
        InitializeComponent();
        WeakReferenceMessenger.Default.Register<NotificationMessage>(
            this,
            (r, m) =>
            {
                NotificationManager?.Show(new Notification(m.Title, m.Message, m.Type));
            }
        );
    }

    private void SettingsWindowInit(object? Sender, EventArgs E)
    {
        var activeScreen = Screens.ScreenFromWindow(this);
        var screenWidth = activeScreen?.Bounds.Width ?? 1920;
        var screenHeight = activeScreen?.Bounds.Height ?? 1080;
        Width = screenWidth * 0.6;
        Height = screenHeight - 100;
    }

    private WindowNotificationManager NotificationManager;

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        NotificationManager = new WindowNotificationManager(this)
        {
            Position = NotificationPosition.BottomRight,
            MaxItems = 3,
        };
    }
}

