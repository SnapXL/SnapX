// SPDX-License-Identifier: GPL-3.0-or-later


namespace SnapX.Core.Upload.BaseUploaders;

public abstract class FileUploader : GenericUploader
{
    public virtual UploaderCategory Category => UploaderCategory.FileUploaders;
    public UploadResult UploadFile(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath)) throw new ArgumentNullException(nameof(filePath));
        if (!System.IO.File.Exists(filePath)) throw new FileNotFoundException(filePath);

        using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
        return Upload(stream, Path.GetFileName(filePath));
    }
}
