using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace SnapX.Core.Utils.Converters;

public class EnumProperNameKeepCaseConverter(Type Type) : EnumConverter(Type)
{
    private Type enumType = Type;

    public override bool CanConvertTo(ITypeDescriptorContext context, Type destType)
    {
        return destType == typeof(string);
    }

    public override object ConvertTo(ITypeDescriptorContext? context, CultureInfo? culture, object? value, Type destType)
    {
        return Helpers.GetProperName(value.ToString(), true);
    }

    public override bool CanConvertFrom(ITypeDescriptorContext? context, Type srcType)
    {
        return srcType == typeof(string);
    }

    [RequiresUnreferencedCode("Uses Enum.GetValues")]
    public override object ConvertFrom(ITypeDescriptorContext? context, CultureInfo? culture, object value)
    {
        foreach (Enum e in Enum.GetValues(enumType).OfType<Enum>())
        {
            if (Helpers.GetProperName(e.ToString(), true) == (string)value)
            {
                return e;
            }
        }

        return Enum.Parse(enumType, (string)value);
    }
}
