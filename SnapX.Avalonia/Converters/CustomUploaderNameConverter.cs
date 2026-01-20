using System.Globalization;
using Avalonia.Data.Converters;
using SnapX.Core.Utils;

namespace SnapX.Avalonia.Converters;

public class CustomUploaderNameConverter : IMultiValueConverter
{
    public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
    {
        string? name = values.Count > 0 ? values[0] as string : null;
        string? url = values.Count > 1 ? values[1] as string : null;

        if (!string.IsNullOrWhiteSpace(name))
        {
            return name;
        }

        if (!string.IsNullOrWhiteSpace(url))
        {
            return URLHelpers.GetHostName(url);
        }

        return "New Uploader";
    }
}
