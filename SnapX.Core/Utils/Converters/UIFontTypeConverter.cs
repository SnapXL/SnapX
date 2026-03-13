
using System.ComponentModel;
using System.Globalization;

namespace SnapX.Core.Utils.Converters;

public class UIFontTypeConverter : TypeConverter
{
    public override bool CanConvertFrom(ITypeDescriptorContext? context, Type sourceType) =>
        sourceType == typeof(string) || base.CanConvertFrom(context, sourceType);

    public override bool CanConvertTo(ITypeDescriptorContext? context, Type? destinationType) =>
        destinationType == typeof(string) || base.CanConvertTo(context, destinationType);

    public override object ConvertFrom(ITypeDescriptorContext? context, CultureInfo? culture, object value)
    {
        if (value is string text)
        {
            var parts = text.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (parts.Length == 2)
            {
                var sizePart = parts[1].Replace("pt", "", StringComparison.OrdinalIgnoreCase).Trim();
                if (float.TryParse(sizePart, NumberStyles.Float, CultureInfo.InvariantCulture, out var size))
                {
                    return new Theme.UIFont(parts[0], size);
                }
            }
            throw new FormatException($"Invalid format for UIFont: '{text}'. Expected 'Name, Size' (size may include 'pt' suffix).");
        }
        return base.ConvertFrom(context, culture, value);
    }

    public override object? ConvertTo(ITypeDescriptorContext? context, CultureInfo? culture, object? value, Type destinationType)
    {
        if (destinationType == typeof(string) && value is Theme.UIFont font)
        {
            return $"{font.Name}, {font.Size.ToString(CultureInfo.InvariantCulture)}pt";
        }
        return base.ConvertTo(context, culture, value, destinationType);
    }
}
