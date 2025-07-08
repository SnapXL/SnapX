
// SPDX-License-Identifier: GPL-3.0-or-later


using System.Drawing;

namespace SnapX.Core.ScreenCapture.Helpers;

public class SnapSize
{
    private const int MinimumWidth = 2;

    private int width;

    public int Width
    {
        get
        {
            return width;
        }
        set
        {
            width = Math.Max(value, MinimumWidth);
        }
    }

    private const int MinimumHeight = 2;

    private int height;

    public int Height
    {
        get
        {
            return height;
        }
        set
        {
            height = Math.Max(value, MinimumHeight);
        }
    }

    public SnapSize()
    {
        width = MinimumWidth;
        height = MinimumHeight;
    }

    public SnapSize(int width, int height)
    {
        Width = width;
        Height = height;
    }

    public static implicit operator Size(SnapSize size)
    {
        return new Size(size.Width, size.Height);
    }

    public override string ToString()
    {
        return $"{Width}x{Height}";
    }
}
