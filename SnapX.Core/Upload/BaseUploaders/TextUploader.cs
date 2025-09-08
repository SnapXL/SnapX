
// SPDX-License-Identifier: GPL-3.0-or-later


using System.Text;

namespace SnapX.Core.Upload.BaseUploaders;

public abstract class TextUploader : GenericUploader
{
    public override UploadResult Upload(Stream stream, string? fileName)
    {
        DebugHelper.WriteLine($"Stream: size ={stream.Length} writable {stream.CanWrite}");
        using (StreamReader sr = new StreamReader(stream, Encoding.UTF8))
        {
            return UploadText(sr.ReadToEnd(), fileName);
        }
    }

    public abstract UploadResult UploadText(string? text, string? fileName);

    public UploadResult UploadTextFile(string filePath)
    {
        if (System.IO.File.Exists(filePath))
        {
            using (FileStream stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                return Upload(stream, Path.GetFileName(filePath));
            }
        }

        return null;
    }
}
