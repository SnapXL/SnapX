
// SPDX-License-Identifier: GPL-3.0-or-later


namespace SnapX.Core.ScreenCapture.ScreenRecording;

public class FFmpegCaptureDevice
{
    public string Value { get; set; }
    public string Title { get; set; }

    public FFmpegCaptureDevice(string value, string title)
    {
        Value = value;
        Title = title;
    }

    public static FFmpegCaptureDevice None { get; } = new FFmpegCaptureDevice("", "None");
    public static FFmpegCaptureDevice GDIGrab { get; } = new FFmpegCaptureDevice("gdigrab", "gdigrab (Graphics Device Interface)");
    public static FFmpegCaptureDevice DDAGrab { get; } = new FFmpegCaptureDevice("ddagrab", "ddagrab (Desktop Duplication API)");
    public static FFmpegCaptureDevice ScreenCaptureRecorder { get; } = new FFmpegCaptureDevice("screen-capture-recorder", "dshow (screen-capture-recorder)");
    public static FFmpegCaptureDevice VirtualAudioCapturer { get; } = new FFmpegCaptureDevice("virtual-audio-capturer", "dshow (virtual-audio-capturer)");

    public override string ToString()
    {
        return Title;
    }
}
