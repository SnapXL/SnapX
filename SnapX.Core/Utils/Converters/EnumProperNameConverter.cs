using System.ComponentModel;
using System.Globalization;

namespace SnapX.Core.Utils.Converters;

public class EnumProperNameConverter(Type Type) : EnumConverter(Type)
{
    private readonly Type enumType = Type;

    public override bool CanConvertTo(ITypeDescriptorContext? context, Type? destType)
    {
        return destType == typeof(string);
    }

    public override object ConvertTo(ITypeDescriptorContext? context, CultureInfo? culture, object? value, Type destType)
    {
        ArgumentNullException.ThrowIfNull(value);
        return Helpers.GetProperName(value.ToString()!);
    }

    public override bool CanConvertFrom(ITypeDescriptorContext? context, Type srcType)
    {
        return srcType == typeof(string);
    }

    public override object ConvertFrom(ITypeDescriptorContext? context, CultureInfo? culture, object value)
    {
        foreach (Enum e in Enum.GetValues(enumType).OfType<Enum>())
        {
            if (Helpers.GetProperName(e.ToString()) == (string)value)
            {
                return e;
            }
        }

        return Enum.Parse(enumType, (string)value);
    }
}
