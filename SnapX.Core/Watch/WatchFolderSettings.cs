
// SPDX-License-Identifier: GPL-3.0-or-later


namespace SnapX.Core.Watch;

public class WatchFolderSettings
{
    public string? FolderPath { get; set; }
    public string Filter { get; set; }
    public bool IncludeSubdirectories { get; set; }
    public bool MoveFilesToScreenshotsFolder { get; set; }
}

