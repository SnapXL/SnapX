
// SPDX-License-Identifier: GPL-3.0-or-later


using SixLabors.ImageSharp;
using SnapX.Core.Media;
using SnapX.Core.Utils.Extensions;

namespace SnapX.Core.Job;

public class TaskMetadata : IDisposable
{
    private const int WindowInfoMaxLength = 255;

    public Image Image { get; set; }

    private string? windowTitle;

    public string? WindowTitle
    {
        get
        {
            return windowTitle;
        }
        set
        {
            windowTitle = value.Truncate(WindowInfoMaxLength);
        }
    }

    private string? processName;

    public string? ProcessName
    {
        get
        {
            return processName;
        }
        set
        {
            processName = value.Truncate(WindowInfoMaxLength);
        }
    }

    public TaskMetadata()
    {
    }

    public TaskMetadata(Image image)
    {
        Image = image;
    }
    public void UpdateInfo(WindowInfo? windowInfo)
    {
        if (windowInfo == null) return;
        WindowTitle = windowInfo.Title;
        ProcessName = windowInfo.ProcessName;
    }
    public void Dispose()
    {
        Image?.Dispose();
    }
}

