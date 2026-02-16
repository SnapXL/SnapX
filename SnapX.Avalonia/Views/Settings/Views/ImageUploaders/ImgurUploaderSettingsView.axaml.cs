using Avalonia.Controls;
using SnapX.Core.Upload;
using SnapX.Core.Upload.Img;

namespace SnapX.Avalonia.Views.Settings.Views.ImageUploaders;

public partial class ImgurUploaderSettingsView : UserControl
{
    public ImgurUploaderSettingsView()
    {
        InitializeComponent();
    }

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);

        if (DataContext is not Imgur imgur) return;
        var config = SnapX.Core.SnapXL.UploadersConfig;

        // --- 1. MANUAL PULL ---

        // Account Type
        AccountTypeCombo.ItemsSource = Enum.GetValues<AccountType>();
        AccountTypeCombo.SelectedItem = config.ImgurAccountType;

        // Toggles
        DirectLinkSwitch.IsChecked = config.ImgurDirectLink;
        UseGifvSwitch.IsChecked = config.ImgurUseGIFV;
        UploadSelectedAlbumSwitch.IsChecked = config.ImgurUploadSelectedAlbum;

        // Enums
        ThumbnailCombo.ItemsSource = Enum.GetValues<ImgurThumbnailType>();
        ThumbnailCombo.SelectedItem = config.ImgurThumbnailType;

        // Albums (Initial Fill)
        ImgurFillAlbumList();

        // --- 2. MANUAL PUSH ---

        AccountTypeCombo.SelectionChanged += (s, ev) =>
            config.ImgurAccountType = (AccountType)(AccountTypeCombo.SelectedItem ?? AccountType.Anonymous);

        DirectLinkSwitch.IsCheckedChanged += (s, ev) =>
            config.ImgurDirectLink = DirectLinkSwitch.IsChecked ?? false;

        UseGifvSwitch.IsCheckedChanged += (s, ev) =>
            config.ImgurUseGIFV = UseGifvSwitch.IsChecked ?? false;

        UploadSelectedAlbumSwitch.IsCheckedChanged += (s, ev) =>
            config.ImgurUploadSelectedAlbum = UploadSelectedAlbumSwitch.IsChecked ?? false;

        ThumbnailCombo.SelectionChanged += (s, ev) =>
        {
            if (ThumbnailCombo.SelectedItem is ImgurThumbnailType type)
                config.ImgurThumbnailType = type;
        };

        RefreshAlbumsButton.Click += (s, ev) => ImgurFillAlbumList();
        AlbumCombo.SelectionChanged += (s, ev) =>
        {
            if (AlbumCombo.SelectedItem is ImgurAlbumData selectedAlbum)
            {
                config.ImgurSelectedAlbum = selectedAlbum;
            }
        };
    }

    private void ImgurFillAlbumList()
    {
        var config = SnapX.Core.SnapXL.UploadersConfig;

        if (config.ImgurAlbumList != null)
        {
            AlbumCombo.ItemsSource = config.ImgurAlbumList;

            if (config.ImgurSelectedAlbum != null)
            {
                AlbumCombo.SelectedItem = config.ImgurAlbumList
                    .FirstOrDefault(a => a.id == config.ImgurSelectedAlbum.id);
            }
        }
    }
}
