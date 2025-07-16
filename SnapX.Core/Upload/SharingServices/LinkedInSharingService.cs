// SPDX-License-Identifier: GPL-3.0-or-later


namespace SnapX.Core.Upload.SharingServices;

public class LinkedInSharingService : SimpleURLSharingService
{
    public override URLSharingServices EnumValue => URLSharingServices.LinkedIn;

    protected override string URLFormatString => "https://www.linkedin.com/shareArticle?url={0}";
}
