// SPDX-License-Identifier: GPL-3.0-or-later

using SnapX.Core.Media;
using SnapX.Core.Utils.Extensions;
using SnapX.Core.Utils.Native;

namespace SnapX.Core.ScreenCapture.Helpers;

public class WindowsList
{
    public List<IntPtr> IgnoreWindows { get; set; }

    private string[] ignoreList = ["Progman", "Button"];
    private List<WindowInfo> windows;

    public WindowsList()
    {
        IgnoreWindows = [];
    }

    public WindowsList(IntPtr ignoreWindow) : this()
    {
        IgnoreWindows.Add(ignoreWindow);
    }

    public List<WindowInfo> GetWindowsList()
    {
        windows = Methods.GetWindowList();
        return windows;
    }

    public List<WindowInfo> GetVisibleWindowsList()
    {
        var windows = GetWindowsList();

        return windows.Where(IsValidWindow).ToList();
    }

    private bool IsValidWindow(WindowInfo window)
    {
        return window != null && window.IsVisible && !string.IsNullOrEmpty(window.Title) && IsWindowAllowed(window) && window.Rectangle.IsValid();
    }

    private bool IsWindowAllowed(WindowInfo window)
    {
        var WindowTitle = window.Title;

        if (!string.IsNullOrEmpty(WindowTitle))
        {
            return ignoreList.All(ignore => !WindowTitle.Equals(ignore, StringComparison.OrdinalIgnoreCase));
        }

        return true;
    }
}
