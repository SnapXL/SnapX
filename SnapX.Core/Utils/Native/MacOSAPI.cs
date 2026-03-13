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

    private const string CoreGraphicsLib =
        "/System/Library/Frameworks/CoreGraphics.framework/CoreGraphics";

    [DllImport(CoreGraphicsLib)]
    static extern CGPoint CGEventGetLocation(IntPtr eventRef);

    [DllImport(CoreGraphicsLib)]
    static extern IntPtr CGEventCreate(IntPtr source);

    [DllImport(CoreGraphicsLib)]
    static extern IntPtr CFRelease(IntPtr eventRef);

    private const string CoreFoundationLib =
        "/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation";

    // Options for window list
    private const uint kCGWindowListOptionIncludingWindow = 1 << 3;

    [DllImport(CoreGraphicsLib)]
    private static extern IntPtr CGWindowListCopyWindowInfo(uint option, uint relativeToWindow);

    [DllImport(CoreFoundationLib)]
    private static extern int CFArrayGetCount(IntPtr theArray);

    [DllImport(CoreFoundationLib)]
    private static extern IntPtr CFArrayGetValueAtIndex(IntPtr theArray, int idx);
    [DllImport(CoreFoundationLib)]
    internal static extern IntPtr CFStringCreateWithCString(
        IntPtr alloc,
        string str,
        uint encoding
    );

    [DllImport(CoreFoundationLib)]
    internal static extern IntPtr CFDictionaryGetValue(IntPtr theDict, IntPtr key);

    [DllImport(CoreGraphicsLib)]
    [return: MarshalAs(UnmanagedType.I1)]
    internal static extern bool CGRectMakeWithDictionaryRepresentation(
        IntPtr dict,
        out CGRect rect
    );
    [StructLayout(LayoutKind.Sequential)]
    public struct CGRect
    {
        public double X;
        public double Y;
        public double Width;
        public double Height;
    }

    // Standard UTF8 encoding ID for CFString
    internal const uint kCFStringEncodingUTF8 = 0x08000100;

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
    public override Rectangle GetWindowRectangle(WindowInfo window)
    {
        return base.GetWindowRectangle(window.Handle);
    }
    public override Rectangle GetWindowRectangle(IntPtr windowHandle)
    {
        uint windowId = (uint)windowHandle.ToInt32();

        IntPtr arrayRef = CGWindowListCopyWindowInfo(kCGWindowListOptionIncludingWindow, windowId);
        if (arrayRef == IntPtr.Zero)
            return Rectangle.Empty;

        try
        {
            int count = CFArrayGetCount(arrayRef);
            if (count == 0)
                return Rectangle.Empty;

            IntPtr dictRef = CFArrayGetValueAtIndex(arrayRef, 0);

            return ExtractCGRectFromDict(dictRef);
        }
        finally
        {
            CFRelease(arrayRef);
        }
    }
    private Rectangle ExtractCGRectFromDict(IntPtr dictRef)
    {
        IntPtr key = CFStringCreateWithCString(
            IntPtr.Zero,
            "kCGWindowBounds",
            kCFStringEncodingUTF8
        );

        try
        {
            IntPtr boundsDict = CFDictionaryGetValue(dictRef, key);

            if (
                boundsDict != IntPtr.Zero
                && CGRectMakeWithDictionaryRepresentation(boundsDict, out CGRect cgRect)
            )
            {
                return new Rectangle(
                    (int)cgRect.X,
                    (int)cgRect.Y,
                    (int)cgRect.Width,
                    (int)cgRect.Height
                );
            }
        }
        finally
        {
            if (key != IntPtr.Zero)
                CFRelease(key);
        }

        return Rectangle.Empty;
    }

    public override List<WindowInfo> GetWindowList()
    {
        DebugHelper.WriteLine($"GetWindowList called");

        var rawWindows = SnapxrustMethods.GetWindowList();

        var windows = rawWindows
            .Select(raw => new WindowInfo
            {
                ProcessId = (int)raw.ProcessId,
                ProcessName = Process.GetProcessById((int)raw.ProcessId).ProcessName,

                Title = raw.Title,
                Rectangle = new Rectangle(raw.X, raw.Y, (int)raw.Width, (int)raw.Height),
                IsMinimized = raw.IsMinimized,
                IsVisible = !raw.IsMinimized,
                IsActive = raw.IsFocused,
                Handle = (nint)raw.Hwnd,
            })
            .ToList();

        return windows;
    }
}
