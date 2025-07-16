// SPDX-License-Identifier: GPL-3.0-or-later


using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace SnapX.Core.Utils.Extensions;

public static class Extensions
{
    public static void ForEach<T>(this IEnumerable<T> source, Action<T> action)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(action);

        foreach (var item in source)
        {
            action(item);
        }
    }

    public static bool IsValidIndex<T>(this T[] array, int index)
    {
        return index >= 0 && index < array.Length;
    }

    public static bool IsValidIndex<T>(this List<T> list, int index)
    {
        return index >= 0 && index < list.Count;
    }

    public static T ReturnIfValidIndex<T>(this T[] array, int index)
    {
        if (array.IsValidIndex(index)) return array[index];
        return default;
    }

    public static T ReturnIfValidIndex<T>(this List<T> list, int index)
    {
        if (list.IsValidIndex(index)) return list[index];
        return default;
    }

    public static T Last<T>(this T[] array, int index = 0)
    {
        if (array.Length > index) return array[array.Length - index - 1];
        return default;
    }

    public static T Last<T>(this List<T> list, int index = 0)
    {
        if (list.Count > index) return list[list.Count - index - 1];
        return default;
    }

    public static double ToDouble(this Version value)
    {
        return (System.Math.Max(value.Major, 0) * System.Math.Pow(10, 12)) +
            (System.Math.Max(value.Minor, 0) * System.Math.Pow(10, 9)) +
            (System.Math.Max(value.Build, 0) * System.Math.Pow(10, 6)) +
            System.Math.Max(value.Revision, 0);
    }

    public static bool IsValid(this Rectangle rect)
    {
        return rect.Width > 0 && rect.Height > 0;
    }

    public static bool IsValid(this RectangleF rect) => rect.Width > 0 && rect.Height > 0;

    public static Point Add(this Point point, int offsetX, int offsetY) =>
        new Point(point.X + offsetX, point.Y + offsetY);

    public static Point Add(this Point point, Point offset) => new Point(point.X + offset.X, point.Y + offset.Y);

    public static Point Add(this Point point, int offset) => point.Add(offset, offset);
    public static PointF Add(this PointF point, float offsetX, float offsetY) => new PointF(point.X + offsetX, point.Y + offsetY);
    public static PointF Add(this PointF point, PointF offset) => new PointF(point.X + offset.X, point.Y + offset.Y);

    public static PointF Scale(this Point point, float scaleFactor) => new PointF(point.X * scaleFactor, point.Y * scaleFactor);

    public static PointF Scale(this PointF point, float scaleFactor) => new PointF(point.X * scaleFactor, point.Y * scaleFactor);
    public static Point Round(this PointF point) => Point.Round(point);

    public static void Offset(this PointF point, PointF offset)
    {
        point.X += offset.X;
        point.Y += offset.Y;
    }

    public static Size Offset(this Size size, int offset) => size.Offset(offset, offset);
    public static Size Offset(this Size size, int width, int height) => new Size(size.Width + width, size.Height + height);

    public static Rectangle Offset(this Rectangle rect, int offset) => new Rectangle(rect.X - offset, rect.Y - offset, rect.Width + (offset * 2), rect.Height + (offset * 2));
    public static RectangleF Offset(this RectangleF rect, float offset) => new RectangleF(rect.X - offset, rect.Y - offset, rect.Width + (offset * 2), rect.Height + (offset * 2));

    public static RectangleF Scale(this RectangleF rect, float scaleFactor) => new RectangleF(rect.X * scaleFactor, rect.Y * scaleFactor, rect.Width * scaleFactor, rect.Height * scaleFactor);

    public static Rectangle Round(this RectangleF rect) => Rectangle.Round(rect);

    public static Rectangle LocationOffset(this Rectangle rect, int x, int y) => new Rectangle(rect.X + x, rect.Y + y, rect.Width, rect.Height);

    public static RectangleF LocationOffset(this RectangleF rect, float x, float y) => new RectangleF(rect.X + x, rect.Y + y, rect.Width, rect.Height);

    public static RectangleF LocationOffset(this RectangleF rect, PointF offset) => rect.LocationOffset(offset.X, offset.Y);

    public static Rectangle LocationOffset(this Rectangle rect, Point offset) => rect.LocationOffset(offset.X, offset.Y);
    public static Rectangle LocationOffset(this Rectangle rect, int offset) => rect.LocationOffset(offset, offset);

    public static Rectangle SizeOffset(this Rectangle rect, int width, int height) => new Rectangle(rect.X, rect.Y, rect.Width + width, rect.Height + height);

    public static RectangleF SizeOffset(this RectangleF rect, float width, float height) => new RectangleF(rect.X, rect.Y, rect.Width + width, rect.Height + height);
    public static Rectangle SizeOffset(this Rectangle rect, int offset) => rect.SizeOffset(offset, offset);

    public static RectangleF SizeOffset(this RectangleF rect, float offset) => rect.SizeOffset(offset, offset);
    public static string Join<T>(this T[] array, string separator = " ") =>
        array?.Length > 0 ? string.Join(separator, array) : string.Empty;


    public static int WeekOfYear(this DateTime dateTime) =>
        CultureInfo.CurrentCulture.Calendar.GetWeekOfYear(dateTime, CalendarWeekRule.FirstDay, DayOfWeek.Monday);

    [RequiresUnreferencedCode("Uploader")]
    public static void ApplyDefaultPropertyValues(this object self) =>
        TypeDescriptor.GetProperties(self)
            .Cast<PropertyDescriptor>()
            .ToList()
            .ForEach(prop =>
            {
                if (prop.Attributes[typeof(DefaultValueAttribute)] is DefaultValueAttribute attr)
                {
                    prop.SetValue(self, attr.Value);
                }
            });




    public static string GetDescription(this Type type)
    {
        var attributes = (DescriptionAttribute[])type.GetCustomAttributes(typeof(DescriptionAttribute), false);
        return attributes.Length > 0 ? attributes[0].Description : type.Name;
    }

    public static IEnumerable<T> TakeLast<T>(this IEnumerable<T> source, int count) =>
        source.Reverse().Take(count).Reverse();

    public static Version Normalize(this Version version, bool ignoreRevision = false, bool ignoreBuild = false, bool ignoreMinor = false)
    {
        return new Version(System.Math.Max(version.Major, 0),
            ignoreMinor ? 0 : System.Math.Max(version.Minor, 0),
            ignoreBuild ? 0 : System.Math.Max(version.Build, 0),
            ignoreRevision ? 0 : System.Math.Max(version.Revision, 0));
    }

    public static void Move<T>(this List<T> list, int oldIndex, int newIndex)
    {
        var obj = list[oldIndex];
        list.RemoveAt(oldIndex);
        list.Insert(newIndex, obj);
    }

    public static Rectangle Combine(this IEnumerable<Rectangle> rects) =>
        rects.Aggregate(Rectangle.Empty, (result, rect) => result.IsEmpty ? rect : Rectangle.Union(result, rect));


    public static RectangleF Combine(this IEnumerable<RectangleF> rects) =>
        rects.Aggregate(RectangleF.Empty, (result, rect) => result.IsEmpty ? rect : RectangleF.Union(result, rect));

    public static RectangleF AddPoint(this RectangleF rect, PointF point)
    {
        return RectangleF.Union(rect, new RectangleF(point, new SizeF(1, 1)));
    }

    public static RectangleF CreateRectangle(this IEnumerable<PointF> points) =>
        points.Aggregate(RectangleF.Empty, (rect, point) => rect.IsEmpty ? new RectangleF(point, new Size(1, 1)) : rect.AddPoint(point));


    public static Point Center(this Rectangle rect)
    {
        return new Point(rect.X + (rect.Width / 2), rect.Y + (rect.Height / 2));
    }

    public static PointF Center(this RectangleF rect)
    {
        return new PointF(rect.X + (rect.Width / 2), rect.Y + (rect.Height / 2));
    }

    public static float Area(this RectangleF rect) => rect.Width * rect.Height;

    public static float Perimeter(this RectangleF rect) =>
        2 * (rect.Width + rect.Height);

    public static PointF Restrict(this PointF point, RectangleF rect)
    {
        point.X = System.Math.Max(point.X, rect.X);
        point.Y = System.Math.Max(point.Y, rect.Y);
        point.X = System.Math.Min(point.X, rect.X + rect.Width - 1);
        point.Y = System.Math.Min(point.Y, rect.Y + rect.Height - 1);
        return point;
    }

    public static void ShowError(this Exception e, bool fullError = true)
    {
        var error = fullError ? e.ToString() : e.Message;
        // TODO: Remove all calls of this function. It shows that they're still handling UI concerns.
        DebugHelper.WriteException(error);
    }

    public static Task ContinueInCurrentContext(this Task task, Action action) =>
        task.ContinueWith(t => action(), TaskScheduler.FromCurrentSynchronizationContext());


    public static List<T> Range<T>(this List<T> source, int start, int end) =>
        source.GetRange(System.Math.Min(start, end), System.Math.Abs(end - start) + 1);


    public static List<T> Range<T>(this List<T> source, T start, T end)
    {
        int startIndex = source.IndexOf(start);
        if (startIndex == -1) return [];

        int endIndex = source.IndexOf(end);
        if (endIndex == -1) return [];

        return Range(source, startIndex, endIndex);
    }

    public static bool IsTransparent(this Color color)
    {
        return color.ToPixel<Rgba32>().A < 255;
    }
    public static string ToStringProper(this Rectangle rect) =>
        $"X: {rect.X}, Y: {rect.Y}, Width: {rect.Width}, Height: {rect.Height}";
}

