# DateTimeExtensions

The `DateTimeExtensions` class provides a comprehensive set of static helper methods for manipulating, querying, and formatting `DateTime` and `TimeSpan` values. These utilities facilitate common operations such as calculating durations, handling scheduling logic, and enforcing retention policies, ensuring consistent time-based operations throughout the `sqlite-multi-tenant` project.

## API

| Member | Description |
| :--- | :--- |
| `IsExpired` | Determines if a specified timestamp occurs before a reference expiration time. |
| `GetAgeDays` | Calculates the number of full days elapsed between a specified date and the current time. |
| `ToIso8601String` | Formats a `DateTime` object into an ISO 8601 compliant string representation. |
| `IsWithinRetentionWindow` | Validates whether a given date falls within a defined `TimeSpan` retention window relative to the current time. |
| `GetNextScheduledTime` | Computes the next anticipated execution time based on a base `DateTime` and a recurrence `TimeSpan` interval. |
| `ToHumanReadableDuration` | Converts a `TimeSpan` duration into a human-friendly string representation. |
| `IsCreatedToday` | Evaluates whether a given `DateTime` corresponds to the current calendar date. |
| `StartOfDayUtc` | Returns a `DateTime` representing the start (00:00:00.000) of the day in UTC for a provided date. |
| `EndOfDayUtc` | Returns a `DateTime` representing the end (23:59:59.999) of the day in UTC for a provided date. |
| `RoundDownToMinute` | Normalizes a `DateTime` by setting the seconds and milliseconds components to zero. |

## Usage

### Example 1: Checking Resource Retention
```csharp
DateTime recordDate = GetRecordDateFromDatabase();
TimeSpan retentionWindow = TimeSpan.FromDays(30);

if (recordDate.IsWithinRetentionWindow(retentionWindow))
{
    // Process the record within the valid window
}
else
{
    // Initiate cleanup for expired record
}
```

### Example 2: Normalizing and Scheduling
```csharp
DateTime lastRun = DateTime.UtcNow;
DateTime normalizedLastRun = lastRun.RoundDownToMinute();

DateTime nextRun = normalizedLastRun.GetNextScheduledTime(TimeSpan.FromHours(1));
Console.WriteLine($"Next scheduled run: {nextRun.ToIso8601String()}");
```

## Notes

*   **Thread Safety:** All methods within `DateTimeExtensions` are static and stateless, relying solely on input parameters. They are thread-safe and can be called concurrently from multiple threads without side effects.
*   **Time Zones:** Methods explicitly returning UTC values (e.g., `StartOfDayUtc`, `EndOfDayUtc`) assume the input `DateTime` is treated in a UTC context. Ensure appropriate conversion using `DateTime.ToUniversalTime()` before passing local `DateTime` objects if UTC-specific behavior is required.
*   **Precision:** The `RoundDownToMinute` method affects only the seconds and sub-second components of the `DateTime` structure; it does not adjust the date or hour components.
*   **Edge Cases:** Calculations involving `GetAgeDays` and `IsWithinRetentionWindow` are relative to `DateTime.UtcNow`. Ensure system clock synchronization across distributed environments if high precision is required for these operations.
