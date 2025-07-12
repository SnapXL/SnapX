using SixLabors.ImageSharp;

namespace SnapX.Core.Media;

public class ImageBeautifierOptions
{
    public int Margin { get; set; }
    public int Padding { get; set; }
    public bool SmartPadding { get; set; }
    public int RoundedCorner { get; set; }
    public int ShadowRadius { get; set; }
    public int ShadowOpacity { get; set; }
    public int ShadowDistance { get; set; }
    public int ShadowAngle { get; set; }
    public Color ShadowColor { get; set; }
    public ImageBeautifierBackgroundType BackgroundType { get; set; }
    public GradientInfo BackgroundGradient { get; set; }
    public Color BackgroundColor { get; set; }
    public string BackgroundImageFilePath { get; set; }

    public ImageBeautifierOptions()
    {
        ResetOptions();
    }

    public void ResetOptions()
    {
        Margin = 80;
        Padding = 40;
        SmartPadding = true;
        RoundedCorner = 20;
        ShadowRadius = 30;
        ShadowOpacity = 80;
        ShadowDistance = 10;
        ShadowAngle = 180;
        ShadowColor = Color.Black;
        BackgroundType = ImageBeautifierBackgroundType.Gradient;
        BackgroundGradient = new GradientInfo(
            LinearGradientMode.Horizontal,
            Color.FromRgba(255, 81, 47, 255),
            Color.FromRgba(221, 36, 118, 255)
        );

        BackgroundColor = Color.FromRgba(34, 34, 34, 255);
        BackgroundImageFilePath = "";
    }
}
