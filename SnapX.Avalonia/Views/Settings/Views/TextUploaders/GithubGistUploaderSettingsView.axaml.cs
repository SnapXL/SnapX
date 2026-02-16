using Avalonia.Controls;
using Avalonia.Input.Platform;
using Avalonia.Interactivity;
using Avalonia.Media;
using FluentAvalonia.UI.Controls;
using SnapX.Core.Upload;
using SnapX.Core.Upload.OAuth;
using SnapX.Core.Upload.Text;
using SnapX.Core.Utils;
using SnapX.Core.Utils.Extensions;

namespace SnapX.Avalonia.Views.Settings.Views.TextUploaders;

public partial class GithubGistUploaderSettingsView : UserControl
{
    public GithubGistUploaderSettingsView()
    {
        InitializeComponent();
    }

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);

        if (DataContext is not GitHubGist) return;
        var config = SnapX.Core.SnapXL.UploadersConfig;
        CalculateAccessibility();
        cbGistUseRawURL.IsChecked = config.GistRawURL;
        cbGistUseRawURL.IsCheckedChanged += (Sender, Args) => config.GistRawURL = cbGistUseRawURL.IsChecked ?? false;
        cbGistPublishPublic.IsChecked = config.GistPublishPublic;
        cbGistPublishPublic.IsCheckedChanged += (Sender, Args) => config.GistRawURL = cbGistPublishPublic.IsChecked ?? false;

        txtGistCustomURL.Text = config.GistCustomURL;
        txtGistCustomURL.TextChanged += (Sender, Args) => config.GistCustomURL = txtGistCustomURL.Text;
    }

    private async void ConnectButton_OnClick(object? Sender, RoutedEventArgs E)
    {
        var control = Sender as Control;
        var oauth = new OAuth2Info(APIKeys.GitHubID, APIKeys.GitHubSecret);
        var gist = new GitHubGist(oauth);
        var config = SnapX.Core.SnapXL.UploadersConfig;
        config.GistOAuth2Info = oauth;
        DataContext = gist;
        var url = await Task.Run(gist.GetAuthorizationURL);
        URLHelpers.OpenURL(url);
    }
    void CalculateAccessibility()
    {
        var Config = SnapX.Core.SnapXL.UploadersConfig;
        var oAuthSuccess = OAuth2Info.CheckOAuth(Config.GistOAuth2Info);

        ConnectButton.Content = oAuthSuccess ? "Disconnect" : "Connect...";
        LoginStatus.Text = oAuthSuccess ? "Status: Logged in." : $"Status: {OAuthLoginStatus.LoginRequired}";
        LoginStatus.Foreground = oAuthSuccess ? Brushes.LawnGreen : Brushes.Red;
    }
    private async void PasswordTextBox_OnPastingFromClipboard(object? Sender, RoutedEventArgs E)
    {
        try
        {
            var control = Sender as Control;
            var gist = control!.DataContext as GitHubGist;
            var topLevel = TopLevel.GetTopLevel(this);
            if (topLevel?.Clipboard == null) return;
            var code = await (topLevel?.Clipboard).TryGetTextAsync();
            if (string.IsNullOrWhiteSpace(code)) return;
            var config = SnapX.Core.SnapXL.UploadersConfig;
            // config.GistOAuth2Info
            var success = await Task.Run(() => gist!.GetAccessToken(code));
            if (success) config.GistOAuth2Info = gist!.AuthInfo;
            CalculateAccessibility();
            var dialog = new ContentDialog
            {
                Title = success ? "Success" : "Login Failed",
                Content = success ? "Login successful!" : "Please check your username and password.",
                CloseButtonText = "OK",
                DefaultButton = ContentDialogButton.Close
            };

            PasswordTextBox.Text = code;
            E.Handled = true;
            if (topLevel != null)
            {
                await dialog.ShowAsync(topLevel);
            }
            else
            {
                await dialog.ShowAsync();
            }
        }
        catch (Exception e)
        {
            e.ShowError();
        }
    }
}
