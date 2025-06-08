using System.Globalization;
using Avalonia.Data.Converters;

namespace SnapX.Avalonia.Converters;

public class FilePathHasSizeToBoolConverter : IValueConverter
{
    public static readonly FilePathHasSizeToBoolConverter Instance = new();

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not string path || string.IsNullOrWhiteSpace(path))
            return false;

        try
        {
            var fileInfo = new FileInfo(path);
            return fileInfo.Exists && fileInfo.Length > 0;
        }
        catch
        {
            return false;
        }
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
