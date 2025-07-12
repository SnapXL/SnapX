using SnapX.Core.Job;

namespace SnapX.Core.Interfaces;

public interface INotificationService
{
    Task CloseActiveFormAsync();
    Task PlayNotificationSoundAsync(NotificationSound sound, TaskSettings settings);
}
