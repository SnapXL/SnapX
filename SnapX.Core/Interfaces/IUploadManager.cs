using SnapX.Core.Job;

namespace SnapX.Core.Interfaces;

public interface IUploadManager
{
    Task RunImageTaskAsync(TaskMetadata metadata, TaskSettings taskSettings);
}
