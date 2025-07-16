// SPDX-License-Identifier: GPL-3.0-or-later


namespace SnapX.Core.Upload.BaseUploaders;

public abstract class URLShortener : Uploader
{
    public abstract UploadResult ShortenURL(string? url);
}
