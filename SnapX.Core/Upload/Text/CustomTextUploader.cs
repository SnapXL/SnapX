// SPDX-License-Identifier: GPL-3.0-or-later


using System.Text;
using SnapX.Core.Upload.BaseServices;
using SnapX.Core.Upload.BaseUploaders;
using SnapX.Core.Upload.Custom;
using SnapX.Core.Upload.Utils;
using SnapX.Core.Utils.Extensions;

namespace SnapX.Core.Upload.Text;

public class CustomTextUploaderService : TextUploaderService
{
    public override TextDestination EnumValue => TextDestination.CustomTextUploader;

    public override bool CheckConfig(UploadersConfig config)
    {
        return config.CustomUploadersList != null && config.CustomUploadersList.IsValidIndex(config.CustomTextUploaderSelected);
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
            index = config.CustomTextUploaderSelected;
        }

        var customUploader = config.CustomUploadersList.ReturnIfValidIndex(index);

        if (customUploader != null)
        {
            return new CustomTextUploader(customUploader);
        }

        return null;
    }
}

public sealed class CustomTextUploader : TextUploader
{
    private CustomUploaderItem uploader;

    public CustomTextUploader(CustomUploaderItem customUploaderItem)
    {
        uploader = customUploaderItem;
    }

    public override UploadResult UploadText(string? text, string? fileName)
    {
        var result = new UploadResult();
        var input = new CustomUploaderInput(fileName, text);

        switch (uploader.Body)
        {
            case CustomUploaderBody.None:
                result.Response = SendRequest(uploader.RequestMethod, uploader.GetRequestURL(input), null, uploader.GetHeaders(input));
                break;

            case CustomUploaderBody.MultipartFormData:
                if (string.IsNullOrEmpty(uploader.FileFormName))
                {
                    result.Response = SendRequestMultiPart(
                        uploader.GetRequestURL(input),
                        uploader.GetArguments(input),
                        uploader.GetHeaders(input),
                        null,
                        uploader.RequestMethod
                    );
                }
                else
                {
                    var bytes = Encoding.UTF8.GetBytes(text);
                    using (var stream = new MemoryStream(bytes))
                    {
                        result = SendRequestFile(
                            uploader.GetRequestURL(input),
                            stream,
                            fileName,
                            uploader.GetFileFormName(),
                            uploader.GetArguments(input),
                            uploader.GetHeaders(input),
                            null,
                            uploader.RequestMethod
                        );
                    }
                }
                break;

            case CustomUploaderBody.FormURLEncoded:
                result.Response = SendRequestURLEncoded(
                    uploader.RequestMethod,
                    uploader.GetRequestURL(input),
                    uploader.GetArguments(input),
                    uploader.GetHeaders(input)
                );
                break;

            case CustomUploaderBody.JSON:
            case CustomUploaderBody.XML:
                result.Response = SendRequest(
                    uploader.RequestMethod,
                    uploader.GetRequestURL(input),
                    uploader.GetData(input),
                    uploader.GetContentType(),
                    null,
                    uploader.GetHeaders(input)
                );
                break;

            case CustomUploaderBody.Binary:
                var binaryBytes = Encoding.UTF8.GetBytes(text);
                using (var binaryStream = new MemoryStream(binaryBytes))
                {
                    result.Response = SendRequest(
                        uploader.RequestMethod,
                        uploader.GetRequestURL(input),
                        binaryStream,
                        MimeTypes.GetMimeType(fileName),
                        null,
                        uploader.GetHeaders(input)
                    );
                }
                break;

            default:
                throw new Exception("Unsupported request format: " + uploader.Body);
        }

        uploader.TryParseResponse(result, LastResponseInfo, Errors, input);
        return result;
    }
}

