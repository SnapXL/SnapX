using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using SixLabors.ImageSharp;

namespace SnapX.Core.Utils.Native;

public class MacOSAPI : NativeAPI
{
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
