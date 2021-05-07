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
    public static bool IsSafeFilePath(this string path, string allowedBasePath)
    {
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
    public static bool EnsureDirectoryExists(this string path)
    {
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
    public static long GetFileSizeBytes(this string filePath)
    {
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
    public static bool SafeDelete(this string filePath, int maxRetries = 3)
    {
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
    public static string GenerateBackupFileName(this string tenantId)
    {
        var timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
        var random = Guid.NewGuid().ToString()[..8];
        return $"backup_{tenantId}_{timestamp}_{random}.db";
    }

    /// <summary>
    /// Gets all files in a directory with a specific extension (recursive).
    /// Returns empty list if directory doesn't exist or error occurs.
    /// </summary>
    public static List<string> GetFilesWithExtension(this string directoryPath, string extension)
    {
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
    public static long GetDirectorySizeBytes(this string directoryPath)
    {
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
    public static bool SafeCopyFile(this string sourcePath, string destPath, bool overwrite = false)
    {
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
    public static DateTime GetFileCreationTimeUtc(this string filePath)
    {
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
