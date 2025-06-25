
// SPDX-License-Identifier: GPL-3.0-or-later


using SnapX.Core.Upload.BaseServices;
using SnapX.Core.Upload.BaseUploaders;
using SnapX.Core.Upload.Custom;
using SnapX.Core.Upload.Utils;
using SnapX.Core.Utils.Extensions;

namespace SnapX.Core.Upload.SharingServices;

public class CustomURLSharingService : URLSharingService
{
    public override URLSharingServices EnumValue => URLSharingServices.CustomURLSharingService;

    public override bool CheckConfig(UploadersConfig config)
    {
        return config.CustomUploadersList != null && config.CustomUploadersList.IsValidIndex(config.CustomURLSharingServiceSelected);
    }

    public override URLSharer CreateSharer(UploadersConfig config, TaskReferenceHelper taskInfo)
    {
        int index = taskInfo.OverrideCustomUploader
            ? taskInfo.CustomUploaderIndex.BetweenOrDefault(0, config.CustomUploadersList.Count - 1)
            : config.CustomURLSharingServiceSelected;

        var customUploader = config.CustomUploadersList.ReturnIfValidIndex(index);

        return customUploader != null ? new CustomURLSharer(customUploader) : null;
    }
}

public sealed class CustomURLSharer : URLSharer
{
    private CustomUploaderItem uploader;

    public CustomURLSharer(CustomUploaderItem customUploaderItem)
    {
        uploader = customUploaderItem;
    }

    public override UploadResult ShareURL(string? url)
    {
        var result = new UploadResult { URL = url, IsURLExpected = false };
        var input = new CustomUploaderInput("", url);

        var response = uploader.Body switch
        {
            CustomUploaderBody.None => SendRequest(uploader.RequestMethod, uploader.GetRequestURL(input), null, uploader.GetHeaders(input)),
            CustomUploaderBody.MultipartFormData => SendRequestMultiPart(uploader.GetRequestURL(input), uploader.GetArguments(input), uploader.GetHeaders(input), null, uploader.RequestMethod),
            CustomUploaderBody.FormURLEncoded => SendRequestURLEncoded(uploader.RequestMethod, uploader.GetRequestURL(input), uploader.GetArguments(input), uploader.GetHeaders(input)),
            CustomUploaderBody.JSON or CustomUploaderBody.XML => SendRequest(uploader.RequestMethod, uploader.GetRequestURL(input), uploader.GetData(input), uploader.GetContentType(), null, uploader.GetHeaders(input)),
            _ => throw new Exception("Unsupported request format: " + uploader.Body)
        };

        result.Response = response;
        return result;
    }
}

