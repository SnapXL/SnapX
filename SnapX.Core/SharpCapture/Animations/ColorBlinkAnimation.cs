
// SPDX-License-Identifier: GPL-3.0-or-later


using SixLabors.ImageSharp;
using SnapX.Core.Utils;

namespace SnapX.Core.ScreenCapture.Animations;

internal class ColorBlinkAnimation : BaseAnimation
{
    public Color FromColor { get; set; }
    public Color ToColor { get; set; }
    public TimeSpan Duration { get; set; }

    public Color CurrentColor { get; set; }

    private bool backward;

    public override bool Update()
    {
        if (IsActive)
        {
            base.Update();

            float amount = (float)Timer.Elapsed.Ticks / Duration.Ticks;

            if (backward)
            {
                amount = 1 - amount;
            }

            if (amount > 1)
            {
                amount = 1;
                backward = true;
                Start();
            }
            else if (amount < 0)
            {
                amount = 0;
                backward = false;
                Start();
            }

            CurrentColor = ColorHelpers.Lerp(FromColor, ToColor, amount);
        }

        return IsActive;
    }
}
