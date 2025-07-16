// SPDX-License-Identifier: GPL-3.0-or-later


using SnapX.Core.Upload.BaseServices;
using SnapX.Core.Upload.BaseUploaders;
using SnapX.Core.Upload.Utils;

namespace SnapX.Core.Upload.File;

public class TransfershFileUploaderService : FileUploaderService
{
    public override FileDestination EnumValue { get; } = FileDestination.Transfersh;

    public override bool CheckConfig(UploadersConfig config) => true;

    public override GenericUploader CreateUploader(UploadersConfig config, TaskReferenceHelper taskInfo)
    {
        return new Transfersh();
    }
}

public sealed class Transfersh : FileUploader
{
    public override UploadResult Upload(Stream stream, string? fileName)
    {
        var result = SendRequestFile("https://transfer.sh", stream, fileName, "file");

        if (result.IsSuccess)
        {
            result.URL = result.Response.Trim();
        }

        return result;
    }
}
