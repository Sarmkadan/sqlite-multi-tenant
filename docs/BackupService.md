# BackupService

The `BackupService` class provides asynchronous operations for managing SQLite database backups in a multi‑tenant environment. It implements `IBackupService` and is responsible for creating, retrieving, updating, and deleting backup records, as well as tracking their completion status, expiration, and tags.

## API

### BackupService()
Initializes a new instance of the `BackupService`. The constructor does not take any parameters and prepares the service for use.

### GetBackupAsync()
**Purpose:** Retrieves a single backup record.  
**Parameters:** None.  
**Return Value:** `Task<Backup?>` – the backup if found, otherwise `null`.  
**Throws:** May throw `OperationCanceledException` if the caller cancels the operation, or `IOException` if an underlying storage error occurs.

### GetDatabaseBackupsAsync()
**Purpose:** Returns all backups associated with a specific database.  
**Parameters:** None.  
**Return Value:** `Task<List<Backup>>` – a list of backup objects; the list may be empty if no backups exist.  
**Throws:** May throw `OperationCanceledException` on cancellation, or `IOException` for storage access problems.

### GetCompletedBackupsAsync()
**Purpose:** Returns backups that have been marked as completed.  
**Parameters:** None.  
**Return Value:** `Task<List<Backup>>` – a list of completed backups; empty list if none are completed.  
**Throws:** May throw `OperationCanceledException` or `IOException` under the same conditions as other async methods.

### GetLatestBackupAsync()
**Purpose:** Retrieves the most recent backup record.  
**Parameters:** None.  
**Return Value:** `Task<Backup?>` – the latest backup, or `null` if no backups exist.  
**Throws:** May throw `OperationCanceledException` or `IOException`.

### CreateBackupAsync()
**Purpose:** Initiates the creation of a new backup.  
**Parameters:** None.  
**Return Value:** `Task<Backup>` – the newly created backup object representing the in‑progress operation.  
**Throws:** May throw `InvalidOperationException` if a backup cannot be started (e.g., another backup is already in progress), `OperationCanceledException`, or `IOException`.

### MarkBackupAsCompletedAsync()
**Purpose:** Marks a backup as successfully completed.  
**Parameters:** None.  
**Return Value:** `Task`.  
**Throws:** May throw `ArgumentException` if the backup identifier is invalid, `OperationCanceledException`, or `IOException`.

### MarkBackupAsFailedAsync()
**Purpose:** Marks a backup as having failed.  
**Parameters:** None.  
**Return Value:** `Task`.  
**Throws:** May throw `ArgumentException` for an invalid identifier, `OperationCanceledException`, or `IOException`.

### VerifyBackupAsync()
**Purpose:** Verifies the integrity of a backup.  
**Parameters:** None.  
**Return Value:** `Task`.  
**Throws:** May throw `OperationCanceledException`, `IOException`, or `BackupVerificationException` (domain‑specific) if verification fails.

### SetBackupExpirationAsync()
**Purpose:** Sets an expiration date/time for a backup.  
**Parameters:** None.  
**Return Value:** `Task`.  
**Throws:** May throw `ArgumentOutOfRangeException` if the expiration is in the past, `OperationCanceledException`, or `IOException`.

### GetExpiredBackupsAsync()
**Purpose:** Retrieves all backups that have passed their expiration date.  
**Parameters:** None.  
**Return Value:** `Task<List<Backup>>` – list of expired backups; may be empty.  
**Throws:** May throw `OperationCanceledException` or `IOException`.

### GetBackupCountAsync()
**Purpose:** Gets the total number of backup records stored.  
**Parameters:** None.  
**Return Value:** `Task<int>` – the count of backups.  
**Throws:** May throw `OperationCanceledException` or `IOException`.

### DeleteBackupAsync()
**Purpose:** Removes a backup record and its associated file(s).  
**Parameters:** None.  
**Return Value:** `Task`.  
**Throws:** May throw `ArgumentException` if the backup does not exist, `OperationCanceledException`, or `IOException`.

### AddBackupTagAsync()
**Purpose:** Associates a tag with a backup for categorization or filtering.  
**Parameters:** None.  
**Return Value:** `Task`.  
**Throws:** May throw `ArgumentException` for an invalid tag, `OperationCanceledException`, or `IOException`.

### BackupWithProgressAsync()
**Purpose:** Creates a backup while reporting progress via an `IProgress<T>` mechanism (implementation‑specific).  
**Parameters:** None.  
**Return Value:** `Task`.  
**Throws:** May throw `OperationCanceledException`, `IOException`, or `ProgressReportingException` if progress reporting fails.

## Usage

```csharp
// Example 1: Create a backup and wait for it to complete.
var backupService = new BackupService();
Backup backup = await backupService.CreateBackupAsync();

// Simulate work or await completion signal from elsewhere.
await backupService.MarkBackupAsCompletedAsync();

Console.WriteLine($"Backup {backup.Id} completed.");
```

```csharp
// Example 2: Clean up expired backups.
var backupService = new BackupService();
List<Backup> expired = await backupService.GetExpiredBackupsAsync();

foreach (var b in expired)
{
    await backupService.DeleteBackupAsync();
    Console.WriteLine($"Deleted expired backup {b.Id}.");
}
```

## Notes

- All methods are asynchronous and should be `awaited`; calling them without awaiting may lead to unobserved exceptions.
- The class is `sealed`, preventing inheritance; thread‑safety depends on the underlying storage implementation used by the service. If the storage layer is thread‑safe, concurrent calls to different methods are safe; however, invoking `CreateBackupAsync` while another backup is already in progress may result in an `InvalidOperationException`.
- Methods that return a nullable `Backup?` (e.g., `GetBackupAsync`, `GetLatestBackupAsync`) return `null` when no matching backup exists rather than throwing an exception.
- Passing invalid identifiers or arguments (where applicable) will result in `ArgumentException` or derived exceptions.
- Cancellation tokens are not shown in the signatures; if the implementation supports them, passing a canceled token will cause an `OperationCanceledException`.
- The service does not automatically purge expired backups; callers must invoke `GetExpiredBackupsAsync` followed by `DeleteBackupAsync` to reclaim space.
