using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SnapX.Core.Media;
using uniffi.snapxrust;

namespace SnapX.Core.Utils.Native;

public class MacOSAPI : NativeAPI
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

    public override void CopyImage(Image image)
    {
        CopyImage(image, GenerateFastString(8) + ".png");
    }

    public override void CopyImage(Image image, string? fileName)
    {
        var tempPath = Path.Combine(
            Path.GetTempPath(),
            $"{Path.GetFileNameWithoutExtension(fileName)}.jpg"
        );
        image.Save(tempPath, new JpegEncoder());
        var appleScript =
            $"set the clipboard to (read (POSIX file \"{tempPath}\") as JPEG picture)";

        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "osascript",
                Arguments = $"-e \"{appleScript.Replace("\"", "\\\"")}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
            },
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

    public override void CopyText(string text)
    {
        var escapedText = text.Replace("\"", "\"\"");

        escapedText = "\"" + Regex.Replace(escapedText, @"(\\+)$", @"$1$1") + "\"";
        ;

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

    [DllImport("/System/Library/Frameworks/CoreGraphics.framework/CoreGraphics")]
    static extern CGPoint CGEventGetLocation(IntPtr eventRef);

    [DllImport("/System/Library/Frameworks/CoreGraphics.framework/CoreGraphics")]
    static extern IntPtr CGEventCreate(IntPtr source);

    [DllImport("/System/Library/Frameworks/CoreGraphics.framework/CoreGraphics")]
    static extern IntPtr CFRelease(IntPtr eventRef);

    public override Point GetCursorPosition()
    {
        var ev = CGEventCreate(IntPtr.Zero);
        var point = CGEventGetLocation(ev);
        CFRelease(ev);
        return new Point((int)point.X, (int)point.Y);
    }
    public void ShowWindow(WindowInfo window)
    {
        if (window.ProcessId == 0)
            return;

        var script =
            $"tell application \"System Events\" to set frontmost of every process whose unix id is {window.ProcessId} to true";

        var psi = new ProcessStartInfo
        {
            FileName = "osascript",
            Arguments = $"-e '{script}'",
            RedirectStandardOutput = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };

        using var process = Process.Start(psi);
        process?.WaitForExit();
    }

    public override List<WindowInfo> GetWindowList()
    {
        DebugHelper.WriteLine($"GetWindowList called");

        var rawWindows = SnapxrustMethods.GetWindowList();

        var windows = rawWindows
            .Select(raw => new WindowInfo
            {
                ProcessId = (int)raw.processId,
                ProcessName = Process.GetProcessById((int)raw.processId).ProcessName,

                Title = raw.title,
                Rectangle = new Rectangle(raw.x, raw.y, (int)raw.width, (int)raw.height),
                IsMinimized = raw.isMinimized,
                IsVisible = !raw.isMinimized,
                IsActive = raw.isFocused,

                // Not exposed
                Handle = IntPtr.Zero,
            })
            .ToList();

        return windows;
    }
}
