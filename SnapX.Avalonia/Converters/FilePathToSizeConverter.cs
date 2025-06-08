using System.Globalization;
using Avalonia.Data.Converters;

namespace SnapX.Avalonia.Converters;

public class FilePathToSizeConverter : IValueConverter
{
    public static readonly FilePathToSizeConverter Instance = new();

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var path = value as string;

        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            return null;

        try
        {
            var fileInfo = new FileInfo(path);
            return fileInfo.Length;
        }
        catch
        {
            return null;
        }
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
