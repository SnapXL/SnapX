
using Avalonia.Controls;
using SnapX.Core.Upload.Text;

namespace SnapX.Avalonia.Views.Settings.Views.TextUploaders;

public partial class HastebinUploaderSettingsView : UserControl
{
    public HastebinUploaderSettingsView()
    {
        InitializeComponent();
    }

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);

        if (DataContext is not Hastebin) return;
        var config = SnapX.Core.SnapXL.UploadersConfig;
        cbUseFileExtension.IsChecked = config.HastebinUseFileExtension;
        cbUseFileExtension.IsCheckedChanged += (Sender, Args) => config.HastebinUseFileExtension = cbUseFileExtension.IsChecked ?? false;

        txtSyntaxHightlight.Text = config.HastebinSyntaxHighlighting;
        txtSyntaxHightlight.TextChanged += (Sender, Args) => config.HastebinSyntaxHighlighting = txtSyntaxHightlight.Text;
        txtCustomURL.Text = config.HastebinCustomDomain;
        txtCustomURL.TextChanged += (Sender, Args) => config.HastebinCustomDomain = txtCustomURL.Text;
    }
}
