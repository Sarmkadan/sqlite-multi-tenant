# BackupExtensions

`BackupExtensions` is a static helper class designed to provide diagnostic and formatting utilities for backup objects within the `sqlite-multi-tenant` framework. It enables developers to easily query backup status and transform raw backup metrics—such as byte counts and temporal durations—into human-readable formats suitable for logging, user interfaces, or reporting.

## API

### GetSavedSpaceBytes
Calculates the total amount of disk space reclaimed or saved by a specific backup process compared to a baseline.

- **Parameters:** Accepts a `Backup` instance (or appropriate context).
- **Return Value:** `long` representing the number of bytes saved.
- **Exceptions:** Throws `ArgumentNullException` if the provided backup instance is null.

### IsFullBackup
Indicates whether the specified backup operation is classified as a full backup, encompassing all data.

- **Parameters:** Accepts a `Backup` instance.
- **Return Value:** `bool` returning `true` if it is a full backup; otherwise, `false`.
- **Exceptions:** Throws `ArgumentNullException` if the provided backup instance is null.

### IsSystemBackup
Determines if the backup operation is related to system-level configuration or infrastructure, rather than user-tenant data.

- **Parameters:** Accepts a `Backup` instance.
- **Return Value:** `bool` returning `true` if it is a system-level backup; otherwise, `false`.
- **Exceptions:** Throws `ArgumentNullException` if the provided backup instance is null.

### GetHumanReadableSize
Converts a raw byte count (e.g., from `GetSavedSpaceBytes`) into a localized, human-readable string representation (e.g., "1.5 GB", "450 MB").

- **Parameters:** `long` value representing size in bytes.
- **Return Value:** `string` representing the formatted size.
- **Exceptions:** None.

### GetHumanReadableDuration
Converts a raw time duration (e.g., `TimeSpan`) into a human-readable string (e.g., "2 minutes, 15 seconds").

- **Parameters:** `TimeSpan` value representing the duration.
- **Return Value:** `string` representing the formatted duration.
- **Exceptions:** None.

## Usage

### Example 1: Logging Backup Summary
```csharp
public void LogBackupDetails(Backup backup)
{
    var savedSpace = BackupExtensions.GetSavedSpaceBytes(backup);
    var isFull = BackupExtensions.IsFullBackup(backup);
    
    Console.WriteLine($"Backup Status: {(isFull ? "Full" : "Incremental")}");
    Console.WriteLine($"Space Saved: {BackupExtensions.GetHumanReadableSize(savedSpace)}");
}
```

### Example 2: Displaying Backup Duration
```csharp
public string FormatDuration(TimeSpan duration)
{
    // Utility for displaying how long a backup took
    return BackupExtensions.GetHumanReadableDuration(duration);
}
```

## Notes

- **Edge Cases:** `GetSavedSpaceBytes` may return `0` if the backup process did not result in measurable space savings or if the baseline is unavailable. `GetHumanReadableSize` and `GetHumanReadableDuration` handle zero or negative inputs gracefully by returning standard representations (e.g., "0 bytes" or "0 seconds").
- **Thread Safety:** `BackupExtensions` is a static class containing pure helper methods and does not maintain internal state. All methods are thread-safe, provided the input objects passed into them are accessed in a thread-safe manner by the caller.
