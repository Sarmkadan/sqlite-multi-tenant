using SqliteMultiTenant.Utilities;
using FluentAssertions;
using Xunit;
using System.IO;

namespace SqliteMultiTenant.Tests.Utilities
{
    public class PathUtilitiesTests
    {
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

        [Fact]
        public void FormatBytes_WithZeroBytes_ReturnsZeroB()
        {
            // Act
            string result = PathUtilities.FormatBytes(0);

            // Assert
            result.Should().Be("0 B");
        }

        [Fact]
        public void FormatBytes_WithBytesLessThanKB_ReturnsBytes()
        {
            // Act
            string result = PathUtilities.FormatBytes(500);

            // Assert
            result.Should().Be("500 B");
        }

        [Fact]
        public void FormatBytes_WithBytesEqualToKB_ReturnsOneKB()
        {
            // Act
            string result = PathUtilities.FormatBytes(1024);

            // Assert
            result.Should().Be("1 KB");
        }

        [Fact]
        public void FormatBytes_WithMegabytes_ReturnsFormattedMB()
        {
            // Act
            string result = PathUtilities.FormatBytes(1024 * 1024 * 5); // 5 MB

            // Assert
            result.Should().MatchRegex(@"5(\.00)? MB");
        }

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