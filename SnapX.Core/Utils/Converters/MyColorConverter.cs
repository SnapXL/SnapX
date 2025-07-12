using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using SixLabors.ImageSharp;

namespace SnapX.Core.Utils.Converters;

public class MyColorConverter : TypeConverter
{
    public override bool CanConvertFrom(ITypeDescriptorContext? context, Type sourceType)
    {
        return sourceType == typeof(string) || base.CanConvertFrom(context, sourceType);
    }

    public override object ConvertFrom(ITypeDescriptorContext? context, System.Globalization.CultureInfo? culture, object value)
    {
        if (value is string colorStr)
        {
            // Try to parse named color first (e.g. "Red")
            var knownColor = TryParseKnownColor(colorStr);
            if (knownColor is not null)
            {
                return knownColor.Value;
            }

            // Try parse hex value (#RRGGBB or #AARRGGBB)
            return colorStr.StartsWith("#") ? Color.ParseHex(colorStr) : throw new FormatException($"Cannot convert '{colorStr}' to ImageSharp Color.");
        }

        return base.ConvertFrom(context, culture, value) ?? throw new FormatException($"Cannot convert '{value}' to ImageSharp Color.");
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
}
