using AsyncImageLoader;
using Avalonia.Controls;
using Avalonia.Media;
using FluentAvalonia.UI.Controls;
using SnapX.Core.Upload;
using SnapX.Core.Upload.Img;
using SnapX.Core.Upload.OAuth;
using SnapX.Core.Utils.Extensions;

namespace SnapX.Avalonia.Views.Settings.Views.ImageUploaders;

public partial class PicasaUploaderSettingsView : UserControl
{
    public PicasaUploaderSettingsView()
    {
        InitializeComponent();
    }

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);

        if (DataContext is not GooglePhotos GooglePhotos) return;
        var config = SnapX.Core.SnapXL.UploadersConfig;

        PublicUploadSwitch.IsChecked = config.GooglePhotosIsPublic;
        txtPicasaAlbumID.Text = config.GooglePhotosAlbumID;
        txtPicasaAlbumID.TextChanged += (Sender, Args) => config.GooglePhotosAlbumID = txtPicasaAlbumID.Text;
        UpdateUserInfo(config.GooglePhotosUserInfo);

        ConnectButton.Click += async (s, ev) =>
        {
            try
            {
                var oauth = new OAuth2Info(config.GooglePhotosOAuth2Info?.Client_ID, config.GooglePhotosOAuth2Info?.Client_Secret);
                IOAuth2Loopback oauthLoopback = new GooglePhotos(oauth).OAuth2;
                var listenerView = new OAuth2ListenerView();
                var dialog = new ContentDialog
                {
                    Title = "External Authentication",
                    Content = listenerView,
                    FullSizeDesired = false
                };

                listenerView.AuthenticationCompleted += (s, result) =>
                {
                    config.GooglePhotosOAuth2Info = result.Info;
                    config.GooglePhotosUserInfo = result.User;
                    UpdateUserInfo();
                    dialog.Hide();
                };

                listenerView.AuthenticationCancelled += (s, e) => dialog.Hide();

                await listenerView.StartListeningAsync(oauthLoopback);
                if (TopLevel.GetTopLevel(this) is var topLevel && topLevel != null)
                {
                    await dialog.ShowAsync();
                }

            }
            catch (Exception ex)
            {
                ex.ShowError();
            }
        };

        DisconnectButton.Click += (s, ev) =>
        {
            config.GooglePhotosOAuth2Info = null;
            config.GooglePhotosUserInfo = null;

            UpdateUserInfo();
        };

        PublicUploadSwitch.IsCheckedChanged += (s, ev) =>
        {
            config.GooglePhotosIsPublic = PublicUploadSwitch.IsChecked ?? false;
        };

        btnGooglePhotosCreateAlbum.Click += async (s, ev) =>
        {
            var albumName = txtGooglePhotosCreateAlbumName.Text;
            try
            {
                var album = await Task.Run(() => GooglePhotos.CreateAlbum(albumName));
                await RefreshAlbumList(GooglePhotos);
            }
            catch (Exception ex)
            {
                ex.ShowError();
            }
        };
        btnPicasaRefreshAlbumList.Click += async (s, ev) =>
        {
            await RefreshAlbumList(GooglePhotos);
        };
    }

    private async Task RefreshAlbumList(GooglePhotos GooglePhotos)
    {
        try
        {
            var list = await Task.Run(GooglePhotos.GetAlbumList);
            lvPicasaAlbumList.ItemsSource = list;
        }
        catch (Exception ex)
        {
            ex.ShowError();
        }
    }
    private void UpdateUserInfo(OAuthUserInfo? user = null)
    {
        var config = SnapX.Core.SnapXL.UploadersConfig;
        var oAuthSuccess = OAuth2Info.CheckOAuth(config.GooglePhotosOAuth2Info);
        if (user is null) user = config.GooglePhotosUserInfo;

        if (oAuthSuccess && user != null)
        {
            UserNameText.Text = user.name;
            UserIdText.Text = $"ID: {user.sub}";
            LoginStatusLabel.Text = "Status: Logged In";
            LoginStatusLabel.Foreground = Brushes.LawnGreen;

            ImageLoader.SetSource(UserPicture, user.picture);
        }
        else
        {
            UserNameText.Text = "Not Logged In";
            UserIdText.Text = "ID: ---";
            LoginStatusLabel.Text = $"Status: {OAuthLoginStatus.LoginRequired}";
            LoginStatusLabel.Foreground = Brushes.Red;

            ImageLoader.SetSource(UserPicture, null);
        }
        DisconnectButton.IsEnabled = oAuthSuccess;

    }
}
