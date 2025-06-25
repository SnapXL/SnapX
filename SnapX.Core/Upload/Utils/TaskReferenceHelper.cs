
// SPDX-License-Identifier: GPL-3.0-or-later


namespace SnapX.Core.Upload.Utils;

public class TaskReferenceHelper
{
    public EDataType DataType { get; set; }
    public bool StopRequested { get; set; }
    public bool OverrideFTP { get; set; }
    public int FTPIndex { get; set; }
    public bool OverrideCustomUploader { get; set; }
    public int CustomUploaderIndex { get; set; }
    public string? TextFormat { get; set; }
}
