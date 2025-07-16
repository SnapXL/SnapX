// SPDX-License-Identifier: GPL-3.0-or-later


using System.Diagnostics;

namespace SnapX.Core.Utils.Miscellaneous;

public class FPSManager
{
    public event Action FPSUpdated;

    public int FPS { get; private set; }
    public int FPSLimit { get; set; }

    private int frameCount;
    private Stopwatch fpsTimer, frameTimer;

    public FPSManager()
    {
        fpsTimer = new Stopwatch();
        frameTimer = new Stopwatch();
    }

    public FPSManager(int fpsLimit) : this()
    {
        FPSLimit = fpsLimit;
    }

    protected void OnFPSUpdated()
    {
        FPSUpdated?.Invoke();
    }

    public void Update()
    {
        frameCount++;

        if (!fpsTimer.IsRunning) fpsTimer.Start();
        else if (fpsTimer.ElapsedMilliseconds >= 1000)
        {
            FPS = (int)System.Math.Round(frameCount / fpsTimer.Elapsed.TotalSeconds);

            OnFPSUpdated();

            frameCount = 0;
            fpsTimer.Restart();
        }

        if (FPSLimit > 0)
        {
            if (!frameTimer.IsRunning)
            {
                frameTimer.Start();
            }
            else
            {
                double currentFrameDuration = frameTimer.Elapsed.TotalMilliseconds;
                double targetFrameDuration = 1000d / FPSLimit;

                if (currentFrameDuration < targetFrameDuration)
                {
                    int sleepDuration = (int)System.Math.Round(targetFrameDuration - currentFrameDuration);

                    if (sleepDuration > 0)
                    {
                        Thread.Sleep(sleepDuration);
                    }
                }

                frameTimer.Restart();
            }
        }
    }
}

