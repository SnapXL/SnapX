using Avalonia.Collections;

namespace SnapX.Avalonia.Extensions;

public static class AvaloniaListExtensions
{
    public static int FindIndex<T>(this AvaloniaList<T> list, Predicate<T> match)
    {
        if (match == null)
            throw new ArgumentNullException(nameof(match));

        for (int i = 0; i < list.Count; i++)
        {
            if (match(list[i]))
                return i;
        }

        return -1;
    }
}
