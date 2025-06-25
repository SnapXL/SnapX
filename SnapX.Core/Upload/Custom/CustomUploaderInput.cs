
// SPDX-License-Identifier: GPL-3.0-or-later


namespace SnapX.Core.Upload.Custom;

public class CustomUploaderInput
{
    public string? FileName { get; set; }
    public string? Input { get; set; }

    public CustomUploaderInput(string? fileName, string? input)
    {
        FileName = fileName;
        Input = input;
    }
}
