
using Avalonia.Controls;
using SnapX.Core.Upload.Text;

namespace SnapX.Avalonia.Views.Settings.Views.TextUploaders;

public partial class OneTimeSecretUploadSettingsView : UserControl
{
    public OneTimeSecretUploadSettingsView()
    {
        InitializeComponent();
    }

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);

        if (DataContext is not OneTimeSecret) return;
        var config = SnapX.Core.SnapXL.UploadersConfig;

        txtEmail.Text = config.OneTimeSecretAPIUsername;
        txtEmail.TextChanged += (Sender, Args) => config.OneTimeSecretAPIUsername = txtEmail.Text;
        txtAPIKey.Text = config.OneTimeSecretAPIKey;
        txtAPIKey.TextChanged += (Sender, Args) => config.OneTimeSecretAPIKey = txtAPIKey.Text;
    }
}
