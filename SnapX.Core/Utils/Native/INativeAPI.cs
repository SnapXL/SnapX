using SixLabors.ImageSharp;
using SnapX.Core.Media;

namespace SnapX.Core.Utils.Native;

// Secure, Contain, & Protect.
public interface INativeAPI
{
    void ShowWindow(WindowInfo windowInfo);
    void ShowWindow(IntPtr hwnd);
    Image GetJumboFileIcon(string filePath, bool jumboSize = true);
    void HideWindow(WindowInfo windowInfo);
    void HideWindow(IntPtr handle);
    List<WindowInfo> GetWindowList();
    void CopyText(string text);
    void CopyImage(Image image, string? fileName);
    Rectangle GetWindowRectangle(WindowInfo window);
    Rectangle GetWindowRectangle(IntPtr windowHandle);
    Point GetCursorPosition();
    Screen? GetScreen(Point pos);
}
