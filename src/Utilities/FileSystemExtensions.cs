#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace SqliteMultiTenant.Utilities;

/// <summary>
/// Extension methods for file system operations specific to database and backup handling.
/// Includes safe file creation, deletion, and path validation to prevent injection attacks.
/// All methods handle exceptions gracefully without throwing (prefer return codes).
/// </summary>
public static class FileSystemExtensions
{
    /// <summary>
    /// Safely checks if a file path is valid and doesn't attempt directory traversal.
    /// Prevents malicious paths like "../../sensitive" from being created.
    /// </summary>
    /// <param name="path">The path to validate.</param>
    /// <param name="allowedBasePath">The base directory that the path must be within.</param>
    /// <returns>True if the path is safe and within the allowed base directory; otherwise false.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="path"/> or <paramref name="allowedBasePath"/> is null.</exception>
    public static bool IsSafeFilePath(this string path, string allowedBasePath)
    {
        ArgumentNullException.ThrowIfNull(path);
        ArgumentNullException.ThrowIfNull(allowedBasePath);

        try
        {
            var fullPath = Path.GetFullPath(path);
            var basePath = Path.GetFullPath(allowedBasePath);

            // Ensure path is within allowed base directory
            return fullPath.StartsWith(basePath, StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Safely creates a directory if it doesn't exist.
    /// Returns true if created or already exists, false if error.
    /// </summary>
    /// <param name="path">The directory path to ensure exists.</param>
    /// <returns>True if the directory exists or was successfully created; otherwise false.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="path"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="path"/> is empty or whitespace.</exception>
    public static bool EnsureDirectoryExists(this string path)
    {
        ArgumentNullException.ThrowIfNull(path);
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        try
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Gets the size of a file in bytes, returns 0 if file doesn't exist.
    /// Handles concurrent file access without throwing exceptions.
    /// </summary>
    /// <param name="filePath">The path to the file to get the size of.</param>
    /// <returns>The size of the file in bytes, or 0 if the file doesn't exist or an error occurs.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="filePath"/> is null.</exception>
    public static long GetFileSizeBytes(this string filePath)
    {
        ArgumentNullException.ThrowIfNull(filePath);

        try
        {
            if (File.Exists(filePath))
            {
                var info = new FileInfo(filePath);
                return info.Length;
            }
        }
        catch
        {
            // File may be locked or deleted by another process
        }
        return 0;
    }

    /// <summary>
    /// Safely deletes a file with retry logic for locked files.
    /// Returns true if deleted, false if error or file doesn't exist.
    /// </summary>
    /// <param name="filePath">The path to the file to delete.</param>
    /// <param name="maxRetries">The maximum number of retry attempts for locked files.</param>
    /// <returns>True if the file was deleted or didn't exist; otherwise false.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="filePath"/> is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="maxRetries"/> is less than 0.</exception>
    public static bool SafeDelete(this string filePath, int maxRetries = 3)
    {
        ArgumentNullException.ThrowIfNull(filePath);
        ArgumentOutOfRangeException.ThrowIfNegative(maxRetries);

        try
        {
            if (!File.Exists(filePath))
                return true;

            for (int i = 0; i < maxRetries; i++)
            {
                try
                {
                    File.Delete(filePath);
                    return true;
                }
                catch (IOException) when (i < maxRetries - 1)
                {
                    // File is locked, wait and retry
                    System.Threading.Thread.Sleep(100 * (i + 1));
                }
            }
        }
        catch
        {
            // Silently fail - file may be in use by another process
        }
        return false;
    }

    /// <summary>
    /// Generates a unique file name to prevent collisions during backup operations.
    /// Format: backup_{tenantId}_{timestamp}_{random}.db
    /// </summary>
    /// <param name="tenantId">The tenant identifier to include in the filename.</param>
    /// <returns>A formatted backup filename with timestamp and random suffix.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="tenantId"/> is null.</exception>
    public static string GenerateBackupFileName(this string tenantId)
    {
        ArgumentNullException.ThrowIfNull(tenantId);

        var timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
        var random = Guid.NewGuid().ToString()[..8];
        return $"backup_{tenantId}_{timestamp}_{random}.db";
    }

    /// <summary>
    /// Gets all files in a directory with a specific extension (recursive).
    /// Returns empty list if directory doesn't exist or error occurs.
    /// </summary>
    /// <param name="directoryPath">The directory path to search for files.</param>
    /// <param name="extension">The file extension to match (e.g., ".db", ".bak").</param>
    /// <returns>A list of file paths matching the specified extension; empty if no matches or error.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="directoryPath"/> or <paramref name="extension"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="extension"/> is empty or whitespace.</exception>
    public static List<string> GetFilesWithExtension(this string directoryPath, string extension)
    {
        ArgumentNullException.ThrowIfNull(directoryPath);
        ArgumentNullException.ThrowIfNull(extension);
        ArgumentException.ThrowIfNullOrWhiteSpace(extension);

        var files = new List<string>();
        try
        {
            if (Directory.Exists(directoryPath))
            {
                files = Directory.GetFiles(directoryPath, $"*{extension}", SearchOption.AllDirectories)
                    .ToList();
            }
        }
        catch
        {
            // Directory may have been deleted or access denied
        }
        return files;
    }

    /// <summary>
    /// Calculates total size of all files in a directory (recursive).
    /// Useful for monitoring disk usage and enforcing storage quotas.
    /// </summary>
    /// <param name="directoryPath">The directory path to calculate size for.</param>
    /// <returns>The total size in bytes of all files in the directory and subdirectories; 0 if directory doesn't exist or error occurs.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="directoryPath"/> is null.</exception>
    public static long GetDirectorySizeBytes(this string directoryPath)
    {
        ArgumentNullException.ThrowIfNull(directoryPath);

        long totalSize = 0;
        try
        {
            if (!Directory.Exists(directoryPath))
                return 0;

            var dirInfo = new DirectoryInfo(directoryPath);
            var files = dirInfo.GetFiles("*", SearchOption.AllDirectories);

            foreach (var file in files)
            {
                try
                {
                    totalSize += file.Length;
                }
                catch
                {
                    // File may be locked or deleted
                }
            }
        }
        catch
        {
            // Directory may be inaccessible
        }
        return totalSize;
    }

    /// <summary>
    /// Copies a file with error handling and progress reporting capability.
    /// Returns true if successful, false otherwise.
    /// </summary>
    /// <param name="sourcePath">The source file path to copy from.</param>
    /// <param name="destPath">The destination file path to copy to.</param>
    /// <param name="overwrite">Whether to overwrite the destination file if it exists.</param>
    /// <returns>True if the file was successfully copied; otherwise false.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="sourcePath"/> or <paramref name="destPath"/> is null.</exception>
    public static bool SafeCopyFile(this string sourcePath, string destPath, bool overwrite = false)
    {
        ArgumentNullException.ThrowIfNull(sourcePath);
        ArgumentNullException.ThrowIfNull(destPath);

        try
        {
            if (!File.Exists(sourcePath))
                return false;

            File.Copy(sourcePath, destPath, overwrite);
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Gets creation time of file, returns epoch if file doesn't exist.
    /// Always returns UTC to maintain consistency.
    /// </summary>
    /// <param name="filePath">The path to the file to get creation time for.</param>
    /// <returns>The file creation time in UTC; DateTime.UnixEpoch if file doesn't exist or error occurs.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="filePath"/> is null.</exception>
    public static DateTime GetFileCreationTimeUtc(this string filePath)
    {
        ArgumentNullException.ThrowIfNull(filePath);

        try
        {
            if (File.Exists(filePath))
            {
                return File.GetCreationTimeUtc(filePath);
            }
        }
        catch
        {
            // File may be inaccessible
        }
        return DateTime.UnixEpoch;
    }
}
