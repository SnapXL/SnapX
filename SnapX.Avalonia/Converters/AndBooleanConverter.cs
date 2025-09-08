using System.Globalization;
using Avalonia.Data.Converters;

namespace SnapX.Avalonia.Converters;

public class AndBooleanConverter : IMultiValueConverter
{
    public object Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
    {
        foreach (var value in values)
        {
            if (value is bool b)
            {
                if (!b)
                    return false;
            }
            else
            {
                return false;
            }
        }

        return true;
    }
}

