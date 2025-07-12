using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SnapX.Core.Media;

namespace SnapX.Core.Utils.Native;

public partial class MacOSAPI : INativeAPI
{
    private static string GenerateFastString(int length)
    {
        const string chars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        var random = new System.Random();
        var result = new char[length];
        for (var i = 0; i < length; i++)
            result[i] = chars[random.Next(chars.Length)];
        return new string(result);
    }
    private const string CoreGraphics = "/System/Library/Frameworks/CoreGraphics.framework/CoreGraphics";


    public void CopyImage(Image image, string? fileName)
    {
        fileName ??= GenerateFastString(8) + ".png";
        var tempPath = Path.Combine(Path.GetTempPath(), $"{Path.GetFileNameWithoutExtension(fileName)}.jpg");
        image.Save(tempPath, new JpegEncoder());
        var appleScript = $"set the clipboard to (read (POSIX file \"{tempPath}\") as JPEG picture)";

        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "osascript",
                Arguments = $"-e \"{appleScript.Replace("\"", "\\\"")}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false
            }
        };

        process.Start();
        process.WaitForExit();

        if (process.ExitCode != 0)
        {
            var error = process.StandardError.ReadToEnd();
            throw new IOException($"osascript failed: {error}");
        }
        File.Delete(tempPath);
    }

    public Rectangle GetWindowRectangle(WindowInfo window)
    {
        throw new NotImplementedException();
    }

    public Rectangle GetWindowRectangle(IntPtr windowHandle)
    {
        throw new NotImplementedException();
    }


    public void ShowWindow(WindowInfo windowInfo)
    {
        throw new NotImplementedException();
    }

    public void ShowWindow(IntPtr hwnd)
    {
        throw new NotImplementedException();
    }

    public Image GetJumboFileIcon(string filePath, bool jumboSize = true)
    {
        throw new NotImplementedException();
    }

    public void HideWindow(WindowInfo windowInfo)
    {
        throw new NotImplementedException();
    }

    public void HideWindow(IntPtr handle)
    {
        throw new NotImplementedException();
    }

    public List<WindowInfo> GetWindowList()
    {
        throw new NotImplementedException();
    }
    public void CopyText(string text)
    {
        // Escape quotes in the text to ensure AppleScript handles them correctly
        // 1. Escape double quotes by replacing `"` with `""` for AppleScript
        var escapedText = text.Replace("\"", "\"\"");

        // 2. Escape backslashes by replacing `\` with `\\`
        escapedText = "\"" + Regex.Replace(escapedText, @"(\\+)$", @"$1$1") + "\"";

        var appleScript = $"set the clipboard to \"{escapedText}\"";

        var process = new Process();
        process.StartInfo.FileName = "osascript";
        process.StartInfo.Arguments = $"-e \"{appleScript}\"";
        process.StartInfo.RedirectStandardOutput = true;
        process.StartInfo.UseShellExecute = false;

        process.Start();

        process.WaitForExit();
    }
    [StructLayout(LayoutKind.Sequential)]
    struct CGPoint
    {
        public double X;
        public double Y;
    }
    [LibraryImport(CoreGraphics)]
    private static partial CGPoint CGEventGetLocation(IntPtr eventRef);

    [LibraryImport(CoreGraphics)]
    private static partial IntPtr CGEventCreate(IntPtr source);
    [LibraryImport(CoreGraphics)]
    private static partial void CFRelease(IntPtr eventRef);
    public Point GetCursorPosition()
    {
        var ev = CGEventCreate(IntPtr.Zero);
        var point = CGEventGetLocation(ev);
        CFRelease(ev);
        return new Point((int)point.X, (int)point.Y);
    }

    public Screen? GetScreen(Point pos)
    {
        throw new NotImplementedException();
    }
}
