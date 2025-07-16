// SPDX-License-Identifier: GPL-3.0-or-later


namespace SnapX.Core.Upload.SharingServices;

public class RedditSharingService : SimpleURLSharingService
{
    public override URLSharingServices EnumValue => URLSharingServices.Reddit;

    protected override string URLFormatString => "https://www.reddit.com/submit?url={0}";
}

