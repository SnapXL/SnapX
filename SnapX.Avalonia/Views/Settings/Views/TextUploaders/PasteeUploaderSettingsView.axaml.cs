using Avalonia.Controls;
using Avalonia.Interactivity;
using SnapX.Core.Upload;
using SnapX.Core.Upload.Text;
using SnapX.Core.Utils;

namespace SnapX.Avalonia.Views.Settings.Views.TextUploaders;

public partial class PasteeUploaderSettingsView : UserControl
{
    public PasteeUploaderSettingsView()
    {
        InitializeComponent();
    }

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);

        if (DataContext is not Paste_ee) return;
        var config = SnapX.Core.SnapXL.UploadersConfig;

        PasswordTextBox.Text = config.Paste_eeUserKey;
        PasswordTextBox.TextChanged += (s, ev) =>
        {
            config.Paste_eeUserKey = PasswordTextBox.Text;
        };

    }

    private void UserKeyButton_OnClick(object? Sender, RoutedEventArgs E)
    {
        URLHelpers.OpenURL($"https://paste.ee/account/api/authorize/{APIKeys.Paste_eeApplicationKey}");
    }
}
