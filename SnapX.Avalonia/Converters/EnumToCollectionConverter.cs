using System.Globalization;
using Avalonia.Data.Converters;

namespace SnapX.Avalonia.Converters;

public class EnumToCollectionConverter : IValueConverter
{
    public static readonly EnumToCollectionConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is Type enumType && enumType.IsEnum)
        {
            return Enum.GetValues(enumType);
        }
        return null;
    }

    public object? ConvertBack(
        object? value,
        Type targetType,
        object? parameter,
        CultureInfo culture
    )
    {
        return null;
    }
}
