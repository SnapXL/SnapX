namespace SnapX.Core.Interfaces;

public interface IDelayService
{
    Task DelayAsync(int milliseconds);
}
