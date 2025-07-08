
// SPDX-License-Identifier: GPL-3.0-or-later


using System.Drawing;

namespace SnapX.Core.ScreenCapture.Animations;

internal class TextAnimation : OpacityAnimation
{
    public string Text { get; set; }
    public Point Position { get; set; }
}
