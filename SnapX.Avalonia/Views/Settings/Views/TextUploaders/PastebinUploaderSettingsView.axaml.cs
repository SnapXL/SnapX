using Avalonia.Controls;
using Avalonia.Media;
using FluentAvalonia.UI.Controls;
using SnapX.Core.Upload;
using SnapX.Core.Upload.Text;

namespace SnapX.Avalonia.Views.Settings.Views.TextUploaders;

public partial class PastebinUploaderSettingsView : UserControl
{
    public PastebinUploaderSettingsView()
    {
        InitializeComponent();
    }

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);

        if (DataContext is not Pastebin Pastebin) return;
        var config = SnapX.Core.SnapXL.UploadersConfig;

        UsernameTextBox.Text = config.PastebinSettings?.Username;
        PasswordTextBox.Text = config.PastebinSettings?.Password;
        cbPastebinRaw.IsChecked = config.PastebinSettings?.RawURL;
        txtPastebinTitle.Text = config.PastebinSettings?.Title;
        cbPastebinSyntax.ItemsSource = Pastebin.GetSyntaxList();
        cbPastebinSyntax.SelectedItem = config.PastebinSettings?.TextFormat;
        cbPastebinExpiration.ItemsSource = Enum.GetValues<PastebinExpiration>();
        cbPastebinExpiration.SelectedItem = config.PastebinSettings?.Expiration;
        cbPastebinPrivacy.ItemsSource = Enum.GetValues<PastebinPrivacy>();
        cbPastebinPrivacy.SelectedItem = config.PastebinSettings?.Exposure;

        UpdateAccessibility();


        UsernameTextBox.TextChanged += (s, ev) =>
        {
            config.PastebinSettings?.Username = UsernameTextBox.Text;
            UpdateAccessibility();
        };
        PasswordTextBox.TextChanged += (s, ev) =>
        {
            config.PastebinSettings?.Password = PasswordTextBox.Text;
            UpdateAccessibility();
        };
        txtPastebinTitle.TextChanged += (Sender, Args) => config.PastebinSettings?.Title = txtPastebinTitle.Text;
        cbPastebinSyntax.SelectionChanged += (Sender, Args) =>
            config.PastebinSettings?.TextFormat = cbPastebinSyntax.SelectedItem as string;
        cbPastebinExpiration.SelectionChanged += (sender, args) =>
        {
            if (cbPastebinExpiration.SelectedItem is string selectedText &&
                Enum.TryParse<PastebinExpiration>(selectedText, out var result))
            {
                config.PastebinSettings?.Expiration = result;
            }
        };

        cbPastebinRaw.IsCheckedChanged += (s, ev) =>
            config.PastebinSettings?.RawURL = cbPastebinRaw.IsChecked ?? false;
        LoginButton.Click += async (s, ev) =>
        {
            var pasteBinUploader = new Pastebin(APIKeys.PastebinKey, config.PastebinSettings);

            try
            {
                var success = await Task.Run(pasteBinUploader.Login);

                var dialog = new ContentDialog
                {
                    Title = success ? "Success" : "Login Failed",
                    Content = success ? "Login successful!" : "Please check your username and password.",
                    CloseButtonText = "OK",
                    DefaultButton = ContentDialogButton.Close
                };
                LoginStatus.Content = success ? $"Status: {OAuthLoginStatus.LoginSuccessful}" : $"Status: {OAuthLoginStatus.LoginFailed}";


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
    }

    void UpdateAccessibility()
    {
        var config = SnapX.Core.SnapXL.UploadersConfig;
        var loggedIn = config.PastebinSettings is not null && !string.IsNullOrEmpty(config.PastebinSettings.UserKey);
        LoginStatus.Content = loggedIn ? $"Status: {OAuthLoginStatus.LoginSuccessful}" : $"Status: {OAuthLoginStatus.LoginFailed}";
        LoginStatus.Foreground = loggedIn ? Brushes.LawnGreen : Brushes.Red;
        LoginButton.IsEnabled = !string.IsNullOrWhiteSpace(UsernameTextBox.Text) && !string.IsNullOrWhiteSpace(PasswordTextBox.Text);

    }
}
