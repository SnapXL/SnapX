using SixLabors.ImageSharp;

namespace SnapX.Core.Media;

public class Screen
{
    public Rectangle Bounds { get; set; }

    public string Name { get; set; }
    public string Id { get; set; }

    public string Resolution => $"{Bounds.Width}x{Bounds.Height}";

    public double RefreshRate { get; set; }

    // The physical dimensions of the screen (e.g., diagonal size in inches)
    public double DiagonalSizeInches { get; set; }

    public double DPI { get; set; }

    public double ScaleFactor { get; set; }

    public bool IsPrimary { get; set; }

    public ScreenOrientation Orientation { get; set; }

    public SessionType SessionType { get; set; }
    public Screen()
    {
    }
    public Screen(
        string id,
        int width,
        int height,
        string name,
        string resolution,
        double refreshRate,
        double diagonalSizeInches,
        double dpi,
        bool isPrimary,
        ScreenOrientation orientation,
        int x,
        int y,
        double scaleFactor,
        SessionType sessionType)
    {
        Id = id;
        Bounds = new Rectangle(x, y, width, height);
        Name = name;
        RefreshRate = refreshRate;
        DiagonalSizeInches = diagonalSizeInches;
        DPI = dpi;
        IsPrimary = isPrimary;
        Orientation = orientation;
        ScaleFactor = scaleFactor;
        SessionType = sessionType;
    }
    public override string ToString()
    {
        return $"Screen {Name} [Id={Id}, X={Bounds.X}, Y={Bounds.Y}, Width={Bounds.Width}, Height={Bounds.Height}, Orientation={Orientation}]";
    }
}
public enum SessionType
{
    Wayland,
    X11,
    Windows,
    macOS
}
public enum ScreenOrientation
{
    Landscape,
    Portrait
}
