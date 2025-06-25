
// SPDX-License-Identifier: GPL-3.0-or-later


using SnapX.Core.Upload.BaseServices;
using SnapX.Core.Upload.BaseUploaders;
using SnapX.Core.Upload.File;
using SnapX.Core.Upload.Utils;
using SnapX.Core.Utils.Extensions;

namespace SnapX.Core.Upload.SharingServices;

public class PushbulletSharingService : URLSharingService
{
    public override URLSharingServices EnumValue => URLSharingServices.Pushbullet;

    public override bool CheckConfig(UploadersConfig config)
    {
        var pushbulletSettings = config.PushbulletSettings;

        return pushbulletSettings != null && !string.IsNullOrEmpty(pushbulletSettings.UserAPIKey) && pushbulletSettings.DeviceList != null &&
            pushbulletSettings.DeviceList.IsValidIndex(pushbulletSettings.SelectedDevice);
    }

    public override URLSharer CreateSharer(UploadersConfig config, TaskReferenceHelper taskInfo)
    {
        return new PushbulletSharer(config.PushbulletSettings);
    }
}

public sealed class PushbulletSharer : URLSharer
{
    public PushbulletSettings Settings { get; private set; }

    public PushbulletSharer(PushbulletSettings settings)
    {
        Settings = settings;
    }

    public override UploadResult ShareURL(string? url)
    {
        var result = new UploadResult { URL = url, IsURLExpected = false };

        new Pushbullet(Settings).PushLink(url, "SnapX: URL share");

        return result;
    }
}

