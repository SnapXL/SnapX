using System.Reflection;
using System.Resources;

namespace SnapX.Core.Utils;

public static class Lang
{
    public static readonly ResourceManager ResourceManager = new("SnapX.Core.Localization.Resources", Assembly.GetExecutingAssembly());
    public static string Get(string key) => ResourceManager.GetString(key) ?? key;
    public static string UnhandledException => Get("UnhandledException");
    public static string WelcomeMessage => Get("WelcomeMessage");
    public static string AboutSnapX => Get("AboutSnapX");
    public static string UploadToAmazonS3Failed => Get("UploadToAmazonS3Failed");
    public static string SnapXFailedToStart => Get("SnapXFailedToStart");
    public static string ReportErrorToDeveloper => Get("ReportErrorToDeveloper");
    public static string CreateGitHubIssue => Get("CreateGitHubIssue");
    public static string CopyErrorToClipboard => Get("CopyErrorToClipboard");
    public static string EditWithSnapX => Get("EditWithSnapX");
    public static string UploadWithSnapX => Get("UploadWithSnapX");
    public static string UploadManagerUploadFile => Get("UploadManagerUploadFile");
    #region UI strings
    public static string UI_Dropdown_Region => Get("UI_Dropdown_Region");
    public static string UI_Dropdown_RegionLight => Get("UI_Dropdown_RegionLight");
    public static string UI_Dropdown_RegionTransparent => Get("UI_Dropdown_RegionTransparent");
    public static string UI_Dropdown_Window => Get("UI_Dropdown_Window");
    public static string UI_Dropdown_Monitor => Get("UI_Dropdown_Monitor");
    public static string UI_Monitor_DisplayName(string name, int index, string resolution)
        => string.Format(Get("UI_Monitor_DisplayName"), name, index, resolution);
    public static string UI_Capture_Fullscreen => Get("UI_Capture_Fullscreen");
    #endregion

}
