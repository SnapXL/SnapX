// SPDX-License-Identifier: GPL-3.0-or-later


using MimeTypeCore;
using SnapX.Core.Upload.BaseServices;
using SnapX.Core.Upload.BaseUploaders;
using SnapX.Core.Upload.Custom;
using SnapX.Core.Upload.Utils;
using SnapX.Core.Utils.Extensions;

namespace SnapX.Core.Upload.Img;

public class CustomImageUploaderService : ImageUploaderService
{
    public override ImageDestination EnumValue => ImageDestination.CustomImageUploader;

    public override bool CheckConfig(UploadersConfig config)
    {
        return config.CustomUploadersList != null && config.CustomUploadersList.IsValidIndex(config.CustomImageUploaderSelected);
    }

    public override GenericUploader CreateUploader(UploadersConfig config, TaskReferenceHelper taskInfo)
    {
        var index = taskInfo.OverrideCustomUploader
            ? taskInfo.CustomUploaderIndex.BetweenOrDefault(0, config.CustomUploadersList.Count - 1)
            : config.CustomImageUploaderSelected;

        var customUploader = config.CustomUploadersList.ReturnIfValidIndex(index);

        if (customUploader == null)
        {
            return null;
        }

        return new CustomImageUploader(customUploader);
    }
}

public sealed class CustomImageUploader : ImageUploader
{
    private CustomUploaderItem uploader;

    public CustomImageUploader(CustomUploaderItem customUploaderItem)
    {
        uploader = customUploaderItem;
    }

    public override UploadResult Upload(Stream stream, string? fileName)
    {
        var ur = new UploadResult();
        var input = new CustomUploaderInput(fileName, "");

        if (uploader.Body == CustomUploaderBody.MultipartFormData)
        {
            ur = SendRequestFile(uploader.GetRequestURL(input), stream, fileName, uploader.GetFileFormName(), uploader.GetArguments(input),
                uploader.GetHeaders(input), null, uploader.RequestMethod);
        }
        else if (uploader.Body == CustomUploaderBody.Binary)
        {
            ur.Response = SendRequest(uploader.RequestMethod, uploader.GetRequestURL(input), stream, MimeTypeMap.GetMimeType(fileName),
                null, uploader.GetHeaders(input));
        }
        else
        {
            throw new Exception("Unsupported request format: " + uploader.Body);
        }

        uploader.TryParseResponse(ur, LastResponseInfo, Errors, input);

        return ur;
    }
}

