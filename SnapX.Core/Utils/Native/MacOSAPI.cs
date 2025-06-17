using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;

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

    public override void CopyImage(Image image, string fileName)
    {
        var tempPath = Path.Combine(Path.GetTempPath(), $"{Path.GetFileNameWithoutExtension(fileName)}.jpg");
        image.Save(tempPath, new JpegEncoder());
        var appleScript = $"set the clipboard to (read (POSIX file \"\"{tempPath}\"\") as JPEG picture)";

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


    public override void CopyText(string text)
    {
        // Escape quotes in the text to ensure AppleScript handles them correctly
        // 1. Escape double quotes by replacing `"` with `""` for AppleScript
        string escapedText = text.Replace("\"", "\"\"");

        // 2. Escape backslashes by replacing `\` with `\\` (for C# string formatting)
        escapedText = "\"" + Regex.Replace(escapedText, @"(\\+)$", @"$1$1") + "\""; ;

        // Properly format the AppleScript to set the clipboard
        var appleScript = $"set the clipboard to \"{escapedText}\"";

        // Create the process to execute the AppleScript
        var process = new Process();
        process.StartInfo.FileName = "osascript";
        process.StartInfo.Arguments = $"-e \"{appleScript}\"";
        process.StartInfo.RedirectStandardOutput = true;
        process.StartInfo.UseShellExecute = false;

        // Start the process
        process.Start();

        // Wait for the process to finish
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
}
