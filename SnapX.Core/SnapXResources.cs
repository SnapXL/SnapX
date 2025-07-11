
// SPDX-License-Identifier: GPL-3.0-or-later


using System.ComponentModel;
using System.Runtime.InteropServices;
using SixLabors.ImageSharp;
using SnapX.Core.Utils;
using SnapX.Core.Utils.Converters;
using SnapX.Core.Utils.Extensions;
using SnapX.Core.Utils.Miscellaneous;

namespace SnapX.Core;

public static class SnapXResources
{
    public static string CPU => OsInfo.GetProcessorName();
    public static int CPUCount => Environment.ProcessorCount;
    public static OsInfo.GenericGraphicsInfo graphicsInfo => OsInfo.GetGenericGraphicsInfo();
    public static string Dotnet => RuntimeInformation.FrameworkDescription;
    public static string fancyOsName => Helpers.GetOperatingSystemProductName();
    public static string UserAgent => $"{SnapX.AppName}/{Helpers.GetApplicationVersion()} (+{Links.GitHub})";
}


public class Theme
{
    public string Name { get; set; } = "Dark";

    private Color backgroundColor;

    [TypeConverter(typeof(MyColorConverter))]
    public Color BackgroundColor
    {
        get => backgroundColor;
        set
        {
            if (!value.IsTransparent()) backgroundColor = value;
        }
    }

    private Color lightBackgroundColor;

    [TypeConverter(typeof(MyColorConverter))]
    public Color LightBackgroundColor
    {
        get => lightBackgroundColor;
        set
        {
            if (!value.IsTransparent()) lightBackgroundColor = value;
        }
    }

    private Color darkBackgroundColor;

    [TypeConverter(typeof(MyColorConverter))]
    public Color DarkBackgroundColor
    {
        get => darkBackgroundColor;
        set
        {
            if (!value.IsTransparent()) darkBackgroundColor = value;
        }
    }

    private Color textColor;

    [TypeConverter(typeof(MyColorConverter))]
    public Color TextColor
    {
        get => textColor;
        set
        {
            if (!value.IsTransparent()) textColor = value;
        }
    }

    private Color borderColor;

    [TypeConverter(typeof(MyColorConverter))]
    public Color BorderColor
    {
        get => borderColor;
        set
        {
            if (!value.IsTransparent()) borderColor = value;
        }
    }

    [TypeConverter(typeof(MyColorConverter))]
    public Color CheckerColor { get; set; }

    [TypeConverter(typeof(MyColorConverter))]
    public Color CheckerColor2 { get; set; }

    public int CheckerSize { get; set; } = 15;

    [TypeConverter(typeof(MyColorConverter))]
    public Color LinkColor { get; set; }

    [TypeConverter(typeof(MyColorConverter))]
    public Color MenuHighlightColor { get; set; }

    [TypeConverter(typeof(MyColorConverter))]
    public Color MenuHighlightBorderColor { get; set; }

    [TypeConverter(typeof(MyColorConverter))]
    public Color MenuBorderColor { get; set; }

    [TypeConverter(typeof(MyColorConverter))]
    public Color MenuCheckBackgroundColor { get; set; }
    public record UIFont(string Name, float Size);
    public UIFont MenuFont { get; set; } = new("Inter", 11f);

    public UIFont ContextMenuFont { get; set; } = new("Inter", 11f);

    public int ContextMenuOpacity { get; set; } = 100;

    [Browsable(false)]
    public double ContextMenuOpacityDouble => ContextMenuOpacity.Clamp(10, 100) / 100d;

    [TypeConverter(typeof(MyColorConverter))]
    public Color SeparatorLightColor { get; set; }

    [TypeConverter(typeof(MyColorConverter))]
    public Color SeparatorDarkColor { get; set; }

    [Browsable(false)]
    public bool IsDarkTheme => ColorHelpers.IsDarkColor(BackgroundColor);


    public static Theme DarkTheme => new()
    {
        Name = "Dark",
        BackgroundColor = Color.FromRgba(39, 39, 39, 255),
        LightBackgroundColor = Color.FromRgba(46, 46, 46, 255),
        DarkBackgroundColor = Color.FromRgba(34, 34, 34, 255),
        TextColor = Color.FromRgba(231, 233, 234, 255),
        BorderColor = Color.FromRgba(31, 31, 31, 255),
        CheckerColor = Color.FromRgba(46, 46, 46, 255),
        CheckerColor2 = Color.FromRgba(39, 39, 39, 255),
        LinkColor = Color.FromRgba(166, 212, 255, 255),
        MenuHighlightColor = Color.FromRgba(46, 46, 46, 255),
        MenuHighlightBorderColor = Color.FromRgba(63, 63, 63, 255),
        MenuBorderColor = Color.FromRgba(63, 63, 63, 255),
        MenuCheckBackgroundColor = Color.FromRgba(51, 51, 51, 255),
        SeparatorLightColor = Color.FromRgba(44, 44, 44, 255),
        SeparatorDarkColor = Color.FromRgba(31, 31, 31, 255)
    };

    public static Theme LightTheme => new()
    {
        Name = "Light",
        BackgroundColor = Color.FromRgba(242, 242, 242, 255),
        LightBackgroundColor = Color.FromRgba(247, 247, 247, 255),
        DarkBackgroundColor = Color.FromRgba(235, 235, 235, 255),
        TextColor = Color.FromRgba(69, 69, 69, 255),
        BorderColor = Color.FromRgba(201, 201, 201, 255),
        CheckerColor = Color.FromRgba(247, 247, 247, 255),
        CheckerColor2 = Color.FromRgba(235, 235, 235, 255),
        LinkColor = Color.FromRgba(166, 212, 255, 255),
        MenuHighlightColor = Color.FromRgba(247, 247, 247, 255),
        MenuHighlightBorderColor = Color.FromRgba(96, 143, 226, 255),
        MenuBorderColor = Color.FromRgba(201, 201, 201, 255),
        MenuCheckBackgroundColor = Color.FromRgba(225, 233, 244, 255),
        SeparatorLightColor = Color.FromRgba(253, 253, 253, 255),
        SeparatorDarkColor = Color.FromRgba(189, 189, 189, 255)
    };

    public static Theme NightTheme => new()
    {
        Name = "Night",
        BackgroundColor = Color.FromRgba(42, 47, 56, 255),
        LightBackgroundColor = Color.FromRgba(52, 57, 65, 255),
        DarkBackgroundColor = Color.FromRgba(28, 32, 38, 255),
        TextColor = Color.FromRgba(235, 235, 235, 255),
        BorderColor = Color.FromRgba(28, 32, 38, 255),
        CheckerColor = Color.FromRgba(60, 60, 60, 255),
        CheckerColor2 = Color.FromRgba(50, 50, 50, 255),
        LinkColor = Color.FromRgba(166, 212, 255, 255),
        MenuHighlightColor = Color.FromRgba(30, 34, 40, 255),
        MenuHighlightBorderColor = Color.FromRgba(116, 129, 152, 255),
        MenuBorderColor = Color.FromRgba(22, 26, 31, 255),
        MenuCheckBackgroundColor = Color.FromRgba(56, 64, 75, 255),
        SeparatorLightColor = Color.FromRgba(56, 64, 75, 255),
        SeparatorDarkColor = Color.FromRgba(22, 26, 31, 255)
    };

    // https://www.nordtheme.com
    public static Theme NordDarkTheme => new()
    {
        Name = "Nord Dark",
        BackgroundColor = Color.FromRgba(46, 52, 64, 255),
        LightBackgroundColor = Color.FromRgba(59, 66, 82, 255),
        DarkBackgroundColor = Color.FromRgba(38, 44, 57, 255),
        TextColor = Color.FromRgba(229, 233, 240, 255),
        BorderColor = Color.FromRgba(30, 38, 54, 255),
        CheckerColor = Color.FromRgba(46, 52, 64, 255),
        CheckerColor2 = Color.FromRgba(36, 42, 54, 255),
        LinkColor = Color.FromRgba(136, 192, 208, 255),
        MenuHighlightColor = Color.FromRgba(36, 42, 54, 255),
        MenuHighlightBorderColor = Color.FromRgba(24, 30, 42, 255),
        MenuBorderColor = Color.FromRgba(24, 30, 42, 255),
        MenuCheckBackgroundColor = Color.FromRgba(59, 66, 82, 255),
        SeparatorLightColor = Color.FromRgba(59, 66, 82, 255),
        SeparatorDarkColor = Color.FromRgba(30, 38, 54, 255)
    };

    // https://www.nordtheme.com
    public static Theme NordLightTheme => new()
    {
        Name = "Nord Light",
        BackgroundColor = Color.FromRgba(229, 233, 240, 255),
        LightBackgroundColor = Color.FromRgba(236, 239, 244, 255),
        DarkBackgroundColor = Color.FromRgba(216, 222, 233, 255),
        TextColor = Color.FromRgba(59, 66, 82, 255),
        BorderColor = Color.FromRgba(207, 216, 233, 255),
        CheckerColor = Color.FromRgba(229, 233, 240, 255),
        CheckerColor2 = Color.FromRgba(216, 222, 233, 255),
        LinkColor = Color.FromRgba(106, 162, 178, 255),
        MenuHighlightColor = Color.FromRgba(236, 239, 244, 255),
        MenuHighlightBorderColor = Color.FromRgba(207, 216, 233, 255),
        MenuBorderColor = Color.FromRgba(216, 222, 233, 255),
        MenuCheckBackgroundColor = Color.FromRgba(229, 233, 240, 255),
        SeparatorLightColor = Color.FromRgba(236, 239, 244, 255),
        SeparatorDarkColor = Color.FromRgba(207, 216, 233, 255)
    };

    // https://draculatheme.com
    public static Theme DraculaTheme => new()
    {
        Name = "Dracula",
        BackgroundColor = Color.FromRgba(40, 42, 54, 255),
        LightBackgroundColor = Color.FromRgba(68, 71, 90, 255),
        DarkBackgroundColor = Color.FromRgba(36, 38, 48, 255),
        TextColor = Color.FromRgba(248, 248, 242, 255),
        BorderColor = Color.FromRgba(33, 35, 43, 255),
        CheckerColor = Color.FromRgba(40, 42, 54, 255),
        CheckerColor2 = Color.FromRgba(36, 38, 48, 255),
        LinkColor = Color.FromRgba(98, 114, 164, 255),
        MenuHighlightColor = Color.FromRgba(36, 38, 48, 255),
        MenuHighlightBorderColor = Color.FromRgba(255, 121, 198, 255),
        MenuBorderColor = Color.FromRgba(33, 35, 43, 255),
        MenuCheckBackgroundColor = Color.FromRgba(45, 47, 61, 255),
        SeparatorLightColor = Color.FromRgba(45, 47, 61, 255),
        SeparatorDarkColor = Color.FromRgba(33, 35, 43, 255)
    };

    public static List<Theme> GetDefaultThemes()
    {
        return [DarkTheme, LightTheme, NightTheme, NordDarkTheme, NordLightTheme, DraculaTheme];
    }

    public override string ToString()
    {
        return Name;
    }
}
