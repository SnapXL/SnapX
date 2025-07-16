// SPDX-License-Identifier: GPL-3.0-or-later


namespace SnapX.Core.Upload.Zip;

public class ZipEntryInfo
{
    public string? EntryName { get; set; }
    public string? SourcePath { get; set; }
    public Stream Data { get; set; }

    public ZipEntryInfo(string? sourcePath, string? entryName = null)
    {
        SourcePath = sourcePath;

        EntryName = string.IsNullOrEmpty(entryName) ? sourcePath : entryName;
    }

    public ZipEntryInfo(Stream data, string? entryName)
    {
        Data = data;
        EntryName = entryName;
    }
}

