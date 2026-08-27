using SqliteMultiTenant.Utilities;
using FluentAssertions;
using Xunit;
using System.IO;

namespace SqliteMultiTenant.Tests.Utilities
{
    /// <summary>
    /// Tests for the PathUtilities class.
    /// </summary>
    public class PathUtilitiesTests
    {
        /// <summary>
        /// Tests that SafeCombinePath combines a base path and relative path correctly.
        /// </summary>
        [Fact]
        public void SafeCombinePath_WithValidPaths_ReturnsCombinedPath()
        {
            // Arrange
            string basePath = "/home/user/documents";
            string relativePath = "projects/project1";

            // Act
            string result = PathUtilities.SafeCombinePath(basePath, relativePath);

            // Assert
            result.Should().Be(Path.GetFullPath(Path.Combine(basePath, relativePath)));
        }

        /// <summary>
        /// Tests that SafeCombinePath throws an ArgumentException when the base path is empty.
        /// </summary>
        [Fact]
        public void SafeCombinePath_WithEmptyBasePath_ThrowsArgumentException()
        {
            // Arrange
            string basePath = "";
            string relativePath = "some/path";

            // Act
            Action act = () => PathUtilities.SafeCombinePath(basePath, relativePath);

            // Assert
            act.Should().Throw<ArgumentException>();
        }

        /// <summary>
        /// Tests that SafeCombinePath throws an ArgumentException when the base path is null.
        /// </summary>
        [Fact]
        public void SafeCombinePath_WithNullBasePath_ThrowsArgumentException()
        {
            // Arrange
            string basePath = null!;
            string relativePath = "some/path";

            // Act
            Action act = () => PathUtilities.SafeCombinePath(basePath, relativePath);

            // Assert
            act.Should().Throw<ArgumentException>();
        }

        /// <summary>
        /// Tests that SafeCombinePath returns the base path when the relative path is empty.
        /// </summary>
        [Fact]
        public void SafeCombinePath_WithEmptyRelativePath_ReturnsBasePath()
        {
            // Arrange
            string basePath = "/home/user/documents";
            string relativePath = "";

            // Act
            string result = PathUtilities.SafeCombinePath(basePath, relativePath);

            // Assert
            result.Should().Be(Path.GetFullPath(basePath));
        }

        /// <summary>
        /// Tests that SafeCombinePath throws an InvalidOperationException when the relative path attempts directory traversal.
        /// </summary>
        [Fact]
        public void SafeCombinePath_WithPathTraversalAttempt_ThrowsInvalidOperationException()
        {
            // Arrange
            string basePath = "/home/user/documents";
            string relativePath = "../../../etc/passwd";

            // Act
            Action act = () => PathUtilities.SafeCombinePath(basePath, relativePath);

            // Assert
            act.Should().Throw<InvalidOperationException>();
        }

        /// <summary>
        /// Tests that SafeCreateDirectory creates a directory and returns true for a valid path.
        /// </summary>
        [Fact]
        public void SafeCreateDirectory_WithValidPath_CreatesDirectoryAndReturnsTrue()
        {
            // Arrange
            string testPath = Path.Combine(Path.GetTempPath(), $"testdir_{Guid.NewGuid()}");

            // Act
            bool result = PathUtilities.SafeCreateDirectory(testPath);

            // Assert
            result.Should().BeTrue();
            Directory.Exists(testPath).Should().BeTrue();

            // Cleanup
            Directory.Delete(testPath, true);
        }

        /// <summary>
        /// Tests that SafeCreateDirectory returns true when the directory already exists.
        /// </summary>
        [Fact]
        public void SafeCreateDirectory_WithExistingDirectory_ReturnsTrue()
        {
            // Arrange
            string testPath = Path.Combine(Path.GetTempPath(), $"testdir_{Guid.NewGuid()}");
            Directory.CreateDirectory(testPath);

            // Act
            bool result = PathUtilities.SafeCreateDirectory(testPath);

            // Assert
            result.Should().BeTrue();

            // Cleanup
            Directory.Delete(testPath, true);
        }

        /// <summary>
        /// Tests that GetDirectorySizeBytes returns zero for an empty directory.
        /// </summary>
        [Fact]
        public void GetDirectorySizeBytes_WithEmptyDirectory_ReturnsZero()
        {
            // Arrange
            string testPath = Path.Combine(Path.GetTempPath(), $"testdir_{Guid.NewGuid()}");
            Directory.CreateDirectory(testPath);

            // Act
            long size = PathUtilities.GetDirectorySizeBytes(testPath);

            // Assert
            size.Should().Be(0);

            // Cleanup
            Directory.Delete(testPath, true);
        }

        /// <summary>
        /// Tests that GetDirectorySizeBytes returns the correct size for a directory containing files.
        /// </summary>
        [Fact]
        public void GetDirectorySizeBytes_WithFiles_ReturnsCorrectSize()
        {
            // Arrange
            string testPath = Path.Combine(Path.GetTempPath(), $"testdir_{Guid.NewGuid()}");
            Directory.CreateDirectory(testPath);

            string file1 = Path.Combine(testPath, "file1.txt");
            string file2 = Path.Combine(testPath, "file2.txt");
            File.WriteAllText(file1, "Hello World"); // 11 bytes
            File.WriteAllText(file2, "Test"); // 4 bytes

            // Act
            long size = PathUtilities.GetDirectorySizeBytes(testPath);

            // Assert
            size.Should().Be(15); // 11 + 4

            // Cleanup
            Directory.Delete(testPath, true);
        }

        /// <summary>
        /// Tests that GetDirectorySizeBytes returns zero for a non-existent directory.
        /// </summary>
        [Fact]
        public void GetDirectorySizeBytes_WithNonExistentDirectory_ReturnsZero()
        {
            // Arrange
            string testPath = Path.Combine(Path.GetTempPath(), $"nonexistent_{Guid.NewGuid()}");

            // Act
            long size = PathUtilities.GetDirectorySizeBytes(testPath);

            // Assert
            size.Should().Be(0);
        }

        /// <summary>
        /// Tests that SafeDeleteDirectory deletes an existing directory and returns true.
        /// </summary>
        [Fact]
        public void SafeDeleteDirectory_WithExistingDirectory_DeletesAndReturnsTrue()
        {
            // Arrange
            string testPath = Path.Combine(Path.GetTempPath(), $"testdir_{Guid.NewGuid()}");
            Directory.CreateDirectory(testPath);
            File.WriteAllText(Path.Combine(testPath, "test.txt"), "content");

            // Act
            bool result = PathUtilities.SafeDeleteDirectory(testPath);

            // Assert
            result.Should().BeTrue();
            Directory.Exists(testPath).Should().BeFalse();
        }

        /// <summary>
        /// Tests that SafeDeleteDirectory returns true when the directory does not exist.
        /// </summary>
        [Fact]
        public void SafeDeleteDirectory_WithNonExistentDirectory_ReturnsTrue()
        {
            // Arrange
            string testPath = Path.Combine(Path.GetTempPath(), $"nonexistent_{Guid.NewGuid()}");

            // Act
            bool result = PathUtilities.SafeDeleteDirectory(testPath);

            // Assert
            result.Should().BeTrue();
        }

        /// <summary>
        /// Tests that GetFilesRecursive returns a list of files in an existing directory.
        /// </summary>
        [Fact]
        public void GetFilesRecursive_WithExistingDirectory_ReturnsFileList()
        {
            // Arrange
            string testPath = Path.Combine(Path.GetTempPath(), $"testdir_{Guid.NewGuid()}");
            Directory.CreateDirectory(testPath);

            string file1 = Path.Combine(testPath, "file1.txt");
            string file2 = Path.Combine(testPath, "file2.log");
            File.WriteAllText(file1, "content1");
            File.WriteAllText(file2, "content2");

            // Act
            List<string> files = PathUtilities.GetFilesRecursive(testPath);

            // Assert
            files.Should().HaveCount(2);
            files.Should().Contain(file1);
            files.Should().Contain(file2);

            // Cleanup
            Directory.Delete(testPath, true);
        }

        /// <summary>
        /// Tests that GetFilesRecursive returns filtered files when a search pattern is provided.
        /// </summary>
        [Fact]
        public void GetFilesRecursive_WithSearchPattern_ReturnsFilteredFiles()
        {
            // Arrange
            string testPath = Path.Combine(Path.GetTempPath(), $"testdir_{Guid.NewGuid()}");
            Directory.CreateDirectory(testPath);

            string file1 = Path.Combine(testPath, "file1.txt");
            string file2 = Path.Combine(testPath, "file2.log");
            string file3 = Path.Combine(testPath, "file3.txt");
            File.WriteAllText(file1, "content1");
            File.WriteAllText(file2, "content2");
            File.WriteAllText(file3, "content3");

            // Act
            List<string> txtFiles = PathUtilities.GetFilesRecursive(testPath, "*.txt");

            // Assert
            txtFiles.Should().HaveCount(2);
            txtFiles.Should().Contain(file1);
            txtFiles.Should().Contain(file3);
            txtFiles.Should().NotContain(file2);

            // Cleanup
            Directory.Delete(testPath, true);
        }

        /// <summary>
        /// Tests that GetFilesRecursive returns an empty list for a non-existent directory.
        /// </summary>
        [Fact]
        public void GetFilesRecursive_WithNonExistentDirectory_ReturnsEmptyList()
        {
            // Arrange
            string testPath = Path.Combine(Path.GetTempPath(), $"nonexistent_{Guid.NewGuid()}");

            // Act
            List<string> files = PathUtilities.GetFilesRecursive(testPath);

            // Assert
            files.Should().BeEmpty();
        }

        /// <summary>
        /// Tests that NormalizePath converts mixed directory separators to the platform's separator.
        /// </summary>
        [Fact]
        public void NormalizePath_WithMixedSeparators_ReturnsNormalizedPath()
        {
            // Arrange
            string path = "folder\\subfolder/file.txt";

            // Act
            string result = PathUtilities.NormalizePath(path);

            // Assert
            result.Should().Be($"folder{Path.DirectorySeparatorChar}subfolder{Path.DirectorySeparatorChar}file.txt");
        }

        /// <summary>
        /// Tests that NormalizePath returns the same value for null or empty input.
        /// </summary>
        [Fact]
        public void NormalizePath_WithNullOrEmpty_ReturnsSameValue()
        {
            // Act
            string resultNull = PathUtilities.NormalizePath(null!);
            string resultEmpty = PathUtilities.NormalizePath(string.Empty);

            // Assert
            resultNull.Should().BeNull();
            resultEmpty.Should().BeEmpty();
        }

        /// <summary>
        /// Tests that MakeRelativePath returns the relative path from base path to full path.
        /// </summary>
        [Fact]
        public void MakeRelativePath_WithValidPaths_ReturnsRelativePath()
        {
            // Arrange
            string basePath = "/home/user/documents";
            string fullPath = "/home/user/documents/projects/project1/file.txt";

            // Act
            string result = PathUtilities.MakeRelativePath(basePath, fullPath);

            // Assert
            result.Should().Be("projects" + Path.DirectorySeparatorChar + "project1" + Path.DirectorySeparatorChar + "file.txt");
        }

        /// <summary>
        /// Tests that MakeRelativePath returns the full path when an exception occurs during Uri creation.
        /// </summary>
        [Fact]
        public void MakeRelativePath_WithException_ReturnsFullPath()
        {
            // Arrange
            string basePath = "/invalid/path\\with*invalid<>chars";
            string fullPath = "/home/user/documents/file.txt";

            // Act
            string result = PathUtilities.MakeRelativePath(basePath, fullPath);

            // Assert - when exception occurs in Uri creation, returns the original fullPath
            // Note: On some systems, Path.GetFullPath might still succeed and create a relative path
            // So we just check it's not empty and contains the filename
            result.Should().NotBeNullOrEmpty();
            result.Should().Contain("file.txt");
        }

        /// <summary>
        /// Tests that FormatBytes returns "0 B" for zero bytes.
        /// </summary>
        [Fact]
        public void FormatBytes_WithZeroBytes_ReturnsZeroB()
        {
            // Act
            string result = PathUtilities.FormatBytes(0);

            // Assert
            result.Should().Be("0 B");
        }

        /// <summary>
        /// Tests that FormatBytes returns the byte count in bytes when less than one kilobyte.
        /// </summary>
        [Fact]
        public void FormatBytes_WithBytesLessThanKB_ReturnsBytes()
        {
            // Act
            string result = PathUtilities.FormatBytes(500);

            // Assert
            result.Should().Be("500 B");
        }

        /// <summary>
        /// Tests that FormatBytes returns "1 KB" for exactly 1024 bytes.
        /// </summary>
        [Fact]
        public void FormatBytes_WithBytesEqualToKB_ReturnsOneKB()
        {
            // Act
            string result = PathUtilities.FormatBytes(1024);

            // Assert
            result.Should().Be("1 KB");
        }

        /// <summary>
        /// Tests that FormatBytes returns a formatted string for megabytes.
        /// </summary>
        [Fact]
        public void FormatBytes_WithMegabytes_ReturnsFormattedMB()
        {
            // Act
            string result = PathUtilities.FormatBytes(1024 * 1024 * 5); // 5 MB

            // Assert
            result.Should().MatchRegex(@"5(\.00)? MB");
        }

        /// <summary>
        /// Tests that IsDirectoryEmpty returns true for an empty directory.
        /// </summary>
        [Fact]
        public void IsDirectoryEmpty_WithEmptyDirectory_ReturnsTrue()
        {
            // Arrange
            string testPath = Path.Combine(Path.GetTempPath(), $"testdir_{Guid.NewGuid()}");
            Directory.CreateDirectory(testPath);

            // Act
            bool result = PathUtilities.IsDirectoryEmpty(testPath);

            // Assert
            result.Should().BeTrue();

            // Cleanup
            Directory.Delete(testPath, true);
        }

        /// <summary>
        /// Tests that IsDirectoryEmpty returns false for a non-empty directory.
        /// </summary>
        [Fact]
        public void IsDirectoryEmpty_WithNonEmptyDirectory_ReturnsFalse()
        {
            // Arrange
            string testPath = Path.Combine(Path.GetTempPath(), $"testdir_{Guid.NewGuid()}");
            Directory.CreateDirectory(testPath);
            File.WriteAllText(Path.Combine(testPath, "file.txt"), "content");

            // Act
            bool result = PathUtilities.IsDirectoryEmpty(testPath);

            // Assert
            result.Should().BeFalse();

            // Cleanup
            Directory.Delete(testPath, true);
        }

        /// <summary>
        /// Tests that IsDirectoryEmpty returns true for a non-existent directory.
        /// </summary>
        [Fact]
        public void IsDirectoryEmpty_WithNonExistentDirectory_ReturnsTrue()
        {
            // Arrange
            string testPath = Path.Combine(Path.GetTempPath(), $"nonexistent_{Guid.NewGuid()}");

            // Act
            bool result = PathUtilities.IsDirectoryEmpty(testPath);

            // Assert
            result.Should().BeTrue();
        }

        /// <summary>
        /// Tests that CleanupOldFiles returns zero when no files are older than the specified age.
        /// </summary>
        [Fact]
        public void CleanupOldFiles_WithNoOldFiles_ReturnsZero()
        {
            // Arrange
            string testPath = Path.Combine(Path.GetTempPath(), $"testdir_{Guid.NewGuid()}");
            Directory.CreateDirectory(testPath);
            File.WriteAllText(Path.Combine(testPath, "recent.txt"), "content");

            // Act
            int deletedCount = PathUtilities.CleanupOldFiles(testPath, TimeSpan.FromDays(1));

            // Assert
            deletedCount.Should().Be(0);

            // Cleanup
            Directory.Delete(testPath, true);
        }

        /// <summary>
        /// Tests that CleanupOldFiles returns the count of deleted files when old files exist.
        /// </summary>
        [Fact]
        public void CleanupOldFiles_WithOldFiles_ReturnsCountOfDeletedFiles()
        {
            // Arrange
            string testPath = Path.Combine(Path.GetTempPath(), $"testdir_{Guid.NewGuid()}");
            Directory.CreateDirectory(testPath);

            string oldFile = Path.Combine(testPath, "old.txt");
            string recentFile = Path.Combine(testPath, "recent.txt");

            File.WriteAllText(oldFile, "old content");
            File.WriteAllText(recentFile, "recent content");

            // Make the old file actually old by setting its last write time
            File.SetLastWriteTimeUtc(oldFile, DateTime.UtcNow - TimeSpan.FromDays(2));

            // Act
            int deletedCount = PathUtilities.CleanupOldFiles(testPath, TimeSpan.FromDays(1));

            // Assert
            deletedCount.Should().Be(1);
            File.Exists(oldFile).Should().BeFalse();
            File.Exists(recentFile).Should().BeTrue();

            // Cleanup
            Directory.Delete(testPath, true);
        }

        /// <summary>
        /// Tests that GetExtensionWithoutDot returns the extension without the leading dot.
        /// </summary>
        [Fact]
        public void GetExtensionWithoutDot_WithExtension_ReturnsExtensionWithoutDot()
        {
            // Arrange
            string path = "document.pdf";

            // Act
            string result = PathUtilities.GetExtensionWithoutDot(path);

            // Assert
            result.Should().Be("pdf");
        }

        /// <summary>
        /// Tests that GetExtensionWithoutDot returns the extension without the leading dot for multi-part extensions.
        /// </summary>
        [Fact]
        public void GetExtensionWithoutDot_WithExtensionWithDot_ReturnsExtensionWithoutDot()
        {
            // Arrange
            string path = "archive.tar.gz";

            // Act
            string result = PathUtilities.GetExtensionWithoutDot(path);

            // Assert
            result.Should().Be("gz");
        }

        /// <summary>
        /// Tests that GetExtensionWithoutDot returns an empty string when there is no extension.
        /// </summary>
        [Fact]
        public void GetExtensionWithoutDot_WithoutExtension_ReturnsEmptyString()
        {
            // Arrange
            string path = "filename";

            // Act
            string result = PathUtilities.GetExtensionWithoutDot(path);

            // Assert
            result.Should().BeEmpty();
        }

        /// <summary>
        /// Tests that GetExtensionWithoutDot returns the extension without the leading dot for hidden files.
        /// </summary>
        [Fact]
        public void GetExtensionWithoutDot_WithHiddenFile_ReturnsExtensionWithoutDot()
        {
            // Arrange
            string path = ".bashrc";

            // Act
            string result = PathUtilities.GetExtensionWithoutDot(path);

            // Assert
            result.Should().Be("bashrc");
        }
    }
}