
// SPDX-License-Identifier: GPL-3.0-or-later


using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using SnapX.Core.Upload.BaseServices;
using SnapX.Core.Upload.BaseUploaders;
using SnapX.Core.Upload.OAuth;
using SnapX.Core.Upload.Utils;
using SnapX.Core.Utils;
using SnapX.Core.Utils.Miscellaneous;
using SnapX.Core.Utils.Parsers;

namespace SnapX.Core.Upload.File;

public class GoogleCloudStorageNewFileUploaderService : FileUploaderService
{
    public override FileDestination EnumValue => FileDestination.GoogleCloudStorage;
    public override bool CheckConfig(UploadersConfig config)
    {
        return OAuth2Info.CheckOAuth(config.GoogleCloudStorageOAuth2Info) && !string.IsNullOrEmpty(config.GoogleCloudStorageBucket);
    }

    public override GenericUploader CreateUploader(UploadersConfig config, TaskReferenceHelper taskInfo)
    {
        return new GoogleCloudStorage(config.GoogleCloudStorageOAuth2Info)
        {
            Bucket = config.GoogleCloudStorageBucket,
            Domain = config.GoogleCloudStorageDomain,
            Prefix = config.GoogleCloudStorageObjectPrefix,
            RemoveExtensionImage = config.GoogleCloudStorageRemoveExtensionImage,
            RemoveExtensionText = config.GoogleCloudStorageRemoveExtensionText,
            RemoveExtensionVideo = config.GoogleCloudStorageRemoveExtensionVideo,
            SetPublicACL = config.GoogleCloudStorageSetPublicACL
        };
    }
}

public sealed class GoogleCloudStorage : FileUploader, IOAuth2
{
    public GoogleOAuth2 OAuth2 { get; private set; }
    public OAuth2Info AuthInfo => OAuth2.AuthInfo;
    public string? Bucket { get; set; }
    public string? Domain { get; set; }
    public string Prefix { get; set; }
    public bool RemoveExtensionImage { get; set; }
    public bool RemoveExtensionText { get; set; }
    public bool RemoveExtensionVideo { get; set; }
    public bool SetPublicACL { get; set; }

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        TypeInfoResolver = GoogleContext.Default
    };

    public GoogleCloudStorage(OAuth2Info oauth)
    {
        OAuth2 = new GoogleOAuth2(oauth, this)
        {
            Scope = "https://www.googleapis.com/auth/devstorage.read_write https://www.googleapis.com/auth/userinfo.profile"
        };
    }

    [UnconditionalSuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code", Justification = "<Pending>")]
    public bool RefreshAccessToken()
    {
        return OAuth2.RefreshAccessToken();
    }

    public bool CheckAuthorization()
    {
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

    [RequiresDynamicCode("Uploader")]
    [RequiresUnreferencedCode("Uploader")]
    public override UploadResult Upload(Stream stream, string? fileName)
    {
        if (!CheckAuthorization())
            return null;

        var uploadPath = GetUploadPath(fileName);

        OnEarlyURLCopyRequested(GenerateURL(uploadPath));

        var googleCloudStorageMetadata = new GoogleCloudStorageMetadata
        {
            name = uploadPath
        };

        if (SetPublicACL)
        {
            googleCloudStorageMetadata.acl =
            [
                new GoogleCloudStorageAcl
                {
                    entity = "allUsers",
                    role = "READER"
                }
            ];
        }

        var serializedGoogleCloudStorageMetadata = JsonSerializer.Serialize(googleCloudStorageMetadata, SerializerOptions);

        var result = SendRequestFile(
            "https://www.googleapis.com/upload/storage/v1/b/{Bucket}/o?uploadType=multipart&fields=name",
            stream,
            fileName,
            null,
            headers: OAuth2.GetAuthHeaders(),
            contentType: "multipart/related",
            relatedData: serializedGoogleCloudStorageMetadata
        );

        var googleCloudStorageResponse = JsonSerializer.Deserialize<GoogleCloudStorageResponse>(result.Response, SerializerOptions);

        result.URL = GenerateURL(googleCloudStorageResponse.name);

        return result;
    }


    private string? GetUploadPath(string? fileName)
    {
        string? uploadPath = NameParser.Parse(NameParserType.FilePath, Prefix.Trim('/'));

        if ((RemoveExtensionImage && FileHelpers.IsImageFile(fileName)) ||
            (RemoveExtensionText && FileHelpers.IsTextFile(fileName)) ||
            (RemoveExtensionVideo && FileHelpers.IsVideoFile(fileName)))
        {
            fileName = Path.GetFileNameWithoutExtension(fileName);
        }

        return URLHelpers.CombineURL(uploadPath, fileName);
    }

    public string? GenerateURL(string? uploadPath)
    {
        if (string.IsNullOrEmpty(Bucket))
        {
            return "";
        }

        if (string.IsNullOrEmpty(Domain))
        {
            Domain = URLHelpers.CombineURL("storage.googleapis.com", Bucket);
        }

        uploadPath = URLHelpers.URLEncode(uploadPath, true, HelpersOptions.URLEncodeIgnoreEmoji);

        string? url = URLHelpers.CombineURL(Domain, uploadPath);

        return URLHelpers.FixPrefix(url);
    }

    public string? GetPreviewURL()
    {
        string? uploadPath = GetUploadPath("example.png");
        return GenerateURL(uploadPath);
    }

    public class GoogleCloudStorageResponse
    {
        public string? name { get; set; }
    }

    public class GoogleCloudStorageMetadata
    {
        public string? name { get; set; }
        public GoogleCloudStorageAcl[] acl { get; set; }
    }

    public class GoogleCloudStorageAcl
    {
        public string entity { get; set; }
        public string role { get; set; }
    }
}

