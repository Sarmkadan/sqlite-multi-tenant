// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace SqliteMultiTenant.Utilities;

/// <summary>
/// Advanced path and directory manipulation utilities.
/// Provides safe path operations, directory traversal, and file system utilities.
/// </summary>
public static class PathUtilities
{
    /// <summary>
    /// Safely combines paths and resolves any relative path traversal attempts.
    /// Prevents directory traversal attacks using ".." sequences.
    /// </summary>
    public static string SafeCombinePath(string basePath, string relativePath)
    {
        if (string.IsNullOrEmpty(basePath))
            throw new ArgumentException("Base path cannot be null or empty");

        if (string.IsNullOrEmpty(relativePath))
            return basePath;

        var fullPath = Path.Combine(basePath, relativePath);
        var resolvedPath = Path.GetFullPath(fullPath);
        var baseResolved = Path.GetFullPath(basePath);

        // Ensure the resolved path is within the base path (prevent directory traversal)
        if (!resolvedPath.StartsWith(baseResolved, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Path traversal detected - access denied");

        return resolvedPath;
    }

    /// <summary>
    /// Safely creates a directory and all parent directories.
    /// Returns true if created successfully or already exists.
    /// </summary>
    public static bool SafeCreateDirectory(string path)
    {
        try
        {
            if (Directory.Exists(path))
                return true;

            Directory.CreateDirectory(path);
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }

    /// <summary>
    /// Gets the size of a directory including all files recursively.
    /// Returns size in bytes.
    /// </summary>
    public static long GetDirectorySizeBytes(string path)
    {
        try
        {
            if (!Directory.Exists(path))
                return 0;

            var directoryInfo = new DirectoryInfo(path);
            return GetDirectorySizeRecursive(directoryInfo);
        }
        catch (Exception)
        {
            return 0;
        }
    }

    private static long GetDirectorySizeRecursive(DirectoryInfo directory)
    {
        long totalSize = 0;

        try
        {
            // Add files in current directory
            foreach (var file in directory.GetFiles())
            {
                totalSize += file.Length;
            }

            // Recursively add subdirectories
            foreach (var subDir in directory.GetDirectories())
            {
                totalSize += GetDirectorySizeRecursive(subDir);
            }
        }
        catch (UnauthorizedAccessException)
        {
            // Skip directories we don't have access to
        }

        return totalSize;
    }

    /// <summary>
    /// Deletes a directory and all its contents.
    /// Retries on access denied errors for locked files.
    /// </summary>
    public static bool SafeDeleteDirectory(string path, int retryCount = 3)
    {
        try
        {
            if (!Directory.Exists(path))
                return true;

            int attempts = 0;
            while (attempts < retryCount)
            {
                try
                {
                    Directory.Delete(path, recursive: true);
                    return true;
                }
                catch (IOException) when (attempts < retryCount - 1)
                {
                    attempts++;
                    System.Threading.Thread.Sleep(100 * attempts);
                }
            }

            return false;
        }
        catch (Exception)
        {
            return false;
        }
    }

    /// <summary>
    /// Gets all files recursively with optional filter pattern.
    /// </summary>
    public static List<string> GetFilesRecursive(string path, string? searchPattern = null)
    {
        var files = new List<string>();

        try
        {
            if (!Directory.Exists(path))
                return files;

            var directory = new DirectoryInfo(path);
            return GetFilesRecursiveInternal(directory, searchPattern);
        }
        catch (Exception)
        {
            return files;
        }
    }

    private static List<string> GetFilesRecursiveInternal(DirectoryInfo directory, string? searchPattern)
    {
        var files = new List<string>();

        try
        {
            // Add files in current directory
            var currentFiles = searchPattern != null
                ? directory.GetFiles(searchPattern)
                : directory.GetFiles();

            files.AddRange(currentFiles.Select(f => f.FullPath));

            // Recursively add from subdirectories
            foreach (var subDir in directory.GetDirectories())
            {
                files.AddRange(GetFilesRecursiveInternal(subDir, searchPattern));
            }
        }
        catch (UnauthorizedAccessException)
        {
            // Skip directories we don't have access to
        }

        return files;
    }

    /// <summary>
    /// Normalizes path separators to current OS convention.
    /// </summary>
    public static string NormalizePath(string path)
    {
        if (string.IsNullOrEmpty(path))
            return path;

        return path.Replace('/', Path.DirectorySeparatorChar)
                   .Replace('\\', Path.DirectorySeparatorChar);
    }

    /// <summary>
    /// Converts absolute path to relative path from basePath.
    /// </summary>
    public static string MakeRelativePath(string basePath, string fullPath)
    {
        try
        {
            var baseUri = new Uri(Path.GetFullPath(basePath) + Path.DirectorySeparatorChar);
            var fullUri = new Uri(Path.GetFullPath(fullPath));

            var relative = baseUri.MakeRelativeUri(fullUri);
            return Uri.UnescapeDataString(relative.ToString())
                .Replace('/', Path.DirectorySeparatorChar);
        }
        catch (Exception)
        {
            return fullPath;
        }
    }

    /// <summary>
    /// Gets the formatted size string (KB, MB, GB, etc.).
    /// </summary>
    public static string FormatBytes(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB", "TB" };
        double len = bytes;
        int order = 0;

        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len = len / 1024;
        }

        return $"{len:0.##} {sizes[order]}";
    }

    /// <summary>
    /// Checks if directory is empty.
    /// </summary>
    public static bool IsDirectoryEmpty(string path)
    {
        try
        {
            if (!Directory.Exists(path))
                return true;

            return !Directory.EnumerateFileSystemEntries(path).Any();
        }
        catch (Exception)
        {
            return true;
        }
    }

    /// <summary>
    /// Cleans up old files in a directory based on age.
    /// </summary>
    public static int CleanupOldFiles(string path, TimeSpan maxAge)
    {
        int deletedCount = 0;

        try
        {
            if (!Directory.Exists(path))
                return 0;

            var cutoffTime = DateTime.UtcNow - maxAge;
            var files = Directory.GetFiles(path);

            foreach (var file in files)
            {
                var fileInfo = new FileInfo(file);
                if (fileInfo.LastWriteTimeUtc < cutoffTime)
                {
                    File.Delete(file);
                    deletedCount++;
                }
            }
        }
        catch (Exception)
        {
            // Silently fail
        }

        return deletedCount;
    }

    /// <summary>
    /// Gets the file extension without the dot.
    /// </summary>
    public static string GetExtensionWithoutDot(string path)
    {
        var extension = Path.GetExtension(path);
        return extension.TrimStart('.');
    }
}
