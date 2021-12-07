// existing content ...

## BackupExtensions

The `BackupExtensions` class provides a set of extension methods for working with backups. 
It allows you to easily determine the type of backup, calculate saved space, and format human-readable information.

### Usage Example

```csharp
var backup = new Backup
{
    BackupType = BackupType.Full,
    SizeBytes = 1024000,
    DurationMs = 2500,
    IsSystem = true
};

Console.WriteLine(backup.GetHumanReadableSize()); // Output: 1.00 MB
Console.WriteLine(backup.GetHumanReadableDuration()); // Output: 2.5 seconds
Console.WriteLine(BackupExtensions.IsFullBackup(backup)); // Output: True
Console.WriteLine(BackupExtensions.IsSystemBackup(backup)); // Output: True
Console.WriteLine(BackupExtensions.GetSavedSpaceBytes(backup)); // Output: 1024000
``` 

// existing content ...
