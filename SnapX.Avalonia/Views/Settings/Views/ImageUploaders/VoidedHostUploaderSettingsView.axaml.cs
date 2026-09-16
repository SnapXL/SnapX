using Avalonia.Controls;
using Avalonia.Interactivity;
using SnapX.Core.Upload.Img;
using SnapX.Core.Utils;

namespace SnapX.Avalonia.Views.Settings.Views.ImageUploaders;

public partial class VoidedHostUploaderSettingsView : UserControl
{
    public VoidedHostUploaderSettingsView()
    {
        InitializeComponent();
    }

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);

        if (DataContext is not (VoidedHostUploader or SnapX.Core.Upload.Text.VoidedHostTextUploader or SnapX.Core.Upload.File.VoidedHostFileUploader)) return;
        var config = SnapX.Core.SnapXL.UploadersConfig;

        bool guestKeyOk = VoidedHostUploader.IsGuestUploadKeyConfigured();
        if (!guestKeyOk && config.VoidedHostUseGuest)
        {
            config.VoidedHostUseGuest = false;
        }

        cbUseGuest.IsChecked = guestKeyOk && config.VoidedHostUseGuest;
        cbUseGuest.IsEnabled = guestKeyOk;
        txtUploadKey.Text = config.VoidedHostUploadKey ?? "";

        UpdateControlStates();

        cbUseGuest.IsCheckedChanged += (_, _) =>
        {
            config.VoidedHostUseGuest = cbUseGuest.IsChecked == true;
            UpdateControlStates();
        };

        txtUploadKey.TextChanged += (_, _) =>
        {
            config.VoidedHostUploadKey = txtUploadKey.Text ?? "";
        };
    }

    private void UpdateControlStates()
    {
        bool guestKeyOk = VoidedHostUploader.IsGuestUploadKeyConfigured();
        bool guest = guestKeyOk && cbUseGuest.IsChecked == true;

        txtUploadKey.IsEnabled = !guest;
        UploadKeyItem.IsEnabled = !guest;
        ManageKeysItem.IsEnabled = !guest;
    }

    private void RegisterButton_OnClick(object? sender, RoutedEventArgs e)
    {
        URLHelpers.OpenURL(VoidedHostUploader.SnapXSetupRegisterUrl);
    }

    private void ManageKeysButton_OnClick(object? sender, RoutedEventArgs e)
    {
        URLHelpers.OpenURL(VoidedHostUploader.UploadKeySettingsUrl);
    }
}
