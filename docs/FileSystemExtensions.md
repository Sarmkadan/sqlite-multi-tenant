# FileSystemExtensions

The `FileSystemExtensions` static class provides a collection of utility methods for common file and directory operations used throughout the `sqlite-multi-tenant` library. These methods encapsulate error handling, path validation, and cross‑platform file‑system interactions, reducing boilerplate code when working with tenant‑specific storage locations, backup files, and size calculations.

## API

### `IsSafeFilePath`
```csharp
public static bool IsSafeFilePath(string path)
```
**Purpose:** Validates that the given path does not contain characters or patterns that could lead to directory traversal or other unsafe file operations.  
**Parameters:**  
- `path` – The file path to validate.  
**Returns:** `true` if the path is considered safe; otherwise `false`.  
**Throws:** `ArgumentNullException` if `path` is `null`.

### `EnsureDirectoryExists`
```csharp
public static bool EnsureDirectoryExists(string directoryPath)
```
**Purpose:** Creates the specified directory and all missing parent directories if they do not already exist.  
**Parameters:**  
- `directoryPath` – The full path of the directory to ensure.  
**Returns:** `true` if the directory was created or already existed; `false` if creation failed (e.g., due to permissions).  
**Throws:** `ArgumentNullException` if `directoryPath` is `null`.

### `GetFileSizeBytes`
```csharp
public static long GetFileSizeBytes(string filePath)
```
**Purpose:** Returns the size of the file at the given path in bytes.  
**Parameters:**  
- `filePath` – The path to the file.  
**Returns:** The file size in bytes.  
**Throws:**  
- `ArgumentNullException` if `filePath` is `null`.  
- `FileNotFoundException` if the file does not exist.  
- `UnauthorizedAccessException` if the caller lacks read permission.

### `SafeDelete`
```csharp
public static bool SafeDelete(string filePath)
```
**Purpose:** Attempts to delete the specified file without throwing an exception if the file does not exist or cannot be deleted.  
**Parameters:**  
- `filePath` – The path to the file to delete.  
**Returns:** `true` if the file was successfully deleted; `false` if the file did not exist or deletion failed.  
**Throws:** `ArgumentNullException` if `filePath` is `null`.

### `GenerateBackupFileName`
```csharp
public static string GenerateBackupFileName(string originalFilePath)
```
**Purpose:** Creates a backup file name by appending a timestamp to the original file name, preserving the extension.  
**Parameters:**  
- `originalFilePath` – The full path of the original file.  
**Returns:** A string representing the backup file path (same directory, new name).  
**Throws:** `ArgumentNullException` if `originalFilePath` is `null`.

### `GetFilesWithExtension`
```csharp
public static List<string> GetFilesWithExtension(string directoryPath, string extension)
```
**Purpose:** Retrieves all file paths in the given directory that have the specified extension.  
**Parameters:**  
- `directoryPath` – The directory to search.  
- `extension` – The file extension to match (e.g., `".db"`).  
**Returns:** A `List<string>` of matching file paths. Returns an empty list if the directory does not exist or contains no matching files.  
**Throws:**  
- `ArgumentNullException` if either parameter is `null`.  
- `DirectoryNotFoundException` if the directory path is invalid (but not if it simply does not exist – see Notes).

### `GetDirectorySizeBytes`
```csharp
public static long GetDirectorySizeBytes(string directoryPath)
```
**Purpose:** Calculates the total size of all files in the specified directory, including subdirectories.  
**Parameters:**  
- `directoryPath` – The path to the directory.  
**Returns:** The total size in bytes.  
**Throws:**  
- `ArgumentNullException` if `directoryPath` is `null`.  
- `DirectoryNotFoundException` if the directory does not exist.

### `SafeCopyFile`
```csharp
public static bool SafeCopyFile(string sourceFilePath, string destinationFilePath, bool overwrite = false)
```
**Purpose:** Copies a file from the source to the destination, optionally overwriting an existing file. Returns `false` instead of throwing on common recoverable errors.  
**Parameters:**  
- `sourceFilePath` – The path of the file to copy.  
- `destinationFilePath` – The target path.  
- `overwrite` – If `true`, overwrites an existing destination file; otherwise the copy fails if the destination exists.  
**Returns:** `true` if the copy succeeded; `false` if the source file does not exist, the destination exists and `overwrite` is `false`, or an I/O error occurs.  
**Throws:** `ArgumentNullException` if either path is `null`.

### `GetFileCreationTimeUtc`
```csharp
public static DateTime GetFileCreationTimeUtc(string filePath)
```
**Purpose:** Retrieves the creation time of the specified file in Coordinated Universal Time (UTC).  
**Parameters:**  
- `filePath` – The path to the file.  
**Returns:** A `DateTime` in UTC representing the file creation time.  
**Throws:**  
- `ArgumentNullException` if `filePath` is `null`.  
- `FileNotFoundException` if the file does not exist.  
- `UnauthorizedAccessException` if the caller lacks read permission.

## Usage

### Example 1: Backup and size calculation for a tenant database
```csharp
string tenantDbPath = "/data/tenants/tenant42/main.db";

// Ensure the backup directory exists
string backupDir = "/data/backups";
FileSystemExtensions.EnsureDirectoryExists(backupDir);

// Generate a backup file name with timestamp
string backupPath = FileSystemExtensions.GenerateBackupFileName(tenantDbPath);

// Copy the database file (do not overwrite if backup already exists)
if (FileSystemExtensions.SafeCopyFile(tenantDbPath, backupPath))
{
    long dbSize = FileSystemExtensions.GetFileSizeBytes(tenantDbPath);
    Console.WriteLine($"Backup created: {backupPath} ({dbSize} bytes)");
}
else
{
    Console.WriteLine("Backup copy failed (file may already exist or source missing).");
}
```

### Example 2: Cleanup old log files and report directory size
```csharp
string logDir = "/var/log/tenants";

// Delete all .log files older than 7 days
var logFiles = FileSystemExtensions.GetFilesWithExtension(logDir, ".log");
DateTime cutoff = DateTime.UtcNow.AddDays(-7);

foreach (string logFile in logFiles)
{
    DateTime creationTime = FileSystemExtensions.GetFileCreationTimeUtc(logFile);
    if (creationTime < cutoff)
    {
        if (FileSystemExtensions.SafeDelete(logFile))
        {
            Console.WriteLine($"Deleted old log: {logFile}");
        }
    }
}

// Report total size of remaining files
long totalBytes = FileSystemExtensions.GetDirectorySizeBytes(logDir);
Console.WriteLine($"Current log directory size: {totalBytes} bytes");
```

## Notes

- **Path validation:** `IsSafeFilePath` is used internally by several methods to guard against directory traversal attacks. It should be called explicitly when accepting user‑supplied paths.
- **Non‑existent directories:** `GetFilesWithExtension` returns an empty list if the directory does not exist, while `GetDirectorySizeBytes` throws `DirectoryNotFoundException`. This distinction allows callers to handle missing directories differently depending on the operation.
- **Thread safety:** None of the methods are thread‑safe by themselves. Concurrent calls to `SafeDelete`, `SafeCopyFile`, or `EnsureDirectoryExists` on the same file or directory may produce race conditions. Callers should synchronize access when operating on shared file‑system resources.
- **Error handling:** Methods that return `bool` (e.g., `SafeDelete`, `SafeCopyFile`) suppress common I/O exceptions and indicate failure via the return value. Methods that return a value (e.g., `GetFileSizeBytes`) throw exceptions for missing files or permission issues. Always check the return value of boolean methods before proceeding.
- **Cross‑platform compatibility:** Paths are treated as strings; no platform‑specific path normalization is performed. On Windows, ensure paths use backslashes or forward slashes consistently.
