using System.Globalization;
using Avalonia.Data.Converters;

namespace SnapX.Avalonia.Converters;

public class FilePathToSizeConverter : IValueConverter
{
    public static readonly FilePathToSizeConverter Instance = new();

    // The complete IEC binary prefix scale
    private static readonly string[] Units =
    {
        "B",
        "KiB",
        "MiB",
        "GiB",
        "TiB",
        "PiB",
        "EiB",
        "ZiB",
        "YiB",
    };

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        decimal bytes = 0;

        try
        {
            if (value is string path && Directory.Exists(path))
            {
                bytes = new DirectoryInfo(path)
                    .EnumerateFiles("*", SearchOption.AllDirectories)
                    .Sum(fi => (decimal)fi.Length);
            }
            else if (value is string filePath && File.Exists(filePath))
            {
                bytes = new FileInfo(filePath).Length;
            }
            else if (value is decimal d)
                bytes = d;
            else if (value is long l)
                bytes = l;
            else
                return "0 B";

            if (bytes == 0)
                return "0 B";

            int i = 0;
            while (bytes >= 1024 && i < Units.Length - 1)
            {
                bytes /= 1024;
                i++;
            }
            var format = bytes >= 100 ? "0" : "0.##";
            return $"{bytes.ToString(format, culture)} {Units[i]}";
        }
        catch
        {
            return "Error";
        }
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
