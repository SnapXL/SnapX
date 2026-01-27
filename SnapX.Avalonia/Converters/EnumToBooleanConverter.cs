using System.Globalization;
using Avalonia.Data.Converters;

namespace SnapX.Avalonia.Converters;

public class EnumToBooleanConverter : IMultiValueConverter
{
    // Convert: Enum (Current Value) -> Boolean (IsChecked)
    // public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    // {
    //     if (value is Enum current && parameter is Enum target)
    //     {
    //         DebugHelper.WriteLine(
    //             "EnumToBooleanConverter Convert: " + current.ToString() + " | " + target.ToString()
    //         );
    //         return current.HasFlag(target);
    //     }
    //     else
    //     {
    //         DebugHelper.WriteLine(
    //             "EnumToBooleanConverter Convert: false, value is not Enum or parameter is not Enum"
    //         );
    //         DebugHelper.WriteLine(
    //             "Value Type: " + (value?.GetType().ToString() ?? "null") + $"({value})"
    //         );
    //         DebugHelper.WriteLine(
    //             "Parameter Type: " + (parameter?.GetType().ToString() ?? "null") + $"({parameter})"
    //         );
    //     }
    //     return false;
    // }

    public object Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
    {
        // values[0] = SelectedUploader.DestinationType
        // values[1] = Current Item in ItemsControl (The specific Enum value)
        if (values.Count >= 2 && values[0] is Enum current && values[1] is Enum target)
        {
            return current.HasFlag(target);
        }
        return false;
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
