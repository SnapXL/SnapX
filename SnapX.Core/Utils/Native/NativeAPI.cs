using SixLabors.ImageSharp;
using SnapX.Core.Media;

namespace SnapX.Core.Utils.Native;

// Secure, Contain, & Protect.
public class NativeAPI
{
    public virtual void ShowWindow(WindowInfo windowInfo) => throw new NotImplementedException("NativeAPI.ShowWindow is not implemented.");

    public virtual void ShowWindow(IntPtr handle) => throw new NotImplementedException("NativeAPI.ShowWindow is not implemented.");

    public virtual void HideWindow(WindowInfo windowInfo) => throw new NotImplementedException("NativeAPI.HideWindow is not implemented.");
    public virtual List<WindowInfo> GetWindowList() => throw new NotImplementedException("NativeAPI.GetWindowList is not implemented.");
    public virtual void HideWindow(IntPtr handle) => throw new NotImplementedException("NativeAPI.HideWindow is not implemented.");
    public virtual void CopyText(string text) => throw new NotImplementedException("NativeAPI.CopyText is not implemented.");
    public virtual void CopyImage(Image image) => CopyImage(image, "image.png"); // this could will never run

    public virtual void CopyImage(Image image, string fileName) => throw new NotImplementedException("NativeAPI.CopyImage is not implemented.");
    public virtual Rectangle GetWindowRectangle(WindowInfo window) => throw new NotImplementedException("NativeAPI.GetWindowRect is not implemented.");
    public virtual Rectangle GetWindowRectangle(IntPtr windowHandle) => throw new NotImplementedException("NativeAPI.GetWindowRect is not implemented.");
    public virtual Point GetCursorPosition() => throw new NotImplementedException("NativeAPI.GetCursorPosition is not implemented.");
    public virtual Screen GetScreen(Point pos) =>
        throw new NotImplementedException("NativeAPI.GetScreen is not implemented");
}
