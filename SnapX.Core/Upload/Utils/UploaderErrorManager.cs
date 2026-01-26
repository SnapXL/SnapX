
// SPDX-License-Identifier: GPL-3.0-or-later


namespace SnapX.Core.Upload.Utils;

public class UploaderErrorManager
{
    public List<UploaderErrorInfo> Errors { get; private set; } = [];

    public int Count => Errors.Count;

    public string? DefaultTitle { get; set; } = "Error";

    public void Add(string? text)
    {
        Add(DefaultTitle, text);
    }

    private void Add(string? title, string? text)
    {
        Errors.Add(new UploaderErrorInfo(title, text));
    }

    public void Add(UploaderErrorManager manager)
    {
        if (manager == null)
        {
            return;
        }

        Errors.AddRange(manager.Errors);
    }

    public void AddFirst(string? text)
    {
        AddFirst(DefaultTitle, text);
    }

    private void AddFirst(string? title, string? text)
    {
        Errors.Insert(0, new UploaderErrorInfo(title, text));
    }

    public override string ToString()
    {
        var output = string.Join(Environment.NewLine + Environment.NewLine, Errors.Select(x => x.Text));
        return output;
    }

    public void Clear()
    {
        Errors.Clear();
    }
}
