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

## ConflictResolutionServiceExtensions

The `ConflictResolutionServiceExtensions` class provides extension methods for detecting, resolving, and applying conflict resolutions in multi-tenant data operations. It supports identifying conflicting fields, determining conflict types, and applying resolution strategies with retry logic.

### Usage Example

```csharp
var original = new BackupData { Id = "1", Name = "Original", Timestamp = DateTime.UtcNow };
var modified = new BackupData { Id = "1", Name = "Modified", Timestamp = DateTime.UtcNow.AddMinutes(-5) };

var detectionResult = ConflictResolutionServiceExtensions.CreateConflictDetectionResult(original, modified);
if (detectionResult.HasConflictType(ConflictType.DataModification))
{
    var conflictingFields = ConflictResolutionServiceExtensions.GetConflictingFields(detectionResult);
    var firstConflict = ConflictResolutionServiceExtensions.GetFirstConflictOfType(detectionResult, ConflictType.DataModification);

    var resolution = new ConflictResolutionStrategy { Strategy = ConflictResolutionStrategyType.KeepNewest };
    var resolutionResult = await ConflictResolutionServiceExtensions.ResolveConflictsAsync(detectionResult, resolution);
    var applied = await ConflictResolutionServiceExtensions.ApplyResolutionWithRetryAsync(resolutionResult, maxRetries: 3);
}
```

// existing content ...
