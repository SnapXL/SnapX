// SPDX-License-Identifier: GPL-3.0-or-later


using SnapX.Core.Upload.BaseUploaders;
using SnapX.Core.Upload.Utils;

namespace SnapX.Core.Upload.BaseServices;

public abstract class URLSharingService : UploaderService<URLSharingServices>
{
    public abstract URLSharer CreateSharer(UploadersConfig config, TaskReferenceHelper taskInfo);
}
