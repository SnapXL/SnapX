// SPDX-License-Identifier: GPL-3.0-or-later


namespace SnapX.Core.Upload.BaseServices;

public interface IUploaderService
{
    string ServiceIdentifier { get; }

    string ServiceName { get; }

    bool CheckConfig(UploadersConfig config);

}
