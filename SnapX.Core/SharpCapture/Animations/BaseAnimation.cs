
// SPDX-License-Identifier: GPL-3.0-or-later


using System.Diagnostics;

namespace SnapX.Core.ScreenCapture.Animations;

internal class BaseAnimation
{
    public virtual bool IsActive { get; protected set; }

    protected Stopwatch Timer { get; private set; }
    protected TimeSpan TotalElapsed { get; private set; }
    protected TimeSpan Elapsed { get; private set; }

    protected TimeSpan previousElapsed;

    public BaseAnimation()
    {
        Timer = new Stopwatch();
    }

    public virtual void Start()
    {
        IsActive = true;
        Timer.Restart();
    }

    public virtual void Stop()
    {
        Timer.Stop();
        IsActive = false;
    }

    public virtual bool Update()
    {
        if (IsActive)
        {
            TotalElapsed = Timer.Elapsed;
            Elapsed = TotalElapsed - previousElapsed;
            previousElapsed = TotalElapsed;
        }

        return IsActive;
    }
}
