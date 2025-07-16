// SPDX-License-Identifier: GPL-3.0-or-later


using SnapX.Core.Job;

namespace SnapX.Core.Hotkey;
public record HotkeySettings()
{
    public HotkeyInfo? HotkeyInfo { get; set; } = new();

    public TaskSettings? TaskSettings { get; set; }

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
            return $"Hotkey: {HotkeyInfo}, Description: {TaskSettings}, Job: {TaskSettings.Job}";
        }

        return "";
    }
}

