// SPDX-License-Identifier: GPL-3.0-or-later


using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace SnapX.Core.Utils.Extensions;

public static class EnumExtensions
{
    public const string HotkeyType_Category_Upload = "Upload";
    public const string HotkeyType_Category_ScreenCapture = "ScreenCapture";
    public const string HotkeyType_Category_ScreenRecord = "ScreenRecord";
    public const string HotkeyType_Category_Tools = "Tools";
    public const string HotkeyType_Category_Other = "Other";

    public static string GetDescription(this Enum value)
    {
        var field = value.GetType().GetField(value.ToString());
        return field?.GetCustomAttribute<DescriptionAttribute>()?.Description ?? value.ToString();
    }

    public static string GetLocalizedDescription(this Enum value) => value.GetDescription();

    public static string GetLocalizedCategory(this Enum value)
    {
        var field = value.GetType().GetField(value.ToString());
        return Lang.ResourceManager.GetString(field?.GetCustomAttribute<CategoryAttribute>()?.Category ?? value.ToString());
    }

    [RequiresDynamicCode("Uploader")]
    public static int GetIndex(this Enum value) => Array.IndexOf(Enum.GetValues(value.GetType()) as Array, value);

    public static IEnumerable<T> GetFlags<T>(this T value) where T : struct, Enum =>
        Enum.GetValues<T>()
        .Where(flag => Convert.ToUInt64(flag) != 0 && value.HasFlag(flag));


    public static bool HasFlag<T>(this Enum value, params T[] flags)
    {
        var keysVal = Convert.ToUInt64(value);
        var flagVal = flags.Select(x => Convert.ToUInt64(x)).Aggregate((x, next) => x | next);
        return (keysVal & flagVal) == flagVal;
    }

    public static bool HasFlagAny<T>(this Enum value, params T[] flags) => flags.Any(x => value.HasFlag(x));
    public static T Add<T>(this Enum value, params T[] flags) where T : Enum
    {
        var result = Convert.ToUInt64(value);
        result |= flags.Select(flag => Convert.ToUInt64(flag)).Aggregate(result, (current, next) => current | next);
        return (T)Enum.ToObject(typeof(T), result);
    }


    public static T Remove<T>(this Enum value, params T[] flags)
    {
        var keysVal = Convert.ToUInt64(value);
        var flagVal = flags.Select(x => Convert.ToUInt64(x)).Aggregate((x, next) => x | next);
        return (T)Enum.ToObject(typeof(T), keysVal & ~flagVal);
    }

    public static T Swap<T>(this Enum value, params T[] flags)
    {
        var keysVal = Convert.ToUInt64(value);
        var flagVal = flags.Select(x => Convert.ToUInt64(x)).Aggregate((x, next) => x | next);
        return (T)Enum.ToObject(typeof(T), keysVal ^ flagVal);
    }

    [RequiresDynamicCode("Uploader")]
    public static T Next<T>(this Enum value)
    {
        var values = Enum.GetValues(value.GetType());
        var i = Array.IndexOf(values, value) + 1;
        return i == values.Length ? (T)values.GetValue(0) : (T)values.GetValue(i);
    }

    public static T Previous<T>(this Enum value)
    {
        var values = Enum.GetValues(value.GetType());
        var i = Array.IndexOf(values, value) - 1;
        return i == -1 ? (T)values.GetValue(values.Length - 1) : (T)values.GetValue(i);
    }
}

