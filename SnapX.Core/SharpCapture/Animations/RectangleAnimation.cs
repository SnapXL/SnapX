
// SPDX-License-Identifier: GPL-3.0-or-later


using SixLabors.ImageSharp;
using SnapX.Core.Utils;

namespace SnapX.Core.ScreenCapture.Animations;

internal class RectangleAnimation : BaseAnimation
{
    public RectangleF FromRectangle { get; set; }
    public RectangleF ToRectangle { get; set; }
    public TimeSpan Duration { get; set; }

    public RectangleF CurrentRectangle { get; private set; }

    public override bool Update()
    {
        if (IsActive)
        {
            base.Update();

            var amount = (float)Timer.Elapsed.Ticks / Duration.Ticks;
            amount = Math.Min(amount, 1);

            var x = MathHelpers.Lerp(FromRectangle.X, ToRectangle.X, amount);
            var y = MathHelpers.Lerp(FromRectangle.Y, ToRectangle.Y, amount);
            var width = MathHelpers.Lerp(FromRectangle.Width, ToRectangle.Width, amount);
            var height = MathHelpers.Lerp(FromRectangle.Height, ToRectangle.Height, amount);

            CurrentRectangle = new RectangleF(x, y, width, height);

            if (amount >= 1)
            {
                Stop();
            }
        }

        return IsActive;
    }
}
