
// SPDX-License-Identifier: GPL-3.0-or-later


using System.Diagnostics;
using SnapX.Core.Utils.Miscellaneous;

namespace SnapX.Core.Upload.Utils;

public class ProgressManager
{
    private readonly Stopwatch startTimer = new();
    private readonly Stopwatch smoothTimer = new();
    private readonly FixedSizedQueue<double> averageSpeed = new(10);
    private const int smoothTime = 250;
    private long speedTest;
    private double smoothedSpeed;

    public long Position { get; private set; }
    public long Length { get; set; }
    public double Percentage => Length > 0 ? Math.Min(100, (double)Position / Length * 100) : 0;
    public double Speed => smoothedSpeed;
    public TimeSpan Elapsed => startTimer.Elapsed;
    public TimeSpan Remaining
    {
        get
        {
            var currentSpeed = Speed;
            if (!(currentSpeed > 0) || Position >= Length) return TimeSpan.Zero;
            var seconds = (Length - Position) / currentSpeed;
            return seconds > 86400 ? TimeSpan.FromHours(24) : TimeSpan.FromSeconds(seconds);
        }
    }

    public ProgressManager(long length, long position = 0)
    {
        Length = length;
        Position = position;
        startTimer.Start();
        smoothTimer.Start();
    }

    public bool UpdateProgress(long bytesRead)
    {
        return ProcessUpdate(bytesRead, false);
    }

    public bool UpdateAbsoluteProgress(long totalBytesTransferred)
    {
        var bytesDelta = Math.Max(0, totalBytesTransferred - Position);
        return ProcessUpdate(bytesDelta, true, totalBytesTransferred);
    }
    private bool ProcessUpdate(long delta, bool isAbsolute, long absoluteValue = 0)
    {
        if (isAbsolute)
            Position = absoluteValue;
        else
            Position += delta;

        speedTest += delta;

        if (Position >= Length)
        {
            startTimer.Stop();
            smoothTimer.Stop();
            if (smoothedSpeed <= 0)
            {
                var totalSeconds = startTimer.Elapsed.TotalSeconds;
                // Prevent division by zero for near-instant transfers
                smoothedSpeed = totalSeconds > 0 ? Position / totalSeconds : Position;
            }
            return true;
        }

        if (smoothTimer.ElapsedMilliseconds < smoothTime)
            return false;

        var intervalSeconds = smoothTimer.Elapsed.TotalSeconds;
        if (intervalSeconds > 0)
        {
            var currentSpeed = speedTest / intervalSeconds;
            averageSpeed.Enqueue(currentSpeed);

            while (averageSpeed.Count > 8)
                averageSpeed.Dequeue();

            smoothedSpeed = averageSpeed.Average();
        }

        speedTest = 0;
        smoothTimer.Restart();
        return true;
    }

}

