
// SPDX-License-Identifier: GPL-3.0-or-later


using SixLabors.ImageSharp;

namespace SnapX.Core.Upload.BaseUploaders;

public abstract class ImageUploader : FileUploader
{
    public UploadResult UploadImage(Image image, string? fileName)
    {
        using var stream = new MemoryStream();
        image.Save(stream, image.Metadata.DecodedImageFormat!);
        return Upload(stream, fileName);
    }
}
