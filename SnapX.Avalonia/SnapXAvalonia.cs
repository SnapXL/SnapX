using LibVLCSharp.Shared;
using SnapX.Core;

namespace SnapX.Avalonia;

public class SnapXAvalonia : Core.SnapX
{
    public override async Task PlaySound(Stream stream)
    {
        DebugHelper.WriteLine($"PlaySound {stream.Length} bytes {stream.Position} {stream.CanSeek} {stream.CanRead}");
        var vlc = new LibVLC(enableDebugLogs: false);
        DebugHelper.WriteLine($"VLC Version: {vlc.Version}");
        var MediaPlayer = new MediaPlayer(vlc);
        var input = new StreamMediaInput(stream);
        var mediaOptions = new[] { ":input-title-format=flac" };

        var media = new Media(vlc, input, mediaOptions);
        media.AddOption(":input-title-format=flac");
        MediaPlayer.EnableHardwareDecoding = true;
        MediaPlayer.Play(media);
        MediaPlayer.Stopped += async (Sender, Args) =>
        {
            await stream.DisposeAsync();
            media.Dispose();
            input.Dispose();
            vlc.Dispose();
        };
    }
}
