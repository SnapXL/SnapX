using System.Globalization;
using Avalonia.Data.Converters;
using SnapX.Core.Utils.Extensions;

namespace SnapX.Avalonia.Converters;

public class EnumDescriptionConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is Enum e)
        {
            return e.GetLocalizedDescription();
        }
        return value?.ToString();
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}
