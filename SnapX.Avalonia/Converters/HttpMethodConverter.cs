using System.Globalization;
using Avalonia.Data.Converters;

namespace SnapX.Avalonia.Converters;

public class HttpMethodTypeConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is HttpMethod method)
        {
            return method.Method;
        }

        return "GET";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is string str)
        {
            return new HttpMethod(str.ToUpperInvariant());
        }

        return HttpMethod.Get;
    }
}
