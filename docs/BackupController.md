# BackupController

The `BackupController` provides asynchronous operations for managing backups in a multi‑tenant SQLite environment. It encapsulates the logic for creating, retrieving, verifying, restoring, and tagging backups, returning results wrapped in an `ApiResponse` to convey success or failure information.

## API

### BackupController()
Initializes a new instance of the `BackupController`. The constructor does not take any parameters and prepares the controller for use. It does not throw exceptions under normal circumstances.

### CreateBackupAsync
**Purpose:** Initiates the creation of a new backup for a specified tenant.  
**Parameters:** The method accepts the necessary backup specification (tenant identifier, backup options, etc.) as defined by the service contract.  
**Return Value:** `Task<ApiResponse<BackupResponse>>`. On success, the `ApiResponse` contains a `BackupResponse` with details such as backup ID, timestamp, and status.  
**When it throws:** May throw `ArgumentNullException` if required arguments are null, `InvalidOperationException` if the backup cannot be started due to service state, or any IOException‑derived exception if underlying storage access fails. Operation‑cancellation tokens, if supplied, can cause an `OperationCanceledException`.

### GetBackupAsync
**Purpose:** Retrieves the details of an existing backup.  
**Parameters:** Accepts the backup identifier (and optionally tenant context) required to locate the backup.  
**Return Value:** `Task<ApiResponse<BackupResponse>>`. Successful responses include a `BackupResponse` populated with the backup’s metadata.  
**When it throws:** Throws `ArgumentNullException` for null identifiers, `KeyNotFoundException` (or equivalent) if the backup does not exist, and storage‑related exceptions for read failures.

### ListBackupsAsync
**Purpose:** Enumerates backups available for a tenant or across the system, depending on the supplied parameters.  
**Parameters:** Takes filtering criteria such as tenant ID, date ranges, or status flags.  
**Return Value:** `Task<ApiResponse<IEnumerable<BackupResponse>>>`. The response contains a collection of `BackupResponse` objects representing the matching backups.  
**When it throws:** May throw `ArgumentNullException` for null filter arguments, and any exception arising from query execution against the backup store.

### VerifyBackupAsync
**Purpose:** Checks the integrity and validity of a backup.  
**Parameters:** Requires the backup identifier (and tenant context) to verify.  
**Return Value:** `Task<ApiResponse<object>>`. A successful response indicates the backup passed verification; the payload is service‑specific and treated as opaque.  
**When it throws:** Throws `ArgumentNullException` for missing identifiers, `InvalidOperationException` if the backup is not in a verifiable state, and storage‑related exceptions for read errors during verification.

### RestoreBackupAsync
**Purpose:** Restores a database from a previously created backup.  
**Parameters:** Accepts the backup identifier to restore, along with any options controlling the restore process (e.g., target tenant, overwrite behavior).  
**Return Value:** `Task<ApiResponse<object>>`. On success, the response confirms the restore operation completed.  
**When it throws:** Throws `ArgumentNullException` for null identifiers, `InvalidOperationException` if the backup cannot be restored (e.g., missing or corrupted), and IOException‑derived exceptions for failures during file copy or database replacement.

### TagBackupAsync
**Purpose:** Applies or updates metadata tags on a backup for categorization or retention purposes.  
**Parameters:** Takes the backup identifier and a collection of key‑value pairs representing the tags to set.  
**Return Value:** `Task<ApiResponse<object>>`. A successful response indicates the tags were stored.  
**When it throws:** Throws `ArgumentNullException` for null identifier or tag collection, and any exception resulting from failure to persist the tag metadata.

## Usage

```csharp
// Create a backup controller instance
var backupController = new BackupController();

// Request a new backup for tenant "acme"
var createResponse = await backupController.CreateBackupAsync(
    new { TenantId = "acme", Options = new { RetentionDays = 30 } });

if (createResponse.IsSuccess)
{
    Console.WriteLine($"Backup created: {createResponse.Data.BackupId}");
}
else
{
    Console.Error.WriteLine($"Backup creation failed: {createResponse.ErrorMessage}");
}
```

```csharp
// List all backups for tenant "acme" from the last 7 days
var listResponse = await backupController.ListBackupsAsync(
    new { TenantId = "acme", Since = DateTime.UtcNow.AddDays(-7) });

if (listResponse.IsSuccess)
{
    foreach (var backup in listResponse.Data)
    {
        Console.WriteLine($"{backup.BackupId} - {backup.Timestamp}");
    }
}
else
{
    Console.Error.WriteLine($"Failed to list backups: {listResponse.ErrorMessage}");
}
```

## Notes
- All methods are instance methods; the controller holds no mutable state after construction, making it safe for concurrent calls from multiple threads provided that the underlying storage service used by the controller is thread‑safe.
- Passing `null` for any required argument will result in an `ArgumentNullException`.
- The `ApiResponse<T>` wrapper should be inspected for `IsSuccess` before accessing `Data`; failure responses contain an explanatory message in `ErrorMessage` and may include an inner exception.
- Backup operations may be long‑running; callers should consider supplying a cancellation token if the API overload supports it, otherwise the operation will run to completion or throw on failure.
- Tagging a backup does not affect the backup data itself; it only updates associated metadata, which may be used by retention policies or UI filtering.
- Restoring a backup will replace the target database file; ensure no active connections are using the database to avoid corruption or access‑denied exceptions.
