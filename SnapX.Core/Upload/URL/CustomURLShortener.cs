// SPDX-License-Identifier: GPL-3.0-or-later


using SnapX.Core.Upload.BaseServices;
using SnapX.Core.Upload.BaseUploaders;
using SnapX.Core.Upload.Custom;
using SnapX.Core.Upload.Utils;
using SnapX.Core.Utils.Extensions;

namespace SnapX.Core.Upload.URL;

public class CustomURLShortenerService : URLShortenerService
{
    public override UrlShortenerType EnumValue { get; } = UrlShortenerType.CustomURLShortener;

    public override bool CheckConfig(UploadersConfig config)
    {
        return config.CustomUploadersList != null && config.CustomUploadersList.IsValidIndex(config.CustomURLShortenerSelected);
    }

    public override URLShortener CreateShortener(UploadersConfig config, TaskReferenceHelper taskInfo)
    {
        int index;

        if (taskInfo.OverrideCustomUploader)
        {
            index = taskInfo.CustomUploaderIndex.BetweenOrDefault(0, config.CustomUploadersList.Count - 1);
        }
        else
        {
            index = config.CustomURLShortenerSelected;
        }

        var customUploader = config.CustomUploadersList.ReturnIfValidIndex(index);

        if (customUploader != null)
        {
            return new CustomURLShortener(customUploader);
        }

        return null;
    }
}

public sealed class CustomURLShortener : URLShortener
{
    private CustomUploaderItem uploader;

    public CustomURLShortener(CustomUploaderItem customUploaderItem)
    {
        uploader = customUploaderItem;
    }

    public override UploadResult ShortenURL(string? url)
    {
        var result = new UploadResult { URL = url };
        var input = new CustomUploaderInput("", url);

        if (uploader.Body == CustomUploaderBody.None)
        {
            result.Response = SendRequest(uploader.RequestMethod, uploader.GetRequestURL(input), null, uploader.GetHeaders(input));
        }
        else if (uploader.Body == CustomUploaderBody.MultipartFormData)
        {
            result.Response = SendRequestMultiPart(uploader.GetRequestURL(input), uploader.GetArguments(input), uploader.GetHeaders(input), null, uploader.RequestMethod);
        }
        else if (uploader.Body == CustomUploaderBody.FormURLEncoded)
        {
            result.Response = SendRequestURLEncoded(uploader.RequestMethod, uploader.GetRequestURL(input), uploader.GetArguments(input), uploader.GetHeaders(input));
        }
        else if (uploader.Body == CustomUploaderBody.JSON || uploader.Body == CustomUploaderBody.XML)
        {
            result.Response = SendRequest(uploader.RequestMethod, uploader.GetRequestURL(input), uploader.GetData(input), uploader.GetContentType(),
                null, uploader.GetHeaders(input));
        }
        else
        {
            throw new Exception("Unsupported request format: " + uploader.Body);
        }

        uploader.TryParseResponse(result, LastResponseInfo, Errors, input, true);

        return result;
    }
}

