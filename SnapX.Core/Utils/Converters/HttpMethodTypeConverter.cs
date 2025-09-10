using System.ComponentModel;
using System.Globalization;

namespace SnapX.Core.Utils.Converters;

public class HttpMethodTypeConverter : TypeConverter
{
    public override bool CanConvertFrom(ITypeDescriptorContext? context, Type sourceType)
    {
        return sourceType == typeof(string) || base.CanConvertFrom(context, sourceType);
    }

    public override bool CanConvertTo(ITypeDescriptorContext? context, Type? destinationType)
    {
        return destinationType == typeof(string) || base.CanConvertTo(context, destinationType);
    }

    public override object? ConvertFrom(ITypeDescriptorContext? context, CultureInfo? culture, object value)
    {
        if (value is string str)
        {
            return new HttpMethod(str.ToUpperInvariant());
        }
        return base.ConvertFrom(context, culture, value);
    }

    public override object? ConvertTo(ITypeDescriptorContext? context, CultureInfo? culture, object? value, Type destinationType)
    {
        if (value is HttpMethod method && destinationType == typeof(string))
        {
            return method.Method;
        }
        return base.ConvertTo(context, culture, value, destinationType);
    }
}
