// SPDX-License-Identifier: GPL-3.0-or-later


namespace SnapX.Core.Upload.SharingServices;

public class StumbleUponSharingService : SimpleURLSharingService
{
    public override URLSharingServices EnumValue => URLSharingServices.StumbleUpon;

    protected override string URLFormatString => "https://www.stumbleupon.com/submit?url={0}";
}

