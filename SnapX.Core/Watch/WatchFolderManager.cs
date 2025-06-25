
// SPDX-License-Identifier: GPL-3.0-or-later


using SnapX.Core.Hotkey;
using SnapX.Core.Job;
using SnapX.Core.Upload;
using SnapX.Core.Utils;

namespace SnapX.Core.Watch;

public class WatchFolderManager : IDisposable
{
    public List<WatchFolder> WatchFolders { get; private set; }

    public void UpdateWatchFolders()
    {
        if (WatchFolders != null)
        {
            UnregisterAllWatchFolders();
        }

        WatchFolders = [];

        foreach (WatchFolderSettings defaultWatchFolderSetting in SnapX.DefaultTaskSettings.WatchFolderList)
        {
            AddWatchFolder(defaultWatchFolderSetting, SnapX.DefaultTaskSettings);
        }

        foreach (HotkeySettings hotkeySetting in SnapX.HotkeysConfig.Hotkeys)
        {
            foreach (WatchFolderSettings watchFolderSetting in hotkeySetting.TaskSettings.WatchFolderList)
            {
                AddWatchFolder(watchFolderSetting, hotkeySetting.TaskSettings);
            }
        }
    }

    private WatchFolder FindWatchFolder(WatchFolderSettings watchFolderSetting)
    {
        return WatchFolders.FirstOrDefault(watchFolder => watchFolder.Settings == watchFolderSetting);
    }

    private bool IsExist(WatchFolderSettings watchFolderSetting)
    {
        return FindWatchFolder(watchFolderSetting) != null;
    }

    public void AddWatchFolder(WatchFolderSettings watchFolderSetting, TaskSettings taskSettings)
    {
        if (!IsExist(watchFolderSetting))
        {
            if (!taskSettings.WatchFolderList.Contains(watchFolderSetting))
            {
                taskSettings.WatchFolderList.Add(watchFolderSetting);
            }

            WatchFolder watchFolder = new WatchFolder();
            watchFolder.Settings = watchFolderSetting;
            watchFolder.TaskSettings = taskSettings;

            watchFolder.FileWatcherTrigger += origPath =>
            {
                var taskSettingsCopy = TaskSettings.GetSafeTaskSettings(taskSettings);
                string? destPath = origPath;

                if (watchFolderSetting.MoveFilesToScreenshotsFolder)
                {
                    string? screenshotsFolder = TaskHelpers.GetScreenshotsFolder(taskSettingsCopy);
                    string fileName = Path.GetFileName(origPath);
                    destPath = Path.Combine(screenshotsFolder, fileName);
                    FileHelpers.CreateDirectoryFromFilePath(destPath);
                    File.Move(origPath, destPath);
                }

                UploadManager.UploadFile(destPath, taskSettingsCopy);
            };

            WatchFolders.Add(watchFolder);

            if (taskSettings.WatchFolderEnabled)
            {
                watchFolder.Enable();
            }
        }
    }

    public void RemoveWatchFolder(WatchFolderSettings watchFolderSetting)
    {
        using (WatchFolder watchFolder = FindWatchFolder(watchFolderSetting))
        {
            if (watchFolder != null)
            {
                watchFolder.TaskSettings.WatchFolderList.Remove(watchFolderSetting);
                WatchFolders.Remove(watchFolder);
            }
        }
    }

    public void UpdateWatchFolderState(WatchFolderSettings watchFolderSetting)
    {
        WatchFolder watchFolder = FindWatchFolder(watchFolderSetting);
        if (watchFolder != null)
        {
            if (watchFolder.TaskSettings.WatchFolderEnabled)
            {
                watchFolder.Enable();
            }
            else
            {
                watchFolder.Dispose();
            }
        }
    }

    public void UnregisterAllWatchFolders()
    {
        if (WatchFolders != null)
        {
            foreach (WatchFolder watchFolder in WatchFolders)
            {
                if (watchFolder != null)
                {
                    watchFolder.Dispose();
                }
            }
        }
    }

    public void Dispose()
    {
        UnregisterAllWatchFolders();
    }
}

