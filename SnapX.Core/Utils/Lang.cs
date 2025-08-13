using System.Reflection;
using System.Resources;

namespace SnapX.Core.Utils;

public static class Lang
{
    public static readonly ResourceManager ResourceManager = new("SnapX.Core.Localization.Resources", Assembly.GetExecutingAssembly());
    private static string Get(string key) => ResourceManager.GetString(key) ?? key;
    public static string UnhandledException => Get("UnhandledException");
    public static string WelcomeMessage => Get("WelcomeMessage");
    public static string AboutSnapX => Get("AboutSnapX");
    public static string UploadToAmazonS3Failed => Get("UploadToAmazonS3Failed");
    public static string SnapXFailedToStart => Get("SnapXFailedToStart");
    public static string FailedToScreenshot => Get("FailedToScreenshot");
    public static string Error => Get("Error");
    public static string Ok => Get("Ok");
    public static string Close => Get("Close");
    public static string Processing => Get("Processing");
    public static string ReportErrorToDeveloper => Get("ReportErrorToDeveloper");
    public static string CreateGitHubIssue => Get("CreateGitHubIssue");
    public static string CopyErrorToClipboard => Get("CopyErrorToClipboard");
    public static string EditWithSnapX => Get("EditWithSnapX");
    public static string UploadWithSnapX => Get("UploadWithSnapX");
    public static string UploadManagerUploadFile => Get("UploadManagerUploadFile");
    #region UI strings
    public static string UI_Settings => Get("UI_Settings");
    public static string UI_NoFilePath => Get("UI_NoFilePath");
    public static string UI_Dropdown_Region => Get("UI_Dropdown_Region");
    public static string UI_Dropdown_RegionLight => Get("UI_Dropdown_RegionLight");
    public static string UI_Dropdown_RegionTransparent => Get("UI_Dropdown_RegionTransparent");
    public static string UI_Dropdown_Window => Get("UI_Dropdown_Window");
    public static string UI_Dropdown_Monitor => Get("UI_Dropdown_Monitor");
    public static string UI_Monitor_DisplayName(string name, int index, string resolution)
        => string.Format(Get("UI_Monitor_DisplayName"), name, index, resolution);
    public static string UI_Capture_Fullscreen => Get("UI_Capture_Fullscreen");
    public static string UI_ShowErrors => Get("UI_ShowErrors");
    public static string UI_Open => Get("UI_Open");
    public static string UI_Open_ToolTip => Get("UI_Open_ToolTip");
    public static string UI_URL => Get("UI_URL");
    public static string UI_URL_ToolTip => Get("UI_URL_ToolTip");
    public static string UI_ShortenURL => Get("UI_ShortenURL");
    public static string UI_ShortenURL_ToolTip => Get("UI_ShortenURL_ToolTip");
    public static string UI_ShortenedURL => Get("UI_ShortenedURL");
    public static string UI_ShortenedURL_ToolTip => Get("UI_ShortenedURL_ToolTip");
    public static string UI_ThumbnailURL => Get("UI_ThumbnailURL");
    public static string UI_ThumbnailURL_ToolTip => Get("UI_ThumbnailURL_ToolTip");
    public static string UI_DeletionURL => Get("UI_DeletionURL");
    public static string UI_DeletionURL_ToolTip => Get("UI_DeletionURL_ToolTip");
    public static string UI_File => Get("UI_File");
    public static string UI_File_ToolTip => Get("UI_File_ToolTip");
    public static string UI_FolderName => Get("UI_FolderName");
    public static string UI_Folder_ToolTip => Get("UI_Folder_ToolTip");
    public static string UI_ThumbnailFile => Get("UI_ThumbnailFile");
    public static string UI_Copy => Get("UI_Copy");
    public static string UI_Image => Get("UI_Image");
    public static string UI_ImageDimensions => Get("UI_ImageDimensions");
    public static string UI_Text => Get("UI_Text");
    public static string UI_ThumbnailImage => Get("UI_ThumbnailImage");
    public static string UI_FilePath => Get("UI_FilePath");
    public static string UI_FileName => Get("UI_FileName");
    public static string UI_FileNameWithExtension => Get("UI_FileNameWithExtension");
    public static string UI_Download => Get("UI_Download");
    public static string UI_Download_ToolTip => Get("UI_Download_ToolTip");
    public static string UI_Upload => Get("UI_Upload");
    public static string UI_Upload_ToolTip => Get("UI_Upload_ToolTip");
    public static string UI_OCRImage => Get("UI_OCRImage");
    public static string UI_OCRImage_ToolTip => Get("UI_OCRImage_ToolTip");
    public static string UI_EditImage => Get("UI_EditImage");
    public static string UI_EditImage_ToolTip => Get("UI_EditImage_ToolTip");
    public static string UI_BeautifyImage => Get("UI_BeautifyImage");
    public static string UI_ShareURL => Get("UI_ShareURL");
    public static string UI_ShareURL_ToolTip => Get("UI_ShareURL_ToolTip");
    public static string UI_BeautifyImage_ToolTip => Get("UI_BeautifyImage_ToolTip");
    public static string UI_AddImageEffects => Get("UI_AddImageEffects");
    public static string UI_AddImageEffects_ToolTip => Get("UI_AddImageEffects_ToolTip");
    public static string UI_RemoveTask => Get("UI_RemoveTask");
    public static string UI_RemoveTask_ToolTip => Get("UI_RemoveTask_ToolTip");
    public static string UI_DeleteFileLocally => Get("UI_DeleteFileLocally");
    public static string UI_DeleteFileLocally_ToolTip => Get("UI_DeleteFileLocally_ToolTip");
    #endregion

}
