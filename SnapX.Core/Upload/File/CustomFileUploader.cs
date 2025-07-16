// SPDX-License-Identifier: GPL-3.0-or-later


using SnapX.Core.Upload.BaseServices;
using SnapX.Core.Upload.BaseUploaders;
using SnapX.Core.Upload.Custom;
using SnapX.Core.Upload.Utils;
using SnapX.Core.Utils.Extensions;

namespace SnapX.Core.Upload.File;

public class CustomFileUploaderService : FileUploaderService
{
    public override FileDestination EnumValue { get; } = FileDestination.CustomFileUploader;

    public override bool CheckConfig(UploadersConfig config)
    {
        return config.CustomUploadersList != null && config.CustomUploadersList.IsValidIndex(config.CustomFileUploaderSelected);
    }

    public override GenericUploader CreateUploader(UploadersConfig config, TaskReferenceHelper taskInfo)
    {
        int index;

        if (taskInfo.OverrideCustomUploader)
        {
            index = taskInfo.CustomUploaderIndex.BetweenOrDefault(0, config.CustomUploadersList.Count - 1);
        }
        else
        {
            index = config.CustomFileUploaderSelected;
        }

        var customUploader = config.CustomUploadersList.ReturnIfValidIndex(index);

        if (customUploader != null)
        {
            return new CustomFileUploader(customUploader);
        }

        return null;
    }
}

public sealed class CustomFileUploader : FileUploader
{
    private CustomUploaderItem uploader;

    public CustomFileUploader(CustomUploaderItem customUploaderItem)
    {
        uploader = customUploaderItem;
    }

    public override UploadResult Upload(Stream stream, string? fileName)
    {
        UploadResult result = new UploadResult();
        CustomUploaderInput input = new CustomUploaderInput(fileName, "");

        if (uploader.Body == CustomUploaderBody.MultipartFormData)
        {
            result = SendRequestFile(uploader.GetRequestURL(input), stream, fileName, uploader.GetFileFormName(), uploader.GetArguments(input),
                uploader.GetHeaders(input), null, uploader.RequestMethod);
        }
        else if (uploader.Body == CustomUploaderBody.Binary)
        {
            result.Response = SendRequest(uploader.RequestMethod, uploader.GetRequestURL(input), stream, MimeTypes.GetMimeType(fileName), null,
                uploader.GetHeaders(input));
        }
        else
        {
            throw new Exception("Unsupported request format: " + uploader.Body);
        }

        uploader.TryParseResponse(result, LastResponseInfo, Errors, input);

        return result;
    }
}
