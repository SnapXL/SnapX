
// SPDX-License-Identifier: GPL-3.0-or-later


using System.Diagnostics;
using System.Text;
using SixLabors.ImageSharp;
using SnapX.Core.Media;
using SnapX.Core.Utils;

namespace SnapX.Core.SharpCapture.ScreenRecording;

public class ScreenRecorder : IDisposable
{
    public bool IsRecording { get; private set; }

    public int FPS
    {
        get
        {
            return fps;
        }
        set
        {
            if (!IsRecording)
            {
                fps = value;
                UpdateInfo();
            }
        }
    }

    public float DurationSeconds
    {
        get
        {
            return durationSeconds;
        }
        set
        {
            if (!IsRecording)
            {
                durationSeconds = value;
                UpdateInfo();
            }
        }
    }

    public Rectangle CaptureRectangle
    {
        get
        {
            return captureRectangle;
        }
        private set
        {
            if (!IsRecording)
            {
                captureRectangle = value;
            }
        }
    }

    public string? CachePath { get; private set; }

    public ScreenRecordOutput OutputType { get; private set; }

    public ScreenRecordingOptions Options { get; set; }

    public event Action RecordingStarted;

    public delegate void ProgressEventHandler(int progress);
    public event ProgressEventHandler EncodingProgressChanged;

    private int fps, delay, frameCount, previousProgress;
    private float durationSeconds;
    private Screenshot screenshot;
    private Rectangle captureRectangle;
    private ImageCache imgCache;
    private FFmpegCLIManager ffmpeg;
    private bool stopRequested;

    public ScreenRecorder(ScreenRecordOutput outputType, ScreenRecordingOptions options, Screenshot screenshot, Rectangle captureRectangle)
    {
        if (string.IsNullOrEmpty(options.OutputPath))
        {
            throw new Exception("Screen recorder cache path is empty.");
        }

        FPS = options.FPS;
        DurationSeconds = options.Duration;
        CaptureRectangle = captureRectangle;
        CachePath = options.OutputPath;
        OutputType = outputType;

        Options = options;

        switch (OutputType)
        {
            default:
            case ScreenRecordOutput.FFmpeg:
                FileHelpers.CreateDirectoryFromFilePath(Options.OutputPath);
                ffmpeg = new FFmpegCLIManager(Options.FFmpeg.FFmpegPath);
                ffmpeg.ShowError = true;
                ffmpeg.EncodeStarted += OnRecordingStarted;
                ffmpeg.EncodeProgressChanged += OnEncodingProgressChanged;
                break;
            case ScreenRecordOutput.GIF:
                imgCache = new HardDiskCache(Options);
                break;
        }

        this.screenshot = screenshot;
    }

    private void UpdateInfo()
    {
        delay = 1000 / fps;
        frameCount = (int)(fps * durationSeconds);
    }

    public void StartRecording()
    {
        if (!IsRecording)
        {
            IsRecording = true;
            stopRequested = false;

            if (OutputType == ScreenRecordOutput.FFmpeg)
            {
                ffmpeg.Run(Options.GetFFmpegCommands());
            }
            else
            {
                OnRecordingStarted();
                RecordUsingCache();
            }
        }

        IsRecording = false;
    }

    private void RecordUsingCache()
    {
        try
        {
            for (int i = 0; !stopRequested && (frameCount == 0 || i < frameCount); i++)
            {
                Stopwatch timer = Stopwatch.StartNew();

                Image img = screenshot.CaptureRectangle(CaptureRectangle);
                //DebugHelper.WriteLine("Screen capture: " + (int)timer.ElapsedMilliseconds);

                imgCache.AddImageAsync(img);

                if (!stopRequested && (frameCount == 0 || i + 1 < frameCount))
                {
                    int sleepTime = delay - (int)timer.ElapsedMilliseconds;

                    if (sleepTime > 0)
                    {
                        Thread.Sleep(sleepTime);
                    }
                    else if (sleepTime < 0)
                    {
                        // Need to handle FPS drops
                    }
                }
            }
        }
        finally
        {
            imgCache.Finish();
        }
    }

    public void StopRecording()
    {
        stopRequested = true;

        if (ffmpeg != null)
        {
            ffmpeg.Close();
        }
    }

    public void SaveAsGIF(string? path, GIFQuality quality)
    {
        if (imgCache != null && imgCache is HardDiskCache && !IsRecording)
        {
            FileHelpers.CreateDirectoryFromFilePath(path);

            var hdCache = imgCache as HardDiskCache;
            // TODO: Reimplement AnimatedGIFCreator
            // using (AnimatedGifCreator gifEncoder = new AnimatedGifCreator(path, delay))
            // {
            //     int i = 0;
            //     int count = hdCache.Count;
            //
            //     foreach (Image img in hdCache.GetImageEnumerator())
            //     {
            //         i++;
            //         OnEncodingProgressChanged((int)((float)i / count * 100));
            //
            //         using (img)
            //         {
            //             gifEncoder.AddFrame(img, quality);
            //         }
            //     }
            // }
        }
    }

    public bool FFmpegEncodeVideo(string input, string? output)
    {
        FileHelpers.CreateDirectoryFromFilePath(output);

        Options.IsRecording = false;
        Options.IsLossless = false;
        Options.InputPath = input;
        Options.OutputPath = output;

        try
        {
            ffmpeg.TrackEncodeProgress = true;

            return ffmpeg.Run(Options.GetFFmpegCommands());
        }
        finally
        {
            ffmpeg.TrackEncodeProgress = false;
        }
    }

    public bool FFmpegEncodeAsGIF(string input, string? output)
    {
        FileHelpers.CreateDirectoryFromFilePath(output);

        try
        {
            ffmpeg.TrackEncodeProgress = true;

            StringBuilder args = new StringBuilder();

            args.Append($"-i \"{input}\" ");

            // https://ffmpeg.org/ffmpeg-filters.html#palettegen-1
            args.Append($"-lavfi \"palettegen=stats_mode={Options.FFmpeg.GIFStatsMode}[palette],");

            // https://ffmpeg.org/ffmpeg-filters.html#paletteuse
            args.Append($"[0:v][palette]paletteuse=dither={Options.FFmpeg.GIFDither}");

            if (Options.FFmpeg.GIFDither == FFmpegPaletteUseDither.bayer)
            {
                args.Append($":bayer_scale={Options.FFmpeg.GIFBayerScale}");
            }

            if (Options.FFmpeg.GIFStatsMode == FFmpegPaletteGenStatsMode.single)
            {
                args.Append(":new=1");
            }

            args.Append("\" ");
            args.Append("-y ");
            args.Append($"\"{output}\"");

            return ffmpeg.Run(args.ToString());
        }
        finally
        {
            ffmpeg.TrackEncodeProgress = false;
        }
    }

    protected void OnRecordingStarted()
    {
        RecordingStarted?.Invoke();
    }

    protected void OnEncodingProgressChanged(float progress)
    {
        int currentProgress = (int)progress;

        if (EncodingProgressChanged != null && currentProgress != previousProgress)
        {
            EncodingProgressChanged(currentProgress);
            previousProgress = currentProgress;
        }
    }

    public void Dispose()
    {
        if (ffmpeg != null)
        {
            ffmpeg.Dispose();
        }

        if (imgCache != null)
        {
            imgCache.Dispose();
        }
    }
}
