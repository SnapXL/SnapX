using System.Diagnostics;
using SnapX.Core;
using SnapX.Core.Utils.Extensions;

namespace SnapX.Avalonia;

public class SnapXAvalonia : Core.SnapX
{
    public override async Task PlaySound(Stream stream)
    {
        DebugHelper.WriteLine($"PlaySound {stream.Length} bytes {stream.Position} {stream.CanSeek} {stream.CanRead}");
        var tempFilePath = Path.GetTempFileName();
        stream.Seek(0, SeekOrigin.Begin);
        stream.WriteToFile(tempFilePath);
        var psi = new ProcessStartInfo
        {
            FileName = "ffplay", // Even on Windows, we expect ffplay to be in the $PATH. https://winstall.app/apps/Gyan.FFmpeg
            Arguments = $"-nodisp -autoexit -hide_banner -loglevel warning \"{tempFilePath}\"",
            UseShellExecute = false,
            RedirectStandardOutput = false,
            RedirectStandardError = false,
            CreateNoWindow = true
        };

        try
        {
            using var process = Process.Start(psi);
            if (process is not null) await process.WaitForExitAsync();
        }
        finally
        {
            try
            {
                File.Delete(tempFilePath);
            }
            catch
            {
                /* ignore */
            }
        }

    }
}
