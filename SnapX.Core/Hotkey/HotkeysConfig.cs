// SPDX-License-Identifier: GPL-3.0-or-later

using SnapX.Core.Utils;

namespace SnapX.Core.Hotkey;

public class HotkeysConfig : SettingsBase<HotkeysConfig>
{
    public List<HotkeySettings> Hotkeys { get; set; } = HotkeyManager.GetDefaultHotkeyList();
}

