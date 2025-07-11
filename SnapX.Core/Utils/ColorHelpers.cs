using System.Text.RegularExpressions;
using SixLabors.ImageSharp.ColorSpaces;
using SixLabors.ImageSharp.PixelFormats;
using SnapX.Core.Utils.Random;
using Color = SixLabors.ImageSharp.Color;

namespace SnapX.Core.Utils;

public static class ColorHelpers
{
    public static Rgba64[] StandardColors =
    [
        Color.FromRgb(0, 0, 0),
        Color.FromRgb(64, 64, 64),
        Color.FromRgb(255, 0, 0),
        Color.FromRgb(255, 106, 0),
        Color.FromRgb(255, 216, 0),
        Color.FromRgb(182, 255, 0),
        Color.FromRgb(76, 255, 0),
        Color.FromRgb(0, 255, 33),
        Color.FromRgb(0, 255, 144),
        Color.FromRgb(0, 255, 255),
        Color.FromRgb(0, 148, 255),
        Color.FromRgb(0, 38, 255),
        Color.FromRgb(72, 0, 255),
        Color.FromRgb(178, 0, 255),
        Color.FromRgb(255, 0, 220),
        Color.FromRgb(255, 0, 110),
        Color.FromRgb(255, 255, 255),
        Color.FromRgb(128, 128, 128),
        Color.FromRgb(127, 0, 0),
        Color.FromRgb(127, 51, 0),
        Color.FromRgb(127, 106, 0),
        Color.FromRgb(91, 127, 0),
        Color.FromRgb(38, 127, 0),
        Color.FromRgb(0, 127, 14),
        Color.FromRgb(0, 127, 70),
        Color.FromRgb(0, 127, 127),
        Color.FromRgb(0, 74, 127),
        Color.FromRgb(0, 19, 127),
        Color.FromRgb(33, 0, 127),
        Color.FromRgb(87, 0, 127),
        Color.FromRgb(127, 0, 110),
        Color.FromRgb(127, 0, 55)
    ];

    #region Convert Color to ...

    public static string ColorToHex(Rgba64 color, ColorFormat format = ColorFormat.RGB)
    {
        switch (format)
        {
            default:
            case ColorFormat.RGB:
                return string.Format("{0:X2}{1:X2}{2:X2}", color.R, color.G, color.B);
            case ColorFormat.RGBA:
                return string.Format("{0:X2}{1:X2}{2:X2}{3:X2}", color.R, color.G, color.B, color.A);
            case ColorFormat.ARGB:
                return string.Format("{0:X2}{1:X2}{2:X2}{3:X2}", color.A, color.R, color.G, color.B);
        }
    }

    public static int ColorToDecimal(Rgba64 color, ColorFormat format = ColorFormat.RGB)
    {
        switch (format)
        {
            default:
            case ColorFormat.RGB:
                return color.R << 16 | color.G << 8 | color.B;
            case ColorFormat.RGBA:
                return color.R << 24 | color.G << 16 | color.B << 8 | color.A;
            case ColorFormat.ARGB:
                return color.A << 24 | color.R << 16 | color.G << 8 | color.B;
        }
    }


    public static Hsv ColorToHsv(Rgba64 color)
    {
        float hue = 0f;
        float saturation = 0f;
        float brightness = 0f;

        float r = color.R / 65535f;
        float g = color.G / 65535f;
        float b = color.B / 65535f;

        float max = MathHelpers.Max(r, MathHelpers.Max(g, b));
        float min = MathHelpers.Min(r, MathHelpers.Min(g, b));

        brightness = max;
        if (max == 0)
        {
            saturation = 0f;
        }
        else
        {
            saturation = (max - min) / max;
        }

        if (max == min)
        {
            hue = 0f;
        }
        else if (max == r)
        {
            hue = (g - b) / (max - min);
        }
        else if (max == g)
        {
            hue = 2f + (b - r) / (max - min);
        }
        else if (max == b)
        {
            hue = 4f + (r - g) / (max - min);
        }

        hue *= 60f;
        if (hue < 0)
        {
            hue += 360f;
        }

        return new Hsv(hue, saturation, brightness);
    }

    public static Cmyk ColorToCMYK(Rgba64 color)
    {
        // If the color is black (R = G = B = 0), the K (black) is fully 1
        if (color.R == 0 && color.G == 0 && color.B == 0)
        {
            return new Cmyk(0f, 0f, 0f, 1f);  // CMYK for black (K = 1, others = 0)
        }

        // Normalize the RGB values (ImageSharp stores them as byte values, so we divide by 255 to get the range [0, 1])
        var c = 1f - (color.R / 255f);
        var m = 1f - (color.G / 255f);
        var y = 1f - (color.B / 255f);

        // Calculate the key (K) value, which is the minimum of C, M, and Y
        var k = MathHelpers.Min(c, MathHelpers.Min(m, y));

        // Normalize the C, M, and Y values to account for the K value
        if (k < 1f)
        {
            c = (c - k) / (1f - k);
            m = (m - k) / (1f - k);
            y = (y - k) / (1f - k);
        }

        // Return the CMYK color model, using floats for CMYK values
        return new Cmyk(c, m, y, k);
    }

    #endregion Convert Color to ...

    #region Convert Hex to ...

    public static Rgba64 HexToColor(string hex, ColorFormat format = ColorFormat.RGB)
    {
        if (string.IsNullOrEmpty(hex))
        {
            return new Rgba64();
        }

        if (hex[0] == '#')
        {
            hex = hex.Remove(0, 1);
        }
        else if (hex.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
        {
            hex = hex.Remove(0, 2);
        }

        if (((format == ColorFormat.RGBA || format == ColorFormat.ARGB) && hex.Length != 8) ||
            (format == ColorFormat.RGB && hex.Length != 6))
        {
            return new Rgba64();
        }

        int r, g, b, a;

        switch (format)
        {
            default:
            case ColorFormat.RGB:
                r = HexToDecimal(hex.Substring(0, 2));
                g = HexToDecimal(hex.Substring(2, 2));
                b = HexToDecimal(hex.Substring(4, 2));
                a = 255;
                break;
            case ColorFormat.RGBA:
                r = HexToDecimal(hex.Substring(0, 2));
                g = HexToDecimal(hex.Substring(2, 2));
                b = HexToDecimal(hex.Substring(4, 2));
                a = HexToDecimal(hex.Substring(6, 2));
                break;
            case ColorFormat.ARGB:
                a = HexToDecimal(hex.Substring(0, 2));
                r = HexToDecimal(hex.Substring(2, 2));
                g = HexToDecimal(hex.Substring(4, 2));
                b = HexToDecimal(hex.Substring(6, 2));
                break;
        }

        return Color.FromRgba((byte)r, (byte)g, (byte)b, (byte)a);
    }

    public static int HexToDecimal(string hex)
    {
        return Convert.ToInt32(hex, 16);
    }

    #endregion Convert Hex to ...

    #region Convert Decimal to ...

    public static Color DecimalToColor(int dec, ColorFormat format = ColorFormat.RGB)
    {
        switch (format)
        {
            default:
            case ColorFormat.RGB:
                return Color.FromRgba((byte)((dec >> 16) & 0xFF), (byte)((dec >> 8) & 0xFF), (byte)(dec & 0xFF), 255);

            case ColorFormat.RGBA:
                return Color.FromRgba((byte)((dec >> 16) & 0xFF), (byte)((dec >> 8) & 0xFF), (byte)(dec & 0xFF), (byte)((dec >> 24) & 0xFF));

            case ColorFormat.ARGB:
                return Color.FromRgba((byte)((dec >> 16) & 0xFF), (byte)((dec >> 8) & 0xFF), (byte)(dec & 0xFF), (byte)((dec >> 24) & 0xFF));
        }
    }

    public static string DecimalToHex(int dec)
    {
        return dec.ToString("X6");
    }

    #endregion Convert Decimal to ...

    #region Convert CMYK to ...

    public static Color CMYKToColor(Cmyk cmyk)
    {
        if (cmyk.C == 0 && cmyk.M == 0 && cmyk.Y == 0 && cmyk.K == 1)
        {
            return new Rgba64();
        }

        double c = (cmyk.C * (1 - cmyk.K)) + cmyk.K;
        double m = (cmyk.M * (1 - cmyk.K)) + cmyk.K;
        double y = (cmyk.Y * (1 - cmyk.K)) + cmyk.K;

        int r = (int)System.Math.Round((1 - c) * 255);
        int g = (int)System.Math.Round((1 - m) * 255);
        int b = (int)System.Math.Round((1 - y) * 255);

        return Color.FromRgb((byte)r, (byte)g, (byte)b);
    }

    #endregion Convert CMYK to ...
    public static double ValidColor(double number)
    {
        return MathHelpers.Clamp(number, 0.0, 1.0);
    }

    public static int ValidColor(int number)
    {
        return MathHelpers.Clamp(number, 0, 255);
    }

    public static byte ValidColor(byte number)
    {
        return (byte)MathHelpers.Clamp(number, 0, 255);
    }

    public static Color RandomColor()
    {
        return Color.FromRgb((byte)RandomFast.Next(255), (byte)RandomFast.Next(255), (byte)RandomFast.Next(255));
    }

    public static bool ParseColor(string text, out Color color)
    {
        if (!string.IsNullOrEmpty(text))
        {
            text = text.Trim();

            if (text.Length <= 20)
            {
                Match matchHex = Regex.Match(text, @"^(?:#|0x)?((?:[0-9A-F]{2}){3})$", RegexOptions.IgnoreCase);

                if (matchHex.Success)
                {
                    color = HexToColor(matchHex.Groups[1].Value);
                    return true;
                }
                else
                {
                    Match matchRGB = Regex.Match(text, @"^(?:rgb\()?([1]?[0-9]?[0-9]|2[0-4][0-9]|25[0-5])(?:\s|,)+([1]?[0-9]?[0-9]|2[0-4][0-9]|25[0-5])(?:\s|,)+([1]?[0-9]?[0-9]|2[0-4][0-9]|25[0-5])\)?$");

                    if (matchRGB.Success)
                    {
                        color = Color.FromRgb(byte.Parse(matchRGB.Groups[1].Value), byte.Parse(matchRGB.Groups[2].Value), byte.Parse(matchRGB.Groups[3].Value));
                        return true;
                    }
                }
            }
        }

        color = new Rgba64();
        return false;
    }

    public static int PerceivedBrightness(Rgba64 color)
    {
        return (int)Math.Sqrt((color.R * color.R * .299) + (color.G * color.G * .587) + (color.B * color.B * .114));
    }

    public static Color VisibleColor(Color color)
    {
        return VisibleColor(color, Color.White, Color.Black);
    }

    public static Color VisibleColor(Color color, Color lightColor, Color darkColor)
    {
        if (IsLightColor(color))
        {
            return darkColor;
        }

        return lightColor;
    }

    public static bool IsLightColor(Color color)
    {
        return PerceivedBrightness(color) > 130;
    }

    public static bool IsDarkColor(Color color)
    {
        return !IsLightColor(color);
    }

    public static Color Lerp(Rgba64 from, Rgba64 to, float amount)
    {
        return Color.FromRgb((byte)MathHelpers.Lerp(from.G, to.G, amount), (byte)MathHelpers.Lerp(from.B, to.B, amount), (byte)MathHelpers.Lerp(from.R, to.R, amount));
    }

    public static Color DeterministicStringToColor(string text)
    {
        int hash = text.GetHashCode();
        int r = (hash & 0xFF0000) >> 16;
        int g = (hash & 0x00FF00) >> 8;
        int b = hash & 0x0000FF;
        return Color.FromRgb((byte)r, (byte)g, (byte)b);
    }

    public static int ColorDifference(Rgba64 color1, Rgba64 color2)
    {
        int rDiff = System.Math.Abs(color1.R - color2.R);
        int gDiff = System.Math.Abs(color1.G - color2.G);
        int bDiff = System.Math.Abs(color1.B - color2.B);
        return rDiff + gDiff + bDiff;
    }

    public static bool ColorsAreClose(Color color1, Color color2, int threshold)
    {
        return ColorDifference(color1, color2) <= threshold;
    }

    public static Color LighterColor(Color color, float amount)
    {
        return Lerp(color, Color.White, amount);
    }

    public static Color DarkerColor(Color color, float amount)
    {
        return Lerp(color, Color.Black, amount);
    }

    public static List<Color> GetKnownColors()
    {
        List<Color> colors =
        [
            Color.White,
            Color.Black,
            Color.AliceBlue,
            Color.Aquamarine,
            Color.Azure,
            Color.Beige,
            Color.Bisque,
            Color.BurlyWood,
            Color.CadetBlue,
            Color.Coral,
            Color.Crimson,
            Color.Cyan,
            Color.DarkBlue,
            Color.DarkCyan,
            Color.DarkGoldenrod,
            Color.DarkGray,
            Color.DarkGreen,
            Color.DarkMagenta,
            Color.DarkOrange,
            Color.DarkRed,
            Color.DarkSlateBlue,
            Color.DarkSlateGray,
            Color.Red,
            Color.Blue,
            Color.Green,
        ];

        return colors;
    }

    public static Color FindClosestKnownColor(Color color)
    {
        List<Color> colors = GetKnownColors();
        return colors.Aggregate(Color.Black, (accu, curr) => ColorDifference(color, curr) < ColorDifference(color, accu) ? curr : accu);
    }

    public static string GetColorName(Color color)
    {
        Color knownColor = FindClosestKnownColor(color);
        // TODO: Make pigs fly and make this function work as intended.
        return Helpers.GetProperName(knownColor.ToString());
    }
}

