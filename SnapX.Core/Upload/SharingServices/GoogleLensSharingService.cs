// SPDX-License-Identifier: GPL-3.0-or-later


namespace SnapX.Core.Upload.SharingServices;

public class GoogleLensSharingService : SimpleURLSharingService
{
    public override URLSharingServices EnumValue => URLSharingServices.GoogleImageSearch;

    protected override string URLFormatString => "https://lens.google.com/uploadbyurl?url={0}";
}

