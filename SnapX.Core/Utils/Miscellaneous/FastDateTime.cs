// SPDX-License-Identifier: GPL-3.0-or-later



namespace SnapX.Core.Utils.Miscellaneous;

public static class FastDateTime
{
    public static TimeSpan LocalUtcOffset { get; private set; }

    public static DateTime Now
    {
        get
        {
            return ToLocalTime(DateTime.UtcNow);
        }
    }

    public static long NowUnix
    {
        get
        {
            return (DateTime.UtcNow.Ticks - 621355968000000000) / 10000000;
        }
    }

    static FastDateTime()
    {
        LocalUtcOffset = TimeZoneInfo.Local.GetUtcOffset(DateTime.Now);
    }

    public static DateTime ToLocalTime(DateTime dateTime)
    {
        return dateTime + LocalUtcOffset;
    }
}

