
// SPDX-License-Identifier: GPL-3.0-or-later


using SixLabors.ImageSharp;

namespace SnapX.Core.ScreenCapture.Helpers;

public class InputManager
{
    public Point MousePosition { get; set; } = Point.Empty;

    public Point PreviousMousePosition { get; set; } = Point.Empty;

    public Point ClientMousePosition { get; set; } = Point.Empty;

    public Point PreviousClientMousePosition { get; set; } = Point.Empty;

    public Point MouseVelocity => new Point(ClientMousePosition.X - PreviousClientMousePosition.X, ClientMousePosition.Y - PreviousClientMousePosition.Y);

    public bool IsMouseMoved => MouseVelocity.X != 0 || MouseVelocity.Y != 0;

}
