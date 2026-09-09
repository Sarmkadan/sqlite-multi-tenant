#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace SqliteMultiTenant.Utilities;

/// <summary>
/// Advanced datetime and time manipulation utilities.
/// Provides formatting, conversion, and calculation helpers for time operations.
/// </summary>
public static class TimeUtilities
{
    private const int BusinessHoursStart = 9;
    private const int BusinessHoursEnd = 17;
    private const int SecondsInMinute = 60;
    private const int MinutesInHour = 60;
    private const int HoursInDay = 24;
    private const int DaysInWeek = 7;
    private const int DaysInMonth = 30;
    private const int DaysInYear = 365;

    /// <summary>
    /// Formats a timespan as a human-readable string.
    /// Example: "2 days, 3 hours, 45 minutes"
    /// </summary>
    public static string FormatTimeSpan(TimeSpan span)
    {
        var parts = new List<string>();

        if (span.Days > 0)
            parts.Add($"{span.Days} day{(span.Days != 1 ? "s" : "")}");

        if (span.Hours > 0)
            parts.Add($"{span.Hours} hour{(span.Hours != 1 ? "s" : "")}");

        if (span.Minutes > 0)
            parts.Add($"{span.Minutes} minute{(span.Minutes != 1 ? "s" : "")}");

        if (span.Seconds > 0 && parts.Count < 3)
            parts.Add($"{span.Seconds} second{(span.Seconds != 1 ? "s" : "")}");

        return parts.Count > 0 ? string.Join(", ", parts) : "less than a second";
    }

    /// <summary>
    /// Formats a DateTime as a relative time string.
    /// Example: "5 hours ago", "in 2 days"
    /// </summary>
    public static string FormatRelativeTime(DateTime dateTime)
    {
        var now = DateTime.UtcNow;
        var span = now - dateTime;

        if (span.TotalSeconds < SecondsInMinute)
            return "just now";

        if (span.TotalMinutes < MinutesInHour)
            return $"{(int)span.TotalMinutes} minute{(span.TotalMinutes != 1 ? "s" : "")} ago";

        if (span.TotalHours < HoursInDay)
            return $"{(int)span.TotalHours} hour{(span.TotalHours != 1 ? "s" : "")} ago";

        if (span.TotalDays < DaysInWeek)
        {
            int weeks = (int)(span.TotalDays / DaysInWeek);
            return $"{weeks} week{(weeks != 1 ? "s" : "")} ago";
        }

        if (span.TotalDays < DaysInMonth)
        {
            int months = (int)(span.TotalDays / DaysInMonth);
            return $"{months} month{(months != 1 ? "s" : "")} ago";
        }

        if (span.TotalDays < DaysInYear)
        {
            int years = (int)(span.TotalDays / DaysInYear);
            return $"{years} year{(years != 1 ? "s" : "")} ago";
        }

        return $"{(int)(span.TotalDays / DaysInYear)} year{(span.TotalDays / DaysInYear != 1 ? "s" : "")} ago";
    }

    /// <summary>
    /// Rounds a DateTime to the nearest specified interval.
    /// </summary>
    public static DateTime RoundToNearest(DateTime dateTime, TimeSpan interval)
    {
        var offset = dateTime.Ticks % interval.Ticks;
        var delta = offset < (interval.Ticks / 2) ? -offset : (interval.Ticks - offset);

        return dateTime.AddTicks(delta);
    }

    /// <summary>
    /// Gets the start of the day (00:00:00) for a given DateTime.
    /// </summary>
    public static DateTime GetStartOfDay(DateTime dateTime)
    {
        return dateTime.Date;
    }

    /// <summary>
    /// Gets the end of the day (23:59:59.999) for a given DateTime.
    /// </summary>
    public static DateTime GetEndOfDay(DateTime dateTime)
    {
        return dateTime.Date.AddDays(1).AddTicks(-1);
    }

    /// <summary>
    /// Gets the start of the week (Monday) for a given DateTime.
    /// </summary>
    public static DateTime GetStartOfWeek(DateTime dateTime)
    {
        int daysToMonday = (int)dateTime.DayOfWeek - 1;
        if (daysToMonday < 0)
            daysToMonday = 6;

        return dateTime.AddDays(-daysToMonday).Date;
    }

    /// <summary>
    /// Gets the end of the week (Sunday) for a given DateTime.
    /// </summary>
    public static DateTime GetEndOfWeek(DateTime dateTime)
    {
        return GetStartOfWeek(dateTime).AddDays(7).AddTicks(-1);
    }

    /// <summary>
    /// Gets the start of the month for a given DateTime.
    /// </summary>
    public static DateTime GetStartOfMonth(DateTime dateTime)
    {
        return new DateTime(dateTime.Year, dateTime.Month, 1);
    }

    /// <summary>
    /// Gets the end of the month for a given DateTime.
    /// </summary>
    public static DateTime GetEndOfMonth(DateTime dateTime)
    {
        return GetStartOfMonth(dateTime).AddMonths(1).AddTicks(-1);
    }

    /// <summary>
    /// Gets the start of the year for a given DateTime.
    /// </summary>
    public static DateTime GetStartOfYear(DateTime dateTime)
    {
        return new DateTime(dateTime.Year, 1, 1);
    }

    /// <summary>
    /// Gets the end of the year for a given DateTime.
    /// </summary>
    public static DateTime GetEndOfYear(DateTime dateTime)
    {
        return new DateTime(dateTime.Year, 12, 31, 23, 59, 59, 999);
    }

    /// <summary>
    /// Checks if a DateTime is in a leap year.
    /// </summary>
    public static bool IsLeapYear(DateTime dateTime)
    {
        return DateTime.IsLeapYear(dateTime.Year);
    }

    /// <summary>
    /// Gets the number of days in a month.
    /// </summary>
    public static int GetDaysInMonth(DateTime dateTime)
    {
        return DateTime.DaysInMonth(dateTime.Year, dateTime.Month);
    }

    /// <summary>
    /// Adds business days to a DateTime, excluding weekends.
    /// </summary>
    public static DateTime AddBusinessDays(DateTime dateTime, int days)
    {
        int sign = days < 0 ? -1 : 1;
        int absdays = Math.Abs(days);

        var date = dateTime;
        for (int i = 0; i < absdays; i++)
        {
            do
            {
                date = date.AddDays(sign);
            } while (date.DayOfWeek == DayOfWeek.Saturday || date.DayOfWeek == DayOfWeek.Sunday);
        }

        return date;
    }

    /// <summary>
    /// Gets the number of business days between two dates.
    /// </summary>
    public static int GetBusinessDaysBetween(DateTime start, DateTime end)
    {
        int count = 0;
        var current = start;

        while (current < end)
        {
            if (current.DayOfWeek != DayOfWeek.Saturday && current.DayOfWeek != DayOfWeek.Sunday)
                count++;

            current = current.AddDays(1);
        }

        return count;
    }

    /// <summary>
    /// Converts Unix timestamp to DateTime.
    /// </summary>
    public static DateTime FromUnixTimestamp(long unixTimestamp)
    {
        var epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        return epoch.AddSeconds(unixTimestamp);
    }

    /// <summary>
    /// Converts DateTime to Unix timestamp.
    /// </summary>
    public static long ToUnixTimestamp(DateTime dateTime)
    {
        var epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        return (long)(dateTime.ToUniversalTime() - epoch).TotalSeconds;
    }

    /// <summary>
    /// Checks if two DateTime values are on the same day.
    /// </>
    public static bool IsSameDay(DateTime date1, DateTime date2)
    {
        return date1.Date == date2.Date;
    }

    /// <summary>
    /// Checks if a DateTime is during business hours (9 AM - 5 PM).
    /// </summary>
    public static bool IsBusinessHours(DateTime dateTime)
    {
        return dateTime.DayOfWeek != DayOfWeek.Saturday &&
               dateTime.DayOfWeek != DayOfWeek.Sunday &&
               dateTime.Hour >= BusinessHoursStart &&
               dateTime.Hour < BusinessHoursEnd;
    }

    /// <summary>
    /// Creates a time range for a given period.
    /// </summary>
    public static (DateTime Start, DateTime End) GetPeriodRange(DateTime date, TimePeriod period)
    {
        return period switch
        {
            TimePeriod.Day => (GetStartOfDay(date), GetEndOfDay(date)),
            TimePeriod.Week => (GetStartOfWeek(date), GetEndOfWeek(date)),
            TimePeriod.Month => (GetStartOfMonth(date), GetEndOfMonth(date)),
            TimePeriod.Year => (GetStartOfYear(date), GetEndOfYear(date)),
            _ => (date, date)
        };
    }
}

public enum TimePeriod
{
    Day,
    Week,
    Month,
    Year
}