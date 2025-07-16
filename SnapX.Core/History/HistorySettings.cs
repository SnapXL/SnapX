// SPDX-License-Identifier: GPL-3.0-or-later


namespace SnapX.Core.History;

public record HistorySettings
{
    public bool RememberWindowState { get; set; } = true;
    public int SplitterDistance { get; set; } = 550;
    public bool RememberSearchText { get; set; } = false;
    public string SearchText { get; set; } = "";
}
