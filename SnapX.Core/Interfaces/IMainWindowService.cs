namespace SnapX.Core.SharpCapture.Interfaces;

public interface IMainWindowService
{
    Task HideAsync();
    Task ForceActivateAsync();
}
