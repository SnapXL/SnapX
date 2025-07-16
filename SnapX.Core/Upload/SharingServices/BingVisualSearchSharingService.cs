// SPDX-License-Identifier: GPL-3.0-or-later


namespace SnapX.Core.Upload.SharingServices;

public class BingVisualSearchSharingService : SimpleURLSharingService
{
    public override URLSharingServices EnumValue => URLSharingServices.BingVisualSearch;

    protected override string URLFormatString => "https://www.bing.com/images/search?view=detailv2&iss=sbi&q=imgurl:{0}";
}

