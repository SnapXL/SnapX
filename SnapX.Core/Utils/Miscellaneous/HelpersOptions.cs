// SPDX-License-Identifier: GPL-3.0-or-later


using SixLabors.ImageSharp;
using Xdg.Directories;

namespace SnapX.Core.Utils.Miscellaneous;

public static class HelpersOptions
{
    public const int RecentColorsMax = 32;

    public static ProxyInfo CurrentProxy { get; set; } = new ProxyInfo();
    public static bool AcceptInvalidSSLCertificates { get; set; } = false;
    public static bool DefaultCopyImageFillBackground { get; set; } = true;
    public static bool UseAlternativeClipboardCopyImage { get; set; } = false;
    public static bool UseAlternativeClipboardGetImage { get; set; } = false;
    public static bool RotateImageByExifOrientationData { get; set; } = true;
    public static string BrowserPath { get; set; } = "";
    public static List<Color> RecentColors { get; set; } = [];
    public static string LastSaveDirectory { get; set; } = "";
    public static bool URLEncodeIgnoreEmoji { get; set; } = false;
    public static Dictionary<string, string> ShareXUserFolders { get; set; } = new()
    {
        { "Desktop", UserDirectory.DesktopDir },
        { "Music", UserDirectory.MusicDir },
        { "Pictures", UserDirectory.PicturesDir },
        { "Videos", UserDirectory.VideosDir },
        { "Documents", UserDirectory.DocumentsDir },
        { "Downloads", UserDirectory.DownloadDir},
        { "Templates", UserDirectory.TemplatesDir}
    };
    public static bool DevMode { get; set; } = false;
}
