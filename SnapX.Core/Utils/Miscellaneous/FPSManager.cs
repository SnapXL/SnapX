// SPDX-License-Identifier: GPL-3.0-or-later


using System.Diagnostics;

namespace SnapX.Core.Utils.Miscellaneous;

public class FPSManager(int FPSLimit)
{
    public event Action FPSUpdated;

    public int FPS { get; private set; }

    private int frameCount;
    private readonly Stopwatch fpsTimer = new();
    private readonly Stopwatch frameTimer = new();

    private void OnFPSUpdated()
    {
        FPSUpdated.Invoke();
    }

    public void Update()
    {
        frameCount++;

        if (!fpsTimer.IsRunning) fpsTimer.Start();
        else if (fpsTimer.ElapsedMilliseconds >= 1000)
        {
            FPS = (int)Math.Round(frameCount / fpsTimer.Elapsed.TotalSeconds);

            OnFPSUpdated();

            frameCount = 0;
            fpsTimer.Restart();
        }

        if (FPSLimit <= 0) return;
        if (!frameTimer.IsRunning)
        {
            frameTimer.Start();
        }
        else
        {
            var currentFrameDuration = frameTimer.Elapsed.TotalMilliseconds;
            var targetFrameDuration = 1000d / FPSLimit;

            if (currentFrameDuration < targetFrameDuration)
            {
                var sleepDuration = (int)Math.Round(targetFrameDuration - currentFrameDuration);

                if (sleepDuration > 0)
                {
                    Thread.Sleep(sleepDuration);
                }
            }

            frameTimer.Restart();
        }
    }
}

