// SPDX-License-Identifier: GPL-3.0-or-later


namespace SnapX.Core.Upload.SharingServices;

public class VkSharingService : SimpleURLSharingService
{
    public override URLSharingServices EnumValue => URLSharingServices.VK;

    protected override string URLFormatString => "https://vk.com/share.php?url={0}";
}
