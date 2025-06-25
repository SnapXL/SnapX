
// SPDX-License-Identifier: GPL-3.0-or-later


using SixLabors.ImageSharp;
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
    // This code should be removed after SnapX.Core compiles
    public void UpdateInfo<T>(T windowInfo)
    {
        // TODO: Migrate API consumers to SnapX.CommonUI
        // if (windowInfo != null)
        // {
        //     WindowTitle = windowInfo.Text;
        //     ProcessName = windowInfo.ProcessName;
        // }
    }

    public void Dispose()
    {
        Image?.Dispose();
    }
}

