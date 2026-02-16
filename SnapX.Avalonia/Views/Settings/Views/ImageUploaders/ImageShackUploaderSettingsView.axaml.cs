using Avalonia.Controls;
using FluentAvalonia.UI.Controls;
using SnapX.Core.Upload;
using SnapX.Core.Upload.Img;
using SnapX.Core.Utils;

namespace SnapX.Avalonia.Views.Settings.Views.ImageUploaders;

public partial class ImageShackUploaderSettingsView : UserControl
{
    public ImageShackUploaderSettingsView()
    {
        InitializeComponent();
    }

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);

        if (DataContext is not ImageShackUploader) return;
        var config = SnapX.Core.SnapXL.UploadersConfig;

        UsernameTextBox.Text = config.ImageShackSettings?.Username;
        PasswordTextBox.Text = config.ImageShackSettings?.Password;
        PublicUploadSwitch.IsChecked = config.ImageShackSettings?.IsPublic;

        UsernameTextBox.TextChanged += (s, ev) => config.ImageShackSettings?.Username = UsernameTextBox.Text;
        PasswordTextBox.TextChanged += (s, ev) => config.ImageShackSettings?.Password = PasswordTextBox.Text;

        LoginButton.Click += async (s, ev) =>
        {
            var imageShackUploader = new ImageShackUploader(APIKeys.ImageShackKey, config.ImageShackSettings);

            try
            {
                var success = await Task.Run(imageShackUploader.GetAccessToken);

                var dialog = new ContentDialog
                {
                    Title = success ? "Success" : "Login Failed",
                    Content = success ? "Login successful!" : "Please check your username and password.",
                    CloseButtonText = "OK",
                    DefaultButton = ContentDialogButton.Close
                };

                var topLevel = TopLevel.GetTopLevel(this);
                if (topLevel != null)
                {
                    await dialog.ShowAsync(topLevel);
                }
                else
                {
                    await dialog.ShowAsync();
                }
            }
            catch (Exception ex)
            {
                ContentDialog errorDialog = new ContentDialog
                {
                    Title = "Error",
                    Content = ex.ToString(),
                    CloseButtonText = "OK"
                };
                var topLevel = TopLevel.GetTopLevel(this);
                if (topLevel != null)
                {
                    await errorDialog.ShowAsync(topLevel);
                }
                else
                {
                    await errorDialog.ShowAsync();
                }
            }
        };

        OpenImagesButton.Click += (s, ev) => URLHelpers.OpenURL("https://imageshack.com/my/images");
        OpenProfileButton.Click += (s, ev) =>
        {
            if (!string.IsNullOrEmpty(config.ImageShackSettings?.Username))
                URLHelpers.OpenURL($"https://imageshack.com/user/{config.ImageShackSettings?.Username}");
            else UsernameTextBox.Focus();
        };

        PublicUploadSwitch.IsCheckedChanged += (s, ev) =>
            config.ImageShackSettings?.IsPublic = PublicUploadSwitch.IsChecked ?? false;



    }
}
