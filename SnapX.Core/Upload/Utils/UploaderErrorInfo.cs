// SPDX-License-Identifier: GPL-3.0-or-later



namespace SnapX.Core.Upload.Utils;

public class UploaderErrorInfo
{
    public string? Title { get; set; }
    public string? Text { get; set; }
    public Exception Exception { get; set; }

    public UploaderErrorInfo(string? title, string? text)
    {
        Title = title;
        Text = text;
    }
}
