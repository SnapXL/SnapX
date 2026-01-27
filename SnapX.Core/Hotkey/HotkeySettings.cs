
// SPDX-License-Identifier: GPL-3.0-or-later


using SnapX.Core.Job;

namespace SnapX.Core.Hotkey;

public class HotkeySettings
{
    public HotkeyInfo HotkeyInfo { get; set; }

    public TaskSettings TaskSettings { get; set; }

    public HotkeySettings()
    {
        HotkeyInfo = new HotkeyInfo();
    }

    public HotkeySettings(HotkeyType job, Keys hotkey = Keys.None) : this()
    {
        TaskSettings = TaskSettings.GetDefaultTaskSettings();
        TaskSettings.Job = job;
        HotkeyInfo = new HotkeyInfo(hotkey);
    }

    public override string ToString()
    {
        if (HotkeyInfo != null && TaskSettings != null)
        {
            return string.Format("Hotkey: {0}, Description: {1}, Job: {2}", HotkeyInfo, TaskSettings, TaskSettings.Job);
        }

        return "";
    }
}

