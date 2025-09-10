using System.Globalization;
using Avalonia.Data.Converters;
using SnapX.Core.Upload;

namespace SnapX.Avalonia.Converters;

public class CustomUploaderBodyConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is CustomUploaderBody body)
            return body.ToString();
        return "None";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is string str && Enum.TryParse<CustomUploaderBody>(str, out var result))
            return result;

        return CustomUploaderBody.None;
    }
}

