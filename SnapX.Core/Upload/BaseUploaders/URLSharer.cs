
// SPDX-License-Identifier: GPL-3.0-or-later


namespace SnapX.Core.Upload.BaseUploaders;

public abstract class URLSharer : Uploader
{
    public virtual UploaderCategory Category => UploaderCategory.URLSharing;
    public abstract UploadResult ShareURL(string? url);
}
