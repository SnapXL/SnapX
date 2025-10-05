
// SPDX-License-Identifier: GPL-3.0-or-later


using System.Diagnostics;
using SnapX.Core.Utils.Miscellaneous;

namespace SnapX.Core.Upload.Utils;

public class ProgressManager
{
    public long Position { get; private set; }
    public long Length { get; set; }

    public double Percentage => (double)Position / Length * 100;

    public double Speed { get; private set; }

    public TimeSpan Elapsed => startTimer.Elapsed;

    public TimeSpan Remaining
    {
        get
        {
            if (Speed > 0)
            {
                return TimeSpan.FromSeconds((Length - Position) / Speed);
            }

            return TimeSpan.Zero;
        }
    }

    private Stopwatch startTimer = new();
    private Stopwatch smoothTimer = new();
    private int smoothTime = 250;
    private long speedTest;
    private FixedSizedQueue<double> averageSpeed = new(10);

    public ProgressManager(long length, long position = 0)
    {
        Length = length;
        Position = position;
        startTimer.Start();
        smoothTimer.Start();
    }

    public bool UpdateProgress(long bytesRead)
    {
        Position += bytesRead;
        speedTest += bytesRead;

        if (Position >= Length)
        {
            startTimer.Stop();
            Speed = Length / Math.Max(startTimer.Elapsed.TotalSeconds, 1);
            return true;
        }

        if (smoothTimer.ElapsedMilliseconds < smoothTime)
            return false;

        double intervalSeconds = smoothTimer.Elapsed.TotalSeconds;
        if (intervalSeconds > 0)
        {
            double currentSpeed = speedTest / intervalSeconds;
            averageSpeed.Enqueue(currentSpeed);
            while (averageSpeed.Count > 5)
                averageSpeed.Dequeue();
            Speed = averageSpeed.Average();
        }

        speedTest = 0;
        smoothTimer.Restart();

        return true;
    }

    public bool UpdateAbsoluteProgress(long totalBytesTransferred)
    {
        long bytesDelta = totalBytesTransferred - Position;
        if (bytesDelta < 0)
            bytesDelta = 0;

        Position = totalBytesTransferred;
        speedTest += bytesDelta;

        if (Position >= Length)
        {
            startTimer.Stop();
            Speed = Length / Math.Max(startTimer.Elapsed.TotalSeconds, 1);
            return true;
        }

        if (smoothTimer.ElapsedMilliseconds < smoothTime)
            return false;

        double intervalSeconds = smoothTimer.Elapsed.TotalSeconds;
        if (intervalSeconds > 0)
        {
            double currentSpeed = speedTest / intervalSeconds;
            averageSpeed.Enqueue(currentSpeed);
            while (averageSpeed.Count > 5)
                averageSpeed.Dequeue();
            Speed = averageSpeed.Average();
        }

        speedTest = 0;
        smoothTimer.Restart();

        return true;
    }

}

