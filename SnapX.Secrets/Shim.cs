using System.Runtime.InteropServices;
using SnapX.Core.Upload;

namespace SnapX.Secrets;

public static class Shim
{
    private static IntPtr ReturnString(string? value)
    {
        return Marshal.StringToCoTaskMemAnsi(value ?? string.Empty);
    }

    [UnmanagedCallersOnly(EntryPoint = "get_imgur_id")]
    public static IntPtr GetImgurId() => ReturnString(APIKeys.ImgurClientID);

    [UnmanagedCallersOnly(EntryPoint = "get_imgur_secret")]
    public static IntPtr GetImgurSecret() => ReturnString(APIKeys.ImgurClientSecret);

    [UnmanagedCallersOnly(EntryPoint = "get_imageshack_key")]
    public static IntPtr GetImageShackKey() => ReturnString(APIKeys.ImageShackKey);

    [UnmanagedCallersOnly(EntryPoint = "get_flickr_key")]
    public static IntPtr GetFlickrKey() => ReturnString(APIKeys.FlickrKey);

    [UnmanagedCallersOnly(EntryPoint = "get_flickr_secret")]
    public static IntPtr GetFlickrSecret() => ReturnString(APIKeys.FlickrSecret);

    [UnmanagedCallersOnly(EntryPoint = "get_photobucket_consumer_key")]
    public static IntPtr GetPhotobucketConsumerKey() => ReturnString(APIKeys.PhotobucketConsumerKey);

    [UnmanagedCallersOnly(EntryPoint = "get_photobucket_consumer_secret")]
    public static IntPtr GetPhotobucketConsumerSecret() => ReturnString(APIKeys.PhotobucketConsumerSecret);

    [UnmanagedCallersOnly(EntryPoint = "get_pastebin_key")]
    public static IntPtr GetPastebinKey() => ReturnString(APIKeys.PastebinKey);

    [UnmanagedCallersOnly(EntryPoint = "get_github_id")]
    public static IntPtr GetGitHubId() => ReturnString(APIKeys.GitHubID);

    [UnmanagedCallersOnly(EntryPoint = "get_github_secret")]
    public static IntPtr GetGitHubSecret() => ReturnString(APIKeys.GitHubSecret);

    [UnmanagedCallersOnly(EntryPoint = "get_paste_ee_application_key")]
    public static IntPtr GetPasteEeApplicationKey() => ReturnString(APIKeys.Paste_eeApplicationKey);

    [UnmanagedCallersOnly(EntryPoint = "get_dropbox_consumer_key")]
    public static IntPtr GetDropboxConsumerKey() => ReturnString(APIKeys.DropboxConsumerKey);

    [UnmanagedCallersOnly(EntryPoint = "get_dropbox_consumer_secret")]
    public static IntPtr GetDropboxConsumerSecret() => ReturnString(APIKeys.DropboxConsumerSecret);

    [UnmanagedCallersOnly(EntryPoint = "get_box_client_id")]
    public static IntPtr GetBoxClientId() => ReturnString(APIKeys.BoxClientID);

    [UnmanagedCallersOnly(EntryPoint = "get_box_client_secret")]
    public static IntPtr GetBoxClientSecret() => ReturnString(APIKeys.BoxClientSecret);

    [UnmanagedCallersOnly(EntryPoint = "get_sendspace_key")]
    public static IntPtr GetSendSpaceKey() => ReturnString(APIKeys.SendSpaceKey);

    [UnmanagedCallersOnly(EntryPoint = "get_jira_consumer_key")]
    public static IntPtr GetJiraConsumerKey() => ReturnString(APIKeys.JiraConsumerKey);

    [UnmanagedCallersOnly(EntryPoint = "get_mediafire_app_id")]
    public static IntPtr GetMediaFireAppId() => ReturnString(APIKeys.MediaFireAppId);

    [UnmanagedCallersOnly(EntryPoint = "get_mediafire_api_key")]
    public static IntPtr GetMediaFireApiKey() => ReturnString(APIKeys.MediaFireApiKey);

    [UnmanagedCallersOnly(EntryPoint = "get_onedrive_client_id")]
    public static IntPtr GetOneDriveClientId() => ReturnString(APIKeys.OneDriveClientID);

    [UnmanagedCallersOnly(EntryPoint = "get_onedrive_client_secret")]
    public static IntPtr GetOneDriveClientSecret() => ReturnString(APIKeys.OneDriveClientSecret);

    [UnmanagedCallersOnly(EntryPoint = "get_bitly_client_id")]
    public static IntPtr GetBitlyClientId() => ReturnString(APIKeys.BitlyClientID);

    [UnmanagedCallersOnly(EntryPoint = "get_bitly_client_secret")]
    public static IntPtr GetBitlyClientSecret() => ReturnString(APIKeys.BitlyClientSecret);

    [UnmanagedCallersOnly(EntryPoint = "get_google_client_id")]
    public static IntPtr GetGoogleClientId() => ReturnString(APIKeys.GoogleClientID);

    [UnmanagedCallersOnly(EntryPoint = "get_google_client_secret")]
    public static IntPtr GetGoogleClientSecret() => ReturnString(APIKeys.GoogleClientSecret);

    [UnmanagedCallersOnly(EntryPoint = "get_twitter_consumer_key")]
    public static IntPtr GetTwitterConsumerKey() => ReturnString(APIKeys.TwitterConsumerKey);

    [UnmanagedCallersOnly(EntryPoint = "get_twitter_consumer_secret")]
    public static IntPtr GetTwitterConsumerSecret() => ReturnString(APIKeys.TwitterConsumerSecret);
}
