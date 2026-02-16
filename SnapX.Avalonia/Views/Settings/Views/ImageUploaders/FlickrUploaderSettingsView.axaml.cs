
using Avalonia.Controls;
using Avalonia.Media;
using FluentAvalonia.UI.Controls;
using SnapX.Core;
using SnapX.Core.Upload;
using SnapX.Core.Upload.Img;
using SnapX.Core.Upload.OAuth;
using SnapX.Core.Utils;
using SnapX.Core.Utils.Extensions;

namespace SnapX.Avalonia.Views.Settings.Views.ImageUploaders;

public partial class FlickrUploaderSettingsView : UserControl
{
    public FlickrUploaderSettingsView()
    {
        InitializeComponent();
    }

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);

        if (DataContext is not FlickrUploader) return;
        var Config = SnapX.Core.SnapXL.UploadersConfig;
        AuthCodeTextBox.Text = Config.FlickrOAuthInfo?.AuthVerifier;
        DirectLinkSwitch.IsChecked = Config.FlickrSettings?.DirectLink;

        CalculateAccessibility();
        AuthCodeTextBox.TextChanged += (Sender, Args) => { CalculateAccessibility(); };

        ConnectButton.Click += async (s, ev) =>
        {
            try
            {
                var oauth = new OAuthInfo(APIKeys.FlickrKey, APIKeys.FlickrSecret);
                // In development, this hanged Avalonia without Task.Run! Cooool!
                var url = await Task.Run(() => new FlickrUploader(oauth).GetAuthorizationURL());

                if (!string.IsNullOrEmpty(url))
                {
                    Config.FlickrOAuthInfo = oauth;
                    URLHelpers.OpenURL(url);
                    DebugHelper.WriteLine("FlickrAuthOpen - Authorization URL is opened: " + url);
                }
                else
                {
                    DebugHelper.WriteLine("FlickrAuthOpen - Authorization URL is empty.");
                }
            }
            catch (Exception ex)
            {
                ex.ShowError();
            }
        };
        CompleteAuthButton.Click += async (s, ev) =>
        {
            try
            {
                var code = AuthCodeTextBox.Text;
                if (string.IsNullOrWhiteSpace(code) || Config.FlickrOAuthInfo == null) return;
                var result = await Task.Run(() => new FlickrUploader(Config.FlickrOAuthInfo).GetAccessToken(code));

                var dialog = new ContentDialog
                {
                    Title = result ? "Success" : "Login Failed",
                    Content = result ? "Login Successful" : "Login Failed",
                    CloseButtonText = "OK",
                    DefaultButton = ContentDialogButton.Close
                };

                LoginStatus.Content = result ? $"Status: {OAuthLoginStatus.LoginSuccessful}" : $"Status: {OAuthLoginStatus.LoginFailed}";
                LoginStatus.Foreground = result ? Brushes.LawnGreen : Brushes.Red;
                AuthCodeTextBox.Text = string.Empty;

                var topLevel = TopLevel.GetTopLevel(this);
                if (topLevel != null)
                {
                    await dialog.ShowAsync();
                }
            }
            catch (Exception ex)
            {
                var errorDialog = new ContentDialog
                {
                    Title = "Error",
                    Content = ex.Message,
                    CloseButtonText = "OK"
                };

                var topLevel = TopLevel.GetTopLevel(this);
                if (topLevel != null)
                {
                    await errorDialog.ShowAsync();
                }
            }
        };
        DisconnectButton.Click += (Sender, Args) =>
        {
            Config.FlickrOAuthInfo = null;
            CalculateAccessibility();
        };

        DirectLinkSwitch.IsCheckedChanged += (s, ev) =>
            Config.FlickrSettings?.DirectLink = DirectLinkSwitch.IsChecked ?? false;



    }

    void CalculateAccessibility()
    {
        var Config = SnapX.Core.SnapXL.UploadersConfig;
        var oAuthSuccess = OAuthInfo.CheckOAuth(Config.FlickrOAuthInfo);

        DisconnectButton.IsEnabled = oAuthSuccess;
        LoginStatus.Content = oAuthSuccess ? "Status: Logged in." : $"Status: {OAuthLoginStatus.LoginRequired}";
        LoginStatus.Foreground = oAuthSuccess ? Brushes.LawnGreen : Brushes.Red;
        CompleteAuthButton.IsEnabled = !string.IsNullOrWhiteSpace(AuthCodeTextBox.Text);
    }
}
