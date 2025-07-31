
// SPDX-License-Identifier: GPL-3.0-or-later


namespace SnapX.Core.Upload.Custom;

public record CustomUploaderInput(string? FileName, string? Input)
{
    public string? FileName { get; set; } = FileName;
    public string? Input { get; set; } = Input;
}
