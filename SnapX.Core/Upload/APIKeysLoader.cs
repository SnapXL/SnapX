using System.Runtime.InteropServices;

namespace SnapX.Core.Upload;

public static class APIKeysLoader
{
    private const string LibName = "SnapX.Secrets.so";

    [DllImport(LibName, EntryPoint = "get_imgur_id")]
    private static extern IntPtr n_get_imgur_id();

    [DllImport(LibName, EntryPoint = "get_imgur_secret")]
    private static extern IntPtr n_get_imgur_secret();

    [DllImport(LibName, EntryPoint = "get_imageshack_key")]
    private static extern IntPtr n_get_imageshack_key();

    [DllImport(LibName, EntryPoint = "get_flickr_key")]
    private static extern IntPtr n_get_flickr_key();

    [DllImport(LibName, EntryPoint = "get_flickr_secret")]
    private static extern IntPtr n_get_flickr_secret();

    [DllImport(LibName, EntryPoint = "get_photobucket_consumer_key")]
    private static extern IntPtr n_get_photobucket_consumer_key();

    [DllImport(LibName, EntryPoint = "get_photobucket_consumer_secret")]
    private static extern IntPtr n_get_photobucket_consumer_secret();

    [DllImport(LibName, EntryPoint = "get_pastebin_key")]
    private static extern IntPtr n_get_pastebin_key();

    [DllImport(LibName, EntryPoint = "get_github_id")]
    private static extern IntPtr n_get_github_id();

    [DllImport(LibName, EntryPoint = "get_github_secret")]
    private static extern IntPtr n_get_github_secret();

    [DllImport(LibName, EntryPoint = "get_paste_ee_application_key")]
    private static extern IntPtr n_get_paste_ee_application_key();

    [DllImport(LibName, EntryPoint = "get_dropbox_consumer_key")]
    private static extern IntPtr n_get_dropbox_consumer_key();

    [DllImport(LibName, EntryPoint = "get_dropbox_consumer_secret")]
    private static extern IntPtr n_get_dropbox_consumer_secret();

    [DllImport(LibName, EntryPoint = "get_box_client_id")]
    private static extern IntPtr n_get_box_client_id();

    [DllImport(LibName, EntryPoint = "get_box_client_secret")]
    private static extern IntPtr n_get_box_client_secret();

    [DllImport(LibName, EntryPoint = "get_sendspace_key")]
    private static extern IntPtr n_get_sendspace_key();

    [DllImport(LibName, EntryPoint = "get_jira_consumer_key")]
    private static extern IntPtr n_get_jira_consumer_key();

    [DllImport(LibName, EntryPoint = "get_mediafire_app_id")]
    private static extern IntPtr n_get_mediafire_app_id();

    [DllImport(LibName, EntryPoint = "get_mediafire_api_key")]
    private static extern IntPtr n_get_mediafire_api_key();

    [DllImport(LibName, EntryPoint = "get_onedrive_client_id")]
    private static extern IntPtr n_get_onedrive_client_id();

    [DllImport(LibName, EntryPoint = "get_onedrive_client_secret")]
    private static extern IntPtr n_get_onedrive_client_secret();

    [DllImport(LibName, EntryPoint = "get_bitly_client_id")]
    private static extern IntPtr n_get_bitly_client_id();

    [DllImport(LibName, EntryPoint = "get_bitly_client_secret")]
    private static extern IntPtr n_get_bitly_client_secret();

    [DllImport(LibName, EntryPoint = "get_google_client_id")]
    private static extern IntPtr n_get_google_client_id();

    [DllImport(LibName, EntryPoint = "get_google_client_secret")]
    private static extern IntPtr n_get_google_client_secret();

    [DllImport(LibName, EntryPoint = "get_twitter_consumer_key")]
    private static extern IntPtr n_get_twitter_consumer_key();

    [DllImport(LibName, EntryPoint = "get_twitter_consumer_secret")]
    private static extern IntPtr n_get_twitter_consumer_secret();

    public static void Load()
    {
        try
        {
            APIKeys.ImgurClientID = Marshal.PtrToStringAnsi(n_get_imgur_id());
            APIKeys.ImgurClientSecret = Marshal.PtrToStringAnsi(n_get_imgur_secret());
            APIKeys.ImageShackKey = Marshal.PtrToStringAnsi(n_get_imageshack_key()) ?? string.Empty;
            APIKeys.FlickrKey = Marshal.PtrToStringAnsi(n_get_flickr_key()) ?? string.Empty;
            APIKeys.FlickrSecret = Marshal.PtrToStringAnsi(n_get_flickr_secret()) ?? string.Empty;
            APIKeys.PhotobucketConsumerKey = Marshal.PtrToStringAnsi(n_get_photobucket_consumer_key()) ?? string.Empty;
            APIKeys.PhotobucketConsumerSecret = Marshal.PtrToStringAnsi(n_get_photobucket_consumer_secret()) ?? string.Empty;
            APIKeys.PastebinKey = Marshal.PtrToStringAnsi(n_get_pastebin_key()) ?? string.Empty;
            APIKeys.GitHubID = Marshal.PtrToStringAnsi(n_get_github_id()) ?? string.Empty;
            APIKeys.GitHubSecret = Marshal.PtrToStringAnsi(n_get_github_secret()) ?? string.Empty;
            APIKeys.Paste_eeApplicationKey = Marshal.PtrToStringAnsi(n_get_paste_ee_application_key()) ?? string.Empty;
            APIKeys.DropboxConsumerKey = Marshal.PtrToStringAnsi(n_get_dropbox_consumer_key()) ?? string.Empty;
            APIKeys.DropboxConsumerSecret = Marshal.PtrToStringAnsi(n_get_dropbox_consumer_secret()) ?? string.Empty;
            APIKeys.BoxClientID = Marshal.PtrToStringAnsi(n_get_box_client_id()) ?? string.Empty;
            APIKeys.BoxClientSecret = Marshal.PtrToStringAnsi(n_get_box_client_secret()) ?? string.Empty;
            APIKeys.SendSpaceKey = Marshal.PtrToStringAnsi(n_get_sendspace_key());
            APIKeys.JiraConsumerKey = Marshal.PtrToStringAnsi(n_get_jira_consumer_key()) ?? string.Empty;
            APIKeys.MediaFireAppId = Marshal.PtrToStringAnsi(n_get_mediafire_app_id());
            APIKeys.MediaFireApiKey = Marshal.PtrToStringAnsi(n_get_mediafire_api_key()) ?? string.Empty;
            APIKeys.OneDriveClientID = Marshal.PtrToStringAnsi(n_get_onedrive_client_id()) ?? string.Empty;
            APIKeys.OneDriveClientSecret = Marshal.PtrToStringAnsi(n_get_onedrive_client_secret()) ?? string.Empty;
            APIKeys.BitlyClientID = Marshal.PtrToStringAnsi(n_get_bitly_client_id());
            APIKeys.BitlyClientSecret = Marshal.PtrToStringAnsi(n_get_bitly_client_secret());
            APIKeys.GoogleClientID = Marshal.PtrToStringAnsi(n_get_google_client_id()) ?? string.Empty;
            APIKeys.GoogleClientSecret = Marshal.PtrToStringAnsi(n_get_google_client_secret()) ?? string.Empty;
            APIKeys.TwitterConsumerKey = Marshal.PtrToStringAnsi(n_get_twitter_consumer_key()) ?? string.Empty;
            APIKeys.TwitterConsumerSecret = Marshal.PtrToStringAnsi(n_get_twitter_consumer_secret()) ?? string.Empty;
        }
        catch (DllNotFoundException ex)
        {
            Console.WriteLine($"{nameof(APIKeysLoader)}: Failed to load '{LibName}' API keys will be unavailable.");
            Console.Error.WriteLine(ex);
        }
    }
}
