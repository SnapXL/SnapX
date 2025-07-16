// SPDX-License-Identifier: GPL-3.0-or-later


using System.Collections.Specialized;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using SnapX.Core.Upload.BaseServices;
using SnapX.Core.Upload.BaseUploaders;
using SnapX.Core.Upload.Utils;
using SnapX.Core.Utils;
using SnapX.Core.Utils.Parsers;

namespace SnapX.Core.Upload.File;
public class AzureStorageUploaderService : FileUploaderService
{
    public override FileDestination EnumValue => FileDestination.AzureStorage;

    public override bool CheckConfig(UploadersConfig config)
    {
        return !string.IsNullOrEmpty(config.AzureStorageAccountName) &&
            !string.IsNullOrEmpty(config.AzureStorageAccountAccessKey) &&
            !string.IsNullOrEmpty(config.AzureStorageContainer);
    }

    public override GenericUploader CreateUploader(UploadersConfig config, TaskReferenceHelper taskInfo)
    {
        return new AzureStorage(config.AzureStorageAccountName, config.AzureStorageAccountAccessKey, config.AzureStorageContainer,
            config.AzureStorageEnvironment, config.AzureStorageCustomDomain, config.AzureStorageUploadPath, config.AzureStorageCacheControl);
    }
}

public sealed class AzureStorage : FileUploader
{
    private const string APIVersion = "2016-05-31";

    public string AzureStorageAccountName { get; private set; }
    public string AzureStorageAccountAccessKey { get; private set; }
    public string AzureStorageContainer { get; private set; }
    public string AzureStorageEnvironment { get; private set; }
    public string? AzureStorageCustomDomain { get; private set; }
    public string AzureStorageUploadPath { get; private set; }
    public string AzureStorageCacheControl { get; private set; }

    public AzureStorage(string azureStorageAccountName, string azureStorageAccessKey, string azureStorageContainer, string azureStorageEnvironment,
        string? customDomain, string uploadPath, string cacheControl)
    {
        AzureStorageAccountName = azureStorageAccountName;
        AzureStorageAccountAccessKey = azureStorageAccessKey;
        AzureStorageContainer = azureStorageContainer;
        AzureStorageEnvironment = (!string.IsNullOrEmpty(azureStorageEnvironment)) ? azureStorageEnvironment : "blob.core.windows.net";
        AzureStorageCustomDomain = customDomain;
        AzureStorageUploadPath = uploadPath;
        AzureStorageCacheControl = cacheControl;
    }

    public override UploadResult Upload(Stream stream, string? fileName)
    {
        if (string.IsNullOrEmpty(AzureStorageAccountName))
            Errors.Add("'Account Name' must not be empty");
        if (string.IsNullOrEmpty(AzureStorageAccountAccessKey))
            Errors.Add("'Access key' must not be empty");
        if (string.IsNullOrEmpty(AzureStorageContainer))
            Errors.Add("'Container' must not be empty");

        if (IsError)
            return null;

        var date = DateTime.UtcNow.ToString("R", CultureInfo.InvariantCulture);
        var uploadPath = GetUploadPath(fileName);
        var requestURL = GenerateURL(uploadPath, true);
        var resultURL = GenerateURL(uploadPath);

        OnEarlyURLCopyRequested(resultURL);

        var contentType = MimeTypes.GetMimeType(fileName);
        var requestHeaders = new NameValueCollection
        {
            { "x-ms-date", date },
            { "x-ms-version", APIVersion },
            { "x-ms-blob-type", "BlockBlob" }
        };

        var canonicalizedHeaders = $"x-ms-blob-type:BlockBlob\nx-ms-date:{date}\nx-ms-version:{APIVersion}\n";

        if (!string.IsNullOrEmpty(AzureStorageCacheControl))
        {
            requestHeaders["x-ms-blob-cache-control"] = AzureStorageCacheControl;
            canonicalizedHeaders = $"x-ms-blob-cache-control:{AzureStorageCacheControl}\n{canonicalizedHeaders}";
        }

        var canonicalizedResource = $"/{AzureStorageAccountName}/{AzureStorageContainer}/{uploadPath}";
        var stringToSign = GenerateStringToSign(canonicalizedHeaders, canonicalizedResource, stream.Length.ToString(), contentType);

        requestHeaders["Authorization"] = $"SharedKey {AzureStorageAccountName}:{stringToSign}";

        SendRequest(HttpMethod.Put, requestURL, stream, contentType, null, requestHeaders);

        if (LastResponseInfo?.IsSuccess == true)
        {
            return new UploadResult
            {
                IsSuccess = true,
                URL = resultURL
            };
        }

        Errors.Add("Upload failed.");
        return null;
    }


    private string GenerateStringToSign(string canonicalizedHeaders, string canonicalizedResource, string contentLength = "", string contentType = "")
    {
        var stringToSign = "PUT" + "\n" +
            "\n" +
            "\n" +
            (contentLength ?? "") + "\n" +
            "\n" +
            (contentType ?? "") + "\n" +
            "\n" +
            "\n" +
            "\n" +
            "\n" +
            "\n" +
            "\n" +
            canonicalizedHeaders +
            canonicalizedResource;

        return HashRequest(stringToSign);
    }

    private string HashRequest(string stringToSign)
    {
        string hashedString;

        using var hashAlgorithm = new HMACSHA256(Convert.FromBase64String(AzureStorageAccountAccessKey));
        var messageBuffer = Encoding.UTF8.GetBytes(stringToSign);
        hashedString = Convert.ToBase64String(hashAlgorithm.ComputeHash(messageBuffer));

        return hashedString;
    }

    private string? GetUploadPath(string? fileName)
    {
        if (!string.IsNullOrWhiteSpace(AzureStorageUploadPath))
        {
            var path = NameParser.Parse(NameParserType.FilePath, AzureStorageUploadPath.Trim('/'));
            return Uri.EscapeDataString(URLHelpers.CombineURL(path, fileName));
        }

        return Uri.EscapeDataString(fileName);
    }


    public string? GenerateURL(string? uploadPath, bool isRequest = false)
    {
        string? url;

        if (!isRequest && !string.IsNullOrEmpty(AzureStorageCustomDomain))
        {
            url = URLHelpers.CombineURL(AzureStorageCustomDomain, uploadPath);
            url = URLHelpers.FixPrefix(url);
        }
        else if (!isRequest && AzureStorageContainer == "$root")
        {
            url = $"https://{AzureStorageAccountName}.{AzureStorageEnvironment}/{uploadPath}";
        }
        else
        {
            url = $"https://{AzureStorageAccountName}.{AzureStorageEnvironment}/{AzureStorageContainer}/{uploadPath}";
        }

        return url;
    }

    public string? GetPreviewURL()
    {
        string? uploadPath = GetUploadPath("example.png");
        return GenerateURL(uploadPath);
    }
}

