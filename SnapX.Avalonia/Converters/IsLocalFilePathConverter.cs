using System.Globalization;
using Avalonia.Data.Converters;

namespace SnapX.Avalonia.Converters;


public class IsLocalFilePathConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is string str && !string.IsNullOrWhiteSpace(str))
        {
            // If it's NOT a valid absolute URI, we assume it's a file path
            return !Uri.IsWellFormedUriString(str, UriKind.Absolute);
        }

        return false;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
