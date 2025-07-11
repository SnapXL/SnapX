// SPDX-License-Identifier: GPL-3.0-or-later

namespace SnapX.Core.Hotkey;

public record HotkeysConfig
{
    public List<HotkeySettings> Hotkeys = HotkeyManager.GetDefaultHotkeyList();
}

