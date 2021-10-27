# TimeUtilities

The `TimeUtilities` class provides a comprehensive suite of static helper methods designed to simplify common date and time manipulation tasks within the application. It streamlines operations such as calculating period boundaries, handling business day arithmetic, converting between Unix timestamps and `DateTime` objects, and formatting time intervals for human-readable display, ensuring consistency across time-sensitive logic.

## API

### `FormatTimeSpan(TimeSpan timeSpan)`
*   **Purpose:** Converts a `TimeSpan` into a human-readable string representation.
*   **Parameters:** `timeSpan` (The `TimeSpan` to format).
*   **Returns:** A string formatted as a human-readable duration.

### `FormatRelativeTime(DateTime dateTime)`
*   **Purpose:** Returns a string indicating the time elapsed since or remaining until the specified `DateTime`.
*   **Parameters:** `dateTime` (The target `DateTime` to compare against the current time).
*   **Returns:** A string representation of the relative time (e.g., "5 minutes ago", "in 2 hours").

### `RoundToNearest(DateTime dateTime, TimeSpan interval)`
*   **Purpose:** Rounds a `DateTime` to the nearest specified time interval.
*   **Parameters:** `dateTime` (The `DateTime` to round), `interval` (The `TimeSpan` interval to round to).
*   **Returns:** The rounded `DateTime`.

### `GetStartOfDay(DateTime dateTime)`
*   **Purpose:** Calculates the start of the day (00:00:00) for the provided `DateTime`.
*   **Parameters:** `dateTime` (The `DateTime` to process).
*   **Returns:** A `DateTime` representing the beginning of the day.

### `GetEndOfDay(DateTime dateTime)`
*   **Purpose:** Calculates the end of the day (23:59:59.999) for the provided `DateTime`.
*   **Parameters:** `dateTime` (The `DateTime` to process).
*   **Returns:** A `DateTime` representing the end of the day.

### `GetStartOfWeek(DateTime dateTime)`
*   **Purpose:** Calculates the start of the week for the provided `DateTime`.
*   **Parameters:** `dateTime` (The `DateTime` to process).
*   **Returns:** A `DateTime` representing the start of the week.

### `GetEndOfWeek(DateTime dateTime)`
*   **Purpose:** Calculates the end of the week for the provided `DateTime`.
*   **Parameters:** `dateTime` (The `DateTime` to process).
*   **Returns:** A `DateTime` representing the end of the week.

### `GetStartOfMonth(DateTime dateTime)`
*   **Purpose:** Calculates the first day of the month for the provided `DateTime`.
*   **Parameters:** `dateTime` (The `DateTime` to process).
*   **Returns:** A `DateTime` representing the start of the month.

### `GetEndOfMonth(DateTime dateTime)`
*   **Purpose:** Calculates the last day of the month for the provided `DateTime`.
*   **Parameters:** `dateTime` (The `DateTime` to process).
*   **Returns:** A `DateTime` representing the end of the month.

### `GetStartOfYear(DateTime dateTime)`
*   **Purpose:** Calculates the first day of the year for the provided `DateTime`.
*   **Parameters:** `dateTime` (The `DateTime` to process).
*   **Returns:** A `DateTime` representing the start of the year.

### `GetEndOfYear(DateTime dateTime)`
*   **Purpose:** Calculates the last day of the year for the provided `DateTime`.
*   **Parameters:** `dateTime` (The `DateTime` to process).
*   **Returns:** A `DateTime` representing the end of the year.

### `IsLeapYear(int year)`
*   **Purpose:** Determines if the specified year is a leap year.
*   **Parameters:** `year` (The year to check).
*   **Returns:** `true` if the year is a leap year; otherwise, `false`.

### `GetDaysInMonth(int year, int month)`
*   **Purpose:** Returns the number of days in the specified month of a given year.
*   **Parameters:** `year` (The year), `month` (The month, 1-12).
*   **Returns:** An integer representing the number of days in that month.
*   **Exceptions:** Throws an `ArgumentOutOfRangeException` if the month is not within the range 1-12.

### `AddBusinessDays(DateTime dateTime, int days)`
*   **Purpose:** Adds a specified number of business days (Monday-Friday) to a `DateTime`.
*   **Parameters:** `dateTime` (The starting `DateTime`), `days` (The number of business days to add).
*   **Returns:** A `DateTime` offset by the requested number of business days.

### `GetBusinessDaysBetween(DateTime start, DateTime end)`
*   **Purpose:** Calculates the number of business days between two dates.
*   **Parameters:** `start` (The start date), `end` (The end date).
*   **Returns:** An integer count of business days between the start and end dates.

### `FromUnixTimestamp(long timestamp)`
*   **Purpose:** Converts a Unix timestamp (seconds since the Unix Epoch) to a UTC `DateTime`.
*   **Parameters:** `timestamp` (The Unix timestamp to convert).
*   **Returns:** A `DateTime` object in UTC.

### `ToUnixTimestamp(DateTime dateTime)`
*   **Purpose:** Converts a UTC `DateTime` to a Unix timestamp (seconds since the Unix Epoch).
*   **Parameters:** `dateTime` (The `DateTime` to convert).
*   **Returns:** A `long` representing the Unix timestamp.

### `IsSameDay(DateTime date1, DateTime date2)`
*   **Purpose:** Checks if two `DateTime` instances occur on the same calendar day.
*   **Parameters:** `date1` (The first date), `date2` (The second date).
*   **Returns:** `true` if both dates are on the same day; otherwise, `false`.

### `IsBusinessHours(DateTime dateTime)`
*   **Purpose:** Determines if the provided `DateTime` falls within standard business hours.
*   **Parameters:** `dateTime` (The `DateTime` to check).
*   **Returns:** `true` if the time falls within business hours (Mon-Fri, 09:00-17:00); otherwise, `false`.

### `GetPeriodRange(DateTime date, PeriodType periodType)`
*   **Purpose:** Calculates the start and end `DateTime` for a specified period (e.g., Day, Week, Month, Year).
*   **Parameters:** `date` (The reference `DateTime`), `periodType` (The type of period to calculate).
*   **Returns:** A tuple containing the `Start` and `End` `DateTime` of the period.

## Usage

```csharp
// Example 1: Calculate a deadline in business days
DateTime requestDate = DateTime.UtcNow;
DateTime dueDate = TimeUtilities.AddBusinessDays(requestDate, 5);
Console.WriteLine($"Request submitted on {requestDate.ToShortDateString()}, deadline is {dueDate.ToShortDateString()}");

// Example 2: Get the start and end of the current month
var (start, end) = TimeUtilities.GetPeriodRange(DateTime.UtcNow, PeriodType.Month);
Console.WriteLine($"Current month range: {start.ToShortDateString()} to {end.ToShortDateString()}");
```

## Notes

*   **Thread Safety:** All methods in `TimeUtilities` are `static` and stateless. They do not rely on or modify any instance-specific data, making them inherently thread-safe for concurrent access in multi-threaded applications.
*   **Edge Cases:**
    *   Methods performing arithmetic (like `AddBusinessDays`) handle `DateTime.MinValue` and `DateTime.MaxValue` by potentially throwing `ArgumentOutOfRangeException` if the result exceeds the supported `DateTime` range.
    *   Business day calculations assume a standard Monday through Friday work week and do not account for public holidays; these should be handled externally if required.
    *   Unix timestamp conversions assume the Unix Epoch (January 1, 1970). Input `DateTime` objects are treated as UTC for `ToUnixTimestamp`, and outputs from `FromUnixTimestamp` are returned in UTC.
