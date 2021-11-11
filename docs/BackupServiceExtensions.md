# BackupServiceExtensions
The `BackupServiceExtensions` class provides a set of extension methods for working with backup services in a multi-tenant SQLite environment. These methods enable developers to easily check for the existence of backups, retrieve the latest completed backup, count the number of completed backups, and determine if any backups exist.

## API
* `public static async Task<bool> ExistsAsync`: Checks if a backup exists. Returns `true` if a backup exists, `false` otherwise. This method does not take any parameters and does not throw any exceptions.
* `public static async Task<Backup?> GetLatestCompletedBackupAsync`: Retrieves the latest completed backup. Returns the latest completed `Backup` object, or `null` if no completed backups exist. This method does not take any parameters and does not throw any exceptions.
* `public static async Task<int> GetCompletedBackupCountAsync`: Counts the number of completed backups. Returns the number of completed backups as an integer. This method does not take any parameters and does not throw any exceptions.
* `public static async Task<bool> HasBackupsAsync`: Checks if any backups exist. Returns `true` if backups exist, `false` otherwise. This method does not take any parameters and does not throw any exceptions.

## Usage
The following examples demonstrate how to use the `BackupServiceExtensions` class:
```csharp
// Check if a backup exists
bool backupExists = await BackupServiceExtensions.ExistsAsync();
if (backupExists)
{
    Console.WriteLine("A backup exists.");
}
else
{
    Console.WriteLine("No backup exists.");
}

// Retrieve the latest completed backup
Backup? latestBackup = await BackupServiceExtensions.GetLatestCompletedBackupAsync();
if (latestBackup != null)
{
    Console.WriteLine($"Latest backup: {latestBackup}");
}
else
{
    Console.WriteLine("No completed backups exist.");
}
```

## Notes
When using the `BackupServiceExtensions` class, note that all methods are asynchronous and should be awaited to ensure proper execution. Additionally, these methods do not throw exceptions, but instead return `null` or default values when no data is available. This class is designed to be thread-safe, allowing for concurrent access and execution of its methods. However, the underlying backup service implementation may impose its own thread-safety constraints, which should be considered when using these extension methods in a multi-threaded environment.
