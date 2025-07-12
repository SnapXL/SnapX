using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using SnapX.Core.Utils.Extensions;

namespace SnapX.Core.Utils.Converters;

public class EnumDescriptionConverter(Type Type) : EnumConverter(Type)
{
    private readonly Type enumType = Type;

    public override bool CanConvertTo(ITypeDescriptorContext? context, Type? destType)
    {
        return destType == typeof(string);
    }

    public override object ConvertTo(ITypeDescriptorContext? context, CultureInfo? culture, object? value, Type destType)
    {
        return ((Enum)value!).GetLocalizedDescription();
    }

    public override bool CanConvertFrom(ITypeDescriptorContext? context, Type srcType)
    {
        return srcType == typeof(string);
    }
    [RequiresUnreferencedCode("Uses Enum.GetValues")]
    public override object ConvertFrom(ITypeDescriptorContext? context, CultureInfo? culture, object value)
    {
        foreach (var e in Enum.GetValues(enumType).OfType<Enum>())
        {
            if (e.GetLocalizedDescription() == (string)value)
            {
                return e;
            }
        }

        return Enum.Parse(enumType, (string)value);
    }
}
