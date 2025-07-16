// SPDX-License-Identifier: GPL-3.0-or-later


namespace SnapX.Core.Upload.SharingServices;

public class PinterestSharingService : SimpleURLSharingService
{
    public override URLSharingServices EnumValue => URLSharingServices.Pinterest;

    protected override string URLFormatString => "https://pinterest.com/pin/create/button/?url={0}&media={0}";
}

