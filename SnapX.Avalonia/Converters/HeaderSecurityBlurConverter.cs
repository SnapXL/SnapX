using SnapX.Core.Upload.Custom;

namespace SnapX.Avalonia.Converters;

using System.Globalization;
using global::Avalonia.Data.Converters;

public class HeaderSecurityBlurConverter : IValueConverter
{

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        string? key = value as string;
        if (string.IsNullOrWhiteSpace(key))
            return 0.0;

        bool isSensitive = CustomUploaderItem.SensitiveKeys.Any(s =>
            key.Equals(s, StringComparison.OrdinalIgnoreCase)
        );

        return isSensitive ? 20.0 : 0.0;
    }

    public object ConvertBack(
        object? value,
        Type targetType,
        object? parameter,
        CultureInfo culture
    ) => throw new NotImplementedException();
}
