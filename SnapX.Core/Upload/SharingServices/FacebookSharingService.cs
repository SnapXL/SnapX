// SPDX-License-Identifier: GPL-3.0-or-later


namespace SnapX.Core.Upload.SharingServices;

public class FacebookSharingService : SimpleURLSharingService
{
    public override URLSharingServices EnumValue => URLSharingServices.Facebook;

    protected override string URLFormatString => "https://www.facebook.com/sharer/sharer.php?u={0}";
}

