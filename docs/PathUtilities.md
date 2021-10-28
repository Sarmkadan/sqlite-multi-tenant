# PathUtilities

The `PathUtilities` class provides a robust set of static utility methods for file system operations, path manipulation, and directory management within the `sqlite-multi-tenant` project. It aims to provide safe, predictable, and cross-platform compatible file system interaction by incorporating error handling and preventative checks for common issues such as directory traversal attacks and file access limitations.

## API

### `SafeCombinePath(string basePath, string relativePath)`
Combines a base path and a relative path, ensuring that the resulting path remains within the `basePath` to prevent directory traversal attacks (e.g., attempts to escape the base directory using `..`).
- **Parameters:** `basePath` (string), `relativePath` (string).
- **Returns:** A string representing the resolved, combined, and validated absolute path.
- **Throws:** `ArgumentException` if `basePath` is null/empty, or `InvalidOperationException` if directory traversal is detected.

### `SafeCreateDirectory(string path)`
Attempts to create the specified directory and all necessary parent directories.
- **Parameters:** `path` (string).
- **Returns:** `true` if the directory was created successfully or already existed; `false` otherwise.

### `GetDirectorySizeBytes(string path)`
Calculates the total size of a directory by recursively summing the sizes of all contained files.
- **Parameters:** `path` (string).
- **Returns:** The total size in bytes (`long`). Returns 0 if the directory does not exist or access is denied.

### `SafeDeleteDirectory(string path, int retryCount = 3)`
Deletes a directory and its contents recursively. Includes a retry mechanism to handle transient file lock issues.
- **Parameters:** `path` (string), `retryCount` (int, default=3).
- **Returns:** `true` if the directory was successfully deleted, `false` otherwise.

### `GetFilesRecursive(string path, string? searchPattern = null)`
Retrieves a list of all files within a directory and its subdirectories, optionally filtered by a search pattern.
- **Parameters:** `path` (string), `searchPattern` (string, optional).
- **Returns:** A `List<string>` containing the full paths of found files.

### `NormalizePath(string path)`
Converts path separators (`/` or `\`) in the given string to the platform-specific directory separator character (`Path.DirectorySeparatorChar`).
- **Parameters:** `path` (string).
- **Returns:** A string with normalized separators.

### `MakeRelativePath(string basePath, string fullPath)`
Converts an absolute path to a relative path based on a provided base directory.
- **Parameters:** `basePath` (string), `fullPath` (string).
- **Returns:** The relative path string, or `fullPath` if the conversion fails.

### `FormatBytes(long bytes)`
Converts a byte count into a human-readable string format with appropriate unit suffixes (B, KB, MB, GB, TB).
- **Parameters:** `bytes` (long).
- **Returns:** A formatted string (e.g., "1.50 MB").

### `IsDirectoryEmpty(string path)`
Checks whether a directory contains any files or subdirectories.
- **Parameters:** `path` (string).
- **Returns:** `true` if the directory is empty or does not exist; `false` if it contains entries.

### `CleanupOldFiles(string path, TimeSpan maxAge)`
Deletes files in a specified directory that have a `LastWriteTimeUtc` older than the specified `maxAge`.
- **Parameters:** `path` (string), `maxAge` (TimeSpan).
- **Returns:** The number of files deleted (`int`).

### `GetExtensionWithoutDot(string path)`
Extracts the file extension from a path and removes the leading dot character.
- **Parameters:** `path` (string).
- **Returns:** The extension as a string without the dot.

## Usage

```csharp
using SqliteMultiTenant.Utilities;

// 1. Safely combine paths and prevent traversal
string baseDir = "/app/data";
string userInput = "../../etc/passwd";
try
{
    string safePath = PathUtilities.SafeCombinePath(baseDir, userInput);
}
catch (InvalidOperationException)
{
    // Handle security violation
}

// 2. Format a directory size for display
long size = PathUtilities.GetDirectorySizeBytes("/app/data/uploads");
string displaySize = PathUtilities.FormatBytes(size);
Console.WriteLine($"Upload directory size: {displaySize}");
```

## Notes

- **Thread Safety:** The methods are generally thread-safe as they primarily perform read/write operations on the file system, relying on standard OS-level locking mechanisms. However, concurrent deletions or modifications to the same directory structure by external processes may cause operations to fail or return unexpected results.
- **Exceptions:** Many methods are designed to be resilient, catching `Exception` internally and returning safe defaults (like `false` or 0) rather than propagating errors to the caller. Callers should check return values appropriately.
- **Directory Traversal:** `SafeCombinePath` is the primary defense against path traversal attacks. Always use it when combining user-supplied paths with trusted base paths.
- **File Locks:** `SafeDeleteDirectory` incorporates retries specifically to handle transient `IOException` errors commonly caused by anti-virus scanners or background processes holding file locks.
