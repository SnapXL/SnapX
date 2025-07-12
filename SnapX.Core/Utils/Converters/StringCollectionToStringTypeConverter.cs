using System.ComponentModel;
using System.Globalization;

namespace SnapX.Core.Utils.UITypeEditors;

public class StringCollectionToStringTypeConverter : TypeConverter
{
    public override object ConvertTo(ITypeDescriptorContext? context, CultureInfo? culture, object? value, Type destinationType)
    {
        ArgumentNullException.ThrowIfNull(value);
        if (destinationType != typeof(string)) return base.ConvertTo(context, culture, value, destinationType) ?? throw new FormatException($"Cannot convert '{value}' to ImageSharp Color.");
        var list = (List<string>)value;
        return string.Join(", ", list);

    }
}
