
// SPDX-License-Identifier: GPL-3.0-or-later


using SixLabors.ImageSharp;

namespace SnapX.Core.ScreenCapture.Animations;

internal class PointAnimation : BaseAnimation
{
    public Point FromPosition { get; set; }
    public Point ToPosition { get; set; }
    public TimeSpan Duration { get; set; }

    public Point CurrentPosition { get; private set; }

    public override bool Update()
    {
        if (IsActive)
        {
            base.Update();

            float amount = (float)Timer.Elapsed.Ticks / Duration.Ticks;
            amount = Math.Min(amount, 1);

            CurrentPosition = new Point(
                (int)(FromPosition.X + (ToPosition.X - FromPosition.X) * amount),
                (int)(FromPosition.Y + (ToPosition.Y - FromPosition.Y) * amount)
            );

            if (amount >= 1)
            {
                Stop();
            }
        }

        return IsActive;
    }
}
