using System;
using System.Collections.Generic;
using System.IO;
using SqliteMultiTenant.Utilities;
using Xunit;

namespace SqliteMultiTenant.Tests
{
    public class FileSystemExtensionsTests : IDisposable
    {
        private readonly string _testDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

        public FileSystemExtensionsTests()
        {
            Directory.CreateDirectory(_testDir);
        }

        public void Dispose()
        {
            if (Directory.Exists(_testDir))
            {
                Directory.Delete(_testDir, true);
            }
        }

        [Fact]
        public void IsSafeFilePath_ShouldValidateCorrectly()
        {
            string baseDir = Path.Combine(_testDir, "base");
            Directory.CreateDirectory(baseDir);
            
            Assert.True(Path.Combine(baseDir, "file.txt").IsSafeFilePath(baseDir));
            Assert.False(Path.Combine(_testDir, "outside.txt").IsSafeFilePath(baseDir));
        }

        [Fact]
        public void EnsureDirectoryExists_ShouldCreateDirectory()
        {
            string newDir = Path.Combine(_testDir, "newDir");
            Assert.True(newDir.EnsureDirectoryExists());
            Assert.True(Directory.Exists(newDir));
        }

        [Fact]
        public void GetFileSizeBytes_And_SafeDelete_ShouldWork()
        {
            string filePath = Path.Combine(_testDir, "test.txt");
            File.WriteAllText(filePath, "hello world");
            
            Assert.Equal(11, filePath.GetFileSizeBytes());
            Assert.True(filePath.SafeDelete());
            Assert.False(File.Exists(filePath));
        }

        [Fact]
        public void GenerateBackupFileName_ShouldFollowFormat()
        {
            string tenantId = "tenant1";
            string fileName = tenantId.GenerateBackupFileName();
            Assert.StartsWith("backup_tenant1_", fileName);
            Assert.EndsWith(".db", fileName);
        }

        [Fact]
        public void GetFilesWithExtension_ShouldReturnFiles()
        {
            string subDir = Path.Combine(_testDir, "sub");
            Directory.CreateDirectory(subDir);
            string file1 = Path.Combine(subDir, "a.db");
            string file2 = Path.Combine(subDir, "b.txt");
            File.WriteAllText(file1, "");
            File.WriteAllText(file2, "");

            var files = _testDir.GetFilesWithExtension(".db");
            Assert.Contains(file1, files);
            Assert.DoesNotContain(file2, files);
        }

        [Fact]
        public void GetDirectorySizeBytes_ShouldReturnTotalSize()
        {
            string subDir = Path.Combine(_testDir, "sub");
            Directory.CreateDirectory(subDir);
            File.WriteAllText(Path.Combine(subDir, "1.txt"), "abc"); // 3 bytes
            File.WriteAllText(Path.Combine(subDir, "2.txt"), "def"); // 3 bytes

            Assert.Equal(6, _testDir.GetDirectorySizeBytes());
        }

        [Fact]
        public void SafeCopyFile_ShouldCopySuccessfully()
        {
            string source = Path.Combine(_testDir, "source.txt");
            string dest = Path.Combine(_testDir, "dest.txt");
            File.WriteAllText(source, "data");

            Assert.True(source.SafeCopyFile(dest));
            Assert.True(File.Exists(dest));
            Assert.Equal("data", File.ReadAllText(dest));
        }

        [Fact]
        public void GetFileCreationTimeUtc_ShouldReturnValidTime()
        {
            string filePath = Path.Combine(_testDir, "time.txt");
            File.WriteAllText(filePath, "data");

            var time = filePath.GetFileCreationTimeUtc();
            Assert.NotEqual(DateTime.UnixEpoch, time);
            Assert.True(time <= DateTime.UtcNow.AddSeconds(1));
        }
    }
}
