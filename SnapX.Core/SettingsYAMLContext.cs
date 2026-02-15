using System.Drawing;
using SnapX.Core.History;
using SnapX.Core.Hotkey;
using SnapX.Core.Indexer;
using SnapX.Core.Job;
using SnapX.Core.Media;
using SnapX.Core.ScreenCapture;
using SnapX.Core.ScreenCapture.Helpers;
using SnapX.Core.ScreenCapture.ScreenRecording;
using SnapX.Core.Upload;
using SnapX.Core.Upload.Custom;
using SnapX.Core.Upload.File;
using SnapX.Core.Upload.Img;
using SnapX.Core.Upload.OAuth;
using SnapX.Core.Upload.Text;
using SnapX.Core.Upload.URL;
using SnapX.Core.Utils.Miscellaneous;
using SnapX.Core.Watch;
using YamlDotNet.Serialization;

namespace SnapX.Core;

[YamlStaticContext]

[YamlSerializable(typeof(ApplicationConfig))]
[YamlSerializable(typeof(UploadersConfig))]
[YamlSerializable(typeof(HotkeysConfig))]
[YamlSerializable(typeof(ImageShackOptions))]
[YamlSerializable(typeof(OAuth2Info))]
[YamlSerializable(typeof(TaskSettings))]
[YamlSerializable(typeof(ExternalProgram))]
[YamlSerializable(typeof(UploaderFilter))]
[YamlSerializable(typeof(WatchFolderSettings))]

[YamlSerializable(typeof(TaskSettingsGeneral))]
[YamlSerializable(typeof(TaskSettingsImage))]
[YamlSerializable(typeof(TaskSettingsAdvanced))]
[YamlSerializable(typeof(TaskSettingsCapture))]
[YamlSerializable(typeof(TaskSettingsTools))]
[YamlSerializable(typeof(TaskSettingsUpload))]
[YamlSerializable(typeof(WindowState))]
[YamlSerializable(typeof(FFmpegOptions))]
[YamlSerializable(typeof(RegionCaptureOptions))]
[YamlSerializable(typeof(SnapSize))]
[YamlSerializable(typeof(BackgroundGradient))]
[YamlSerializable(typeof(OCROptions))]
[YamlSerializable(typeof(ServiceLink))]
[YamlSerializable(typeof(PinToScreenOptions))]
[YamlSerializable(typeof(IndexerSettings))]
[YamlSerializable(typeof(ImageBeautifierOptions))]
[YamlSerializable(typeof(ImageCombinerOptions))]
[YamlSerializable(typeof(VideoConverterOptions))]
[YamlSerializable(typeof(VideoThumbnailOptions))]
[YamlSerializable(typeof(BorderlessWindowSettings))]
[YamlSerializable(typeof(Point))]
[YamlSerializable(typeof(Rectangle))]
[YamlSerializable(typeof(Size))]
[YamlSerializable(typeof(QuickTaskInfo))]
[YamlSerializable(typeof(Theme))]
[YamlSerializable(typeof(ProxyInfo))]
[YamlSerializable(typeof(HistorySettings))]
[YamlSerializable(typeof(ImageHistorySettings))]
[YamlSerializable(typeof(HistorySettings))]
[YamlSerializable(typeof(HotkeySettings))]
[YamlSerializable(typeof(HotkeyInfo))]
[YamlSerializable(typeof(BoxFileEntry))]
[YamlSerializable(typeof(GoogleDriveSharedDrive))]
[YamlSerializable(typeof(OAuthInfo))]
[YamlSerializable(typeof(FlickrSettings))]
[YamlSerializable(typeof(OAuthUserInfo))]
[YamlSerializable(typeof(OAuth2Token))]
[YamlSerializable(typeof(OAuth2ProofKey))]
[YamlSerializable(typeof(CheveretoUploader))]
[YamlSerializable(typeof(PastebinSettings))]
[YamlSerializable(typeof(FTPAccount))]
[YamlSerializable(typeof(OneDriveFileInfo))]
[YamlSerializable(typeof(BoxFileInfo))]
[YamlSerializable(typeof(LocalhostAccount))]
[YamlSerializable(typeof(AmazonS3Settings))]
[YamlSerializable(typeof(PushbulletSettings))]
[YamlSerializable(typeof(LobFileSettings))]
[YamlSerializable(typeof(PomfUploader))]
[YamlSerializable(typeof(PlikSettings))]
[YamlSerializable(typeof(KuttSettings))]
[YamlSerializable(typeof(CustomUploaderItem))]
// [YamlSerializable(typeof(HttpMethod))]

// [YamlSerializable(typeof(SettingsBase<ApplicationConfig>))]
// [YamlSerializable(typeof(SettingsBase<HotkeysConfig>))]
// [YamlSerializable(typeof(SettingsBase<UploadersConfig>))]
// [YamlSerializable(typeof(object[]))]
// [YamlSerializable(typeof(Array))]
// [YamlSerializable(typeof(int))]
// [YamlSerializable(typeof(uint))]
// [YamlSerializable(typeof(bool))]
// [YamlSerializable(typeof(Keys))]
// [YamlSerializable(typeof(Dictionary<string, string>))]
// [YamlSerializable(typeof(Dictionary<string, string?>))]
// ReSharper disable once RedundantExtendsListEntry
public partial class SettingsYAMLContext : StaticContext;
