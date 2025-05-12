namespace SnapX.Core.Media;

public interface Screen
{
    // The screen's unique identifier (e.g., name or ID)
    string Id { get; }

    // The screen's width in pixels
    int Width { get; }

    // The screen's height in pixels
    int Height { get; }

    // The screen's resolution (e.g., "1920x1080")
    string Resolution { get; }

    double RefreshRate { get; }
    string Index { get; }

    // The physical dimensions of the screen (e.g., diagonal size in inches)
    double DiagonalSizeInches { get; }

    double DPI { get; }

    // Indicates if the screen is currently the primary display
    bool IsPrimary { get; }

    ScreenOrientation Orientation { get; }

    // The X-coordinate of the screen's top-left corner (relative to the main display)
    int X { get; }

    // The Y-coordinate of the screen's top-left corner (relative to the main display)
    int Y { get; }
}

public enum ScreenOrientation
{
    Landscape,
    Portrait
}
