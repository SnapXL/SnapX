
// SPDX-License-Identifier: GPL-3.0-or-later



namespace SnapX.Core.Media;

public class VideoThumbnailInfo
{
    public string? FilePath { get; set; }
    public TimeSpan Timestamp { get; set; }

    public VideoThumbnailInfo(string? filePath)
    {
        FilePath = filePath;
    }

    public override string ToString()
    {
        return Path.GetFileName(FilePath);
    }
}
