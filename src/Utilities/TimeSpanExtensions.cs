using System;
using System.Globalization;

namespace SqliteMultiTenant.Utilities
{
    /// <summary>
    /// Provides extension methods for <see cref="TimeSpan"/> to format and compare time intervals.
    /// </summary>
    public static class TimeSpanExtensions
    {
        /// <summary>
        /// Converts the <see cref="TimeSpan"/> to a human‑readable string such as "2h 5m".
        /// Days, hours, minutes, and seconds are included only if they are non‑zero.
        /// </summary>
        /// <param name="ts">The time span to format.</param>
        /// <returns>A culture‑invariant human‑readable representation.</returns>
        public static string ToHumanReadable(this TimeSpan ts)
        {
            if (ts == TimeSpan.Zero)
                return "0s";

            var parts = new System.Collections.Generic.List<string>();

            if (ts.Days != 0)
                parts.Add($"{ts.Days}d");

            if (ts.Hours != 0)
                parts.Add($"{ts.Hours}h");

            if (ts.Minutes != 0)
                parts.Add($"{ts.Minutes}m");

            if (ts.Seconds != 0)
                parts.Add($"{ts.Seconds}s");

            // If all components are zero (e.g., milliseconds only), show milliseconds
            if (parts.Count == 0 && ts.Milliseconds != 0)
                parts.Add($"{ts.Milliseconds}ms");

            return string.Join(" ", parts);
        }

        /// <summary>
        /// Converts the <see cref="TimeSpan"/> to a compact string in the format "hh:mm:ss".
        /// The format is culture‑invariant and zero‑padded.
        /// </summary>
        /// <param name="ts">The time span to format.</param>
        /// <returns>A compact representation of the time span.</returns>
        public static string ToCompact(this TimeSpan ts)
        {
            // Use the standard TimeSpan format with culture‑invariant formatting.
            // The "c" format specifier produces a constant format, but we want zero‑padded
            // hours, minutes, and seconds. The custom format @"hh\:mm\:ss" achieves this.
            return ts.ToString(@"hh\:mm\:ss", CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Determines whether the <see cref="TimeSpan"/> is within the specified tolerance.
        /// </summary>
        /// <param name="ts">The time span to evaluate.</param>
        /// <param name="tolerance">The maximum allowed difference.</param>
        /// <returns><c>true</c> if <paramref name="ts"/> is less than or equal to <paramref name="tolerance"/>; otherwise, <c>false</c>.</returns>
        public static bool IsWithin(this TimeSpan ts, TimeSpan tolerance)
        {
            return ts <= tolerance;
        }
    }
}
