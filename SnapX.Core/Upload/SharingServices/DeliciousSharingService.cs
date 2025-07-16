// SPDX-License-Identifier: GPL-3.0-or-later


namespace SnapX.Core.Upload.SharingServices;

public class DeliciousSharingService : SimpleURLSharingService
{
    public override URLSharingServices EnumValue => URLSharingServices.Delicious;

    protected override string URLFormatString => "https://delicious.com/save?v=5&url={0}";
}

