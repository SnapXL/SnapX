// SPDX-License-Identifier: GPL-3.0-or-later


using SnapX.Core.Job;

namespace SnapX.Core.Hotkey;
public class HotkeyManager
{
    public List<HotkeySettings> Hotkeys { get; private set; }
    public bool IgnoreHotkeys { get; set; }

    public delegate void HotkeyTriggerEventHandler(HotkeySettings hotkeySetting);
    public delegate void HotkeysToggledEventHandler(bool hotkeysEnabled);

    public HotkeyTriggerEventHandler HotkeyTrigger;
    public HotkeysToggledEventHandler HotkeysToggledTrigger;

    public void UpdateHotkeys(List<HotkeySettings> hotkeys, bool showFailedHotkeys)
    {
        if (Hotkeys != null)
        {
            UnregisterAllHotkeys();
        }

        Hotkeys = hotkeys;

        RegisterAllHotkeys();
    }

    protected void OnHotkeyTrigger(HotkeySettings hotkeySetting)
    {
        HotkeyTrigger?.Invoke(hotkeySetting);
    }

    public void RegisterHotkey(HotkeySettings hotkeySetting)
    {
        if (!SnapX.Settings.DisableHotkeys || hotkeySetting.TaskSettings.Job == HotkeyType.DisableHotkeys)
        {
            UnregisterHotkey(hotkeySetting, false);

            if (hotkeySetting.HotkeyInfo.Status != HotkeyStatus.Registered && hotkeySetting.HotkeyInfo.IsValidHotkey)
            {
                // hotkeyForm.RegisterHotkey(hotkeySetting.HotkeyInfo);

                if (hotkeySetting.HotkeyInfo.Status == HotkeyStatus.Registered)
                {
                    DebugHelper.WriteLine("Hotkey registered: " + hotkeySetting);
                }
                else if (hotkeySetting.HotkeyInfo.Status == HotkeyStatus.Failed)
                {
                    DebugHelper.WriteLine("Hotkey register failed: " + hotkeySetting);
                }
            }
            else
            {
                hotkeySetting.HotkeyInfo.Status = HotkeyStatus.NotConfigured;
            }
        }

        if (!Hotkeys.Contains(hotkeySetting))
        {
            Hotkeys.Add(hotkeySetting);
        }
    }

    public void RegisterAllHotkeys()
    {
        foreach (HotkeySettings hotkeySetting in Hotkeys.ToArray())
        {
            RegisterHotkey(hotkeySetting);
        }
    }

    public void RegisterFailedHotkeys()
    {
        foreach (HotkeySettings hotkeySetting in Hotkeys.Where(x => x.HotkeyInfo.Status == HotkeyStatus.Failed))
        {
            RegisterHotkey(hotkeySetting);
        }
    }

    public void UnregisterHotkey(HotkeySettings hotkeySetting, bool removeFromList = true)
    {
        if (hotkeySetting.HotkeyInfo.Status == HotkeyStatus.Registered)
        {
            DebugHelper.WriteLine("UnregisterHotkey(hotkeySetting.HotkeyInfo) " + hotkeySetting);

            if (hotkeySetting.HotkeyInfo.Status == HotkeyStatus.NotConfigured)
            {
                DebugHelper.WriteLine("Hotkey unregistered: " + hotkeySetting);
            }
            else if (hotkeySetting.HotkeyInfo.Status == HotkeyStatus.Failed)
            {
                DebugHelper.WriteLine("Hotkey unregister failed: " + hotkeySetting);
            }
        }

        if (removeFromList)
        {
            Hotkeys.Remove(hotkeySetting);
        }
    }

    public void UnregisterAllHotkeys(bool removeFromList = true, bool temporary = false)
    {
        if (Hotkeys != null)
        {
            foreach (HotkeySettings hotkeySetting in Hotkeys.ToArray())
            {
                if (!temporary || hotkeySetting.TaskSettings.Job != HotkeyType.DisableHotkeys)
                {
                    UnregisterHotkey(hotkeySetting, removeFromList);
                }
            }
        }
    }

    public void ToggleHotkeys(bool hotkeysDisabled)
    {
        if (!hotkeysDisabled)
        {
            RegisterAllHotkeys();
        }
        else
        {
            UnregisterAllHotkeys(false, true);
        }

        HotkeysToggledTrigger?.Invoke(hotkeysDisabled);
    }

    public void ResetHotkeys()
    {
        UnregisterAllHotkeys();
        Hotkeys.AddRange(GetDefaultHotkeyList());
        RegisterAllHotkeys();

        if (SnapX.Settings.DisableHotkeys)
        {
            TaskHelpers.ToggleHotkeys();
        }
    }

    public static List<HotkeySettings> GetDefaultHotkeyList()
    {
        return
        [
            new HotkeySettings(HotkeyType.RectangleRegion, Keys.Control | Keys.PrintScreen),
            new HotkeySettings(HotkeyType.PrintScreen, Keys.PrintScreen),
            new HotkeySettings(HotkeyType.ActiveWindow, Keys.Alt | Keys.PrintScreen),
            new HotkeySettings(HotkeyType.ScreenRecorder, Keys.Shift | Keys.PrintScreen),
            new HotkeySettings(HotkeyType.ScreenRecorderGIF, Keys.Control | Keys.Shift | Keys.PrintScreen)
        ];
    }
}

