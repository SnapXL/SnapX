// SPDX-License-Identifier: GPL-3.0-or-later


namespace SnapX.Core.Upload.SharingServices;

public class TumblrSharingService : SimpleURLSharingService
{
    public override URLSharingServices EnumValue => URLSharingServices.Tumblr;

    protected override string URLFormatString => "https://www.tumblr.com/share?v=3&u={0}";
}

