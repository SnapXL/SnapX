using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using SixLabors.ImageSharp;

namespace SnapX.Core.Utils.Converters;

public class MyColorConverter : TypeConverter
{
    public override bool CanConvertFrom(ITypeDescriptorContext? context, Type sourceType)
    {
        return sourceType == typeof(string) || base.CanConvertFrom(context, sourceType);
    }

    public override bool CanConvertTo(ITypeDescriptorContext? context, Type? destinationType)
    {
        return destinationType == typeof(string) || base.CanConvertTo(context, destinationType);
    }

    public override object ConvertFrom(ITypeDescriptorContext? context, System.Globalization.CultureInfo? culture, object value)
    {
        if (value is not string colorStr)
            return base.ConvertFrom(context, culture, value) ??
                   throw new FormatException($"Cannot convert '{value}' to ImageSharp Color.");
        // Try to parse named color first (e.g. "Red")
        var knownColor = TryParseKnownColor(colorStr);
        if (knownColor is not null)
        {
            return knownColor.Value;
        }

        // Try parse hex value (#RRGGBB or #AARRGGBB)
        return colorStr.StartsWith("#") ? Color.ParseHex(colorStr) : throw new FormatException($"Cannot convert '{colorStr}' to ImageSharp Color.");

    }

    public override object? ConvertTo(ITypeDescriptorContext? context, CultureInfo? culture, object? value, Type destinationType)
    {
        if (destinationType != typeof(string) || value is not Color color)
            return base.ConvertTo(context, culture, value, destinationType);
        var knownColorName = TryGetKnownColorName(color);
        return !string.IsNullOrEmpty(knownColorName) ? knownColorName :
            // If not a named color, return its hexadecimal representation
            color.ToHex();

    }

    [UnconditionalSuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code", Justification = "<Pending>")]
    [RequiresUnreferencedCode("Checks SixLabors predefined colors")]
    private Color? TryParseKnownColor(string name)
    {
        var colorProperty = typeof(Color).GetProperty(name, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
        if (colorProperty is not null && colorProperty.PropertyType == typeof(Color))
        {
            return (Color)colorProperty.GetValue(null)!;
        }
        return null;
    }

    [UnconditionalSuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code", Justification = "<Pending>")]
    [RequiresUnreferencedCode("Checks SixLabors predefined colors")]
    private string? TryGetKnownColorName(Color color)
    {
        var properties = typeof(Color).GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
        foreach (var property in properties)
        {
            if (property.PropertyType == typeof(Color) && Equals(property.GetValue(null), color))
            {
                return property.Name;
            }
        }
        return null;
    }
}
