
// SPDX-License-Identifier: GPL-3.0-or-later


using SnapX.Core.Upload.BaseServices;
using SnapX.Core.Upload.BaseUploaders;
using SnapX.Core.Upload.Utils;
using SnapX.Core.Utils;
using SnapX.Core.Utils.Extensions;

namespace SnapX.Core.Upload.File;

public class SharedFolderFileUploaderService : FileUploaderService
{
    public override FileDestination EnumValue { get; } = FileDestination.SharedFolder;

    public override bool CheckConfig(UploadersConfig config)
    {
        return config.LocalhostAccountList != null && config.LocalhostAccountList.IsValidIndex(config.LocalhostSelectedFiles);
    }

    public override GenericUploader CreateUploader(UploadersConfig config, TaskReferenceHelper taskInfo)
    {
        int index;

        switch (taskInfo.DataType)
        {
            case EDataType.Image:
                index = config.LocalhostSelectedImages;
                break;
            case EDataType.Text:
                index = config.LocalhostSelectedText;
                break;
            default:
            case EDataType.File:
                index = config.LocalhostSelectedFiles;
                break;
        }

        LocalhostAccount account = config.LocalhostAccountList.ReturnIfValidIndex(index);

        if (account != null)
        {
            return new SharedFolderUploader(account);
        }

        return null;
    }
}

public class SharedFolderUploader : FileUploader
{
    private LocalhostAccount account;

    public SharedFolderUploader(LocalhostAccount account)
    {
        this.account = account;
    }

    public override UploadResult Upload(Stream stream, string? fileName)
    {
        UploadResult result = new UploadResult();

        string? filePath = account.GetLocalhostPath(fileName);

        FileHelpers.CreateDirectoryFromFilePath(filePath);

        using (FileStream fs = new FileStream(filePath, FileMode.Create))
        {
            if (TransferData(stream, fs))
            {
                result.URL = account.GetUriPath(Path.GetFileName(fileName));
            }
        }

        return result;
    }
}
