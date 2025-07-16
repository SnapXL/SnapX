// SPDX-License-Identifier: GPL-3.0-or-later



using SnapX.Core.Utils.Extensions;

namespace SnapX.Core.Job;

public class QuickTaskInfo
{
    public string Name { get; set; }
    public AfterCaptureTasks AfterCaptureTasks { get; set; }
    public AfterUploadTasks AfterUploadTasks { get; set; }

    public bool IsValid
    {
        get
        {
            return AfterCaptureTasks != AfterCaptureTasks.None;
        }
    }

    public static List<QuickTaskInfo> DefaultPresets =>
    [
        new("Save, Upload, Copy URL", AfterCaptureTasks.SaveImageToFile | AfterCaptureTasks.UploadImageToHost, AfterUploadTasks.CopyURLToClipboard),
        new("Save, Copy image", AfterCaptureTasks.SaveImageToFile | AfterCaptureTasks.CopyImageToClipboard),
        new("Save, Copy image file", AfterCaptureTasks.SaveImageToFile | AfterCaptureTasks.CopyFileToClipboard),
        new("Annotate, Save, Upload, Copy URL", AfterCaptureTasks.AnnotateImage | AfterCaptureTasks.SaveImageToFile | AfterCaptureTasks.UploadImageToHost, AfterUploadTasks.CopyURLToClipboard),
        new(),
        new("Upload, Copy URL", AfterCaptureTasks.UploadImageToHost, AfterUploadTasks.CopyURLToClipboard),
        new("Save", AfterCaptureTasks.SaveImageToFile),
        new("Copy image", AfterCaptureTasks.CopyImageToClipboard),
        new("Annotate", AfterCaptureTasks.AnnotateImage)
    ];

    public QuickTaskInfo()
    {
    }

    public QuickTaskInfo(string name, AfterCaptureTasks afterCaptureTasks, AfterUploadTasks afterUploadTasks = AfterUploadTasks.None)
    {
        Name = name;
        AfterCaptureTasks = afterCaptureTasks;
        AfterUploadTasks = afterUploadTasks;
    }

    public QuickTaskInfo(AfterCaptureTasks afterCaptureTasks, AfterUploadTasks afterUploadTasks = AfterUploadTasks.None) : this(null, afterCaptureTasks, afterUploadTasks)
    {
    }

    public override string ToString()
    {
        if (!string.IsNullOrEmpty(Name))
        {
            return Name;
        }

        var result = string.Join(", ", AfterCaptureTasks.GetFlags().Select(x => x.GetLocalizedDescription()));

        if (AfterCaptureTasks.HasFlag(AfterCaptureTasks.UploadImageToHost))
        {
            var flags = AfterUploadTasks.GetFlags().Select(x => x.GetLocalizedDescription()).ToArray();

            if (flags != null && flags.Length > 0)
            {
                result += ", " + string.Join(", ", flags);
            }
        }

        return result;
    }
}
