using System.Diagnostics.CodeAnalysis;
using CasCap.Models;
using CasCap.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SnapX.Core.Upload.BaseServices;
using SnapX.Core.Upload.BaseUploaders;
using SnapX.Core.Upload.OAuth;
using SnapX.Core.Upload.Utils;
using SnapX.Core.Utils.Extensions;
using SnapX.Core.Utils.Miscellaneous;

namespace SnapX.Core.Upload.Img;

public class GooglePhotosImageUploaderService : ImageUploaderService
{
    public override ImageDestination EnumValue => ImageDestination.Picasa;

    public override bool CheckConfig(UploadersConfig config)
    {
        return OAuth2Info.CheckOAuth(config.GooglePhotosOAuth2Info);
    }

    public override GenericUploader CreateUploader(UploadersConfig config, TaskReferenceHelper taskInfo)
    {
        return new GooglePhotos(config.GooglePhotosOAuth2Info)
        {
            AlbumID = config.GooglePhotosAlbumID,
            IsPublic = config.GooglePhotosIsPublic
        };
    }
}

public sealed class GooglePhotos : ImageUploader, IOAuth2
{
    public static GoogleOAuth2 OAuth2 { get; private set; }
    public OAuth2Info AuthInfo => OAuth2.AuthInfo;
    public string AlbumID { get; set; }
    public bool IsPublic { get; set; }

    public GooglePhotosOptions GPOptions { get; set; }
    // Most things should be using the shared HttpClient but in this scenario, we are modifying the baseURL so instead we copy it.
    public HttpClient Client { get; private set; } = HttpClientFactory.Get().Copy();
    public GooglePhotosService GooglePhotosService { get; private set; }

    public GooglePhotos(OAuth2Info oauth)
    {
        OAuth2 = new GoogleOAuth2(oauth, this)
        {
            Scope = "https://www.googleapis.com/auth/photoslibrary https://www.googleapis.com/auth/photoslibrary.sharing https://www.googleapis.com/auth/userinfo.profile"
        };
        GPOptions = new()
        {
            User = OAuth2.AuthInfo.Email,
            ClientId = OAuth2.AuthInfo.Client_ID,
            ClientSecret = OAuth2.AuthInfo.Client_Secret,
            Scopes = new[]
            {
                GooglePhotosScope.AppCreatedData,
                GooglePhotosScope.Sharing,
                GooglePhotosScope.AppendOnly,
            }

        };
        Client.BaseAddress = new Uri(GPOptions.BaseAddress);
        var logger = new LoggerFactory().CreateLogger<GooglePhotosService>();

        GooglePhotosService = new GooglePhotosService(logger, Options.Create(GPOptions), Client);
    }

    [UnconditionalSuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code", Justification = "<Pending>")]
    public bool RefreshAccessToken()
    {
        return OAuth2.RefreshAccessToken();
    }

    public bool CheckAuthorization()
    {
        GooglePhotosService.LoginAsync().Wait();
        return OAuth2.CheckAuthorization();
    }

    public string? GetAuthorizationURL()
    {
        return OAuth2.GetAuthorizationURL();
    }

    [UnconditionalSuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code", Justification = "<Pending>")]
    public bool GetAccessToken(string? code)
    {
        return OAuth2.GetAccessToken(code);
    }

    [UnconditionalSuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code", Justification = "<Pending>")]
    public OAuthUserInfo GetUserInfo()
    {
        return OAuth2.GetUserInfo();
    }
    [RequiresDynamicCode("Uploader")]
    [RequiresUnreferencedCode("Uploader")]
    public Album CreateAlbum(string? albumName)
    {
        var album = GooglePhotosService.CreateAlbumAsync(albumName);
        return album.GetAwaiter().GetResult()!;
    }

    [RequiresUnreferencedCode("Uploader")]
    public List<Album> GetAlbumList()
    {
        var album = GooglePhotosService.GetAlbumsAsync();
        return album.GetAwaiter().GetResult()!;
    }

    [RequiresUnreferencedCode("Uploader")]
    public override UploadResult Upload(Stream stream, string? fileName)
    {
        if (!CheckAuthorization()) return null;

        var result = new UploadResult();

        var album = CreateAlbum(fileName);

        if (IsPublic)
        {
            AlbumID = album.id;

            var shareInfo = GooglePhotosService.ShareAlbumAsync(AlbumID).GetAwaiter().GetResult()!;

            result.URL = shareInfo.shareableUrl;
        }

        var newMediaItemResult = GooglePhotosService.UploadSingle(fileName, AlbumID, $"Created by SnapX {Core.SnapX.VersionText}").GetAwaiter().GetResult()!;

        if (!IsPublic)
        {
            result.URL = newMediaItemResult.mediaItem.productUrl;
        }

        return result;
    }
}
