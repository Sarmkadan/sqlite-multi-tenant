using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using SqliteMultiTenant.Constants;
using SqliteMultiTenant.Models;
using SqliteMultiTenant.Repositories;
using Xunit;

namespace SqliteMultiTenant.Tests
{
    public class BackupRepositoryTests : IDisposable
    {
        private readonly string _dbPath;
        private readonly BackupRepository _repository;

        public BackupRepositoryTests()
        {
            // Create a temporary file‑based SQLite database so each connection sees the same schema.
            _dbPath = Path.GetTempFileName();
            var connectionString = $"Data Source={_dbPath};Version=3;";
            _repository = new BackupRepository(connectionString, NullLogger<BackupRepository>.Instance);
        }

        public void Dispose()
        {
            // Clean up the temporary database file after each test run.
            if (File.Exists(_dbPath))
                File.Delete(_dbPath);
        }

        private Backup CreateSampleBackup(
            string backupId = "b1",
            string databaseId = "db1",
            BackupStatus status = BackupStatus.Completed,
            bool isVerified = true,
            DateTime? createdAt = null)
        {
            return new Backup
            {
                BackupId = backupId,
                DatabaseId = databaseId,
                BackupPath = $"/tmp/{backupId}.bak",
                BackupType = BackupType.Full,
                Status = status,
                CreatedAt = createdAt ?? DateTime.UtcNow,
                CompletedAt = DateTime.UtcNow,
                VerifiedAt = isVerified ? DateTime.UtcNow : null,
                SizeBytes = 1024,
                OriginalSizeBytes = 2048,
                CompressionRatio = 50,
                CreatedBy = "tester",
                VerifiedBy = isVerified ? "tester" : null,
                ErrorMessage = null,
                DurationMs = 100,
                IsEncrypted = false,
                IsVerified = isVerified,
                ExpiresAt = null,
                Tags = null
            };
        }

        [Fact]
        public async Task AddAsync_ValidBackup_ShouldBeRetrievable()
        {
            var backup = CreateSampleBackup();
            await _repository.AddAsync(backup);

            var retrieved = await _repository.GetByIdAsync(backup.BackupId);
            Assert.NotNull(retrieved);
            Assert.Equal(backup.BackupId, retrieved!.BackupId);
            Assert.Equal(backup.DatabaseId, retrieved.DatabaseId);
        }

        [Fact]
        public async Task GetAllAsync_ShouldReturnAllBackups()
        {
            var b1 = CreateSampleBackup("b1", "db1");
            var b2 = CreateSampleBackup("b2", "db2");
            await _repository.AddAsync(b1);
            await _repository.AddAsync(b2);

            var all = await _repository.GetAllAsync();
            Assert.Equal(2, all.Count);
            Assert.Contains(all, b => b.BackupId == "b1");
            Assert.Contains(all, b => b.BackupId == "b2");
        }

        [Fact]
        public async Task GetByDatabaseAsync_FiltersByDatabaseId()
        {
            var b1 = CreateSampleBackup("b1", "db1");
            var b2 = CreateSampleBackup("b2", "db1");
            var b3 = CreateSampleBackup("b3", "db2");
            await _repository.AddAsync(b1);
            await _repository.AddAsync(b2);
            await _repository.AddAsync(b3);

            var db1Backups = await _repository.GetByDatabaseAsync("db1");
            Assert.Equal(2, db1Backups.Count);
            Assert.All(db1Backups, b => Assert.Equal("db1", b.DatabaseId));
        }

        [Fact]
        public async Task GetCompletedBackupsAsync_ReturnsOnlyCompleted()
        {
            var completed = CreateSampleBackup("c1", "db1", BackupStatus.Completed);
            var pending = CreateSampleBackup("p1", "db1", BackupStatus.InProgress);
            await _repository.AddAsync(completed);
            await _repository.AddAsync(pending);

            var result = await _repository.GetCompletedBackupsAsync("db1");
            Assert.Single(result);
            Assert.Equal("c1", result[0].BackupId);
        }

        [Fact]
        public async Task GetVerifiedBackupsAsync_ReturnsOnlyVerified()
        {
            var verified = CreateSampleBackup("v1", "db1", BackupStatus.Completed, true);
            var notVerified = CreateSampleBackup("nv1", "db1", BackupStatus.Completed, false);
            await _repository.AddAsync(verified);
            await _repository.AddAsync(notVerified);

            var result = await _repository.GetVerifiedBackupsAsync("db1");
            Assert.Single(result);
            Assert.Equal("v1", result[0].BackupId);
        }

        [Fact]
        public async Task GetFailedBackupsAsync_ReturnsOnlyFailed()
        {
            var failed = CreateSampleBackup("f1", "db1", BackupStatus.Failed);
            var completed = CreateSampleBackup("c1", "db1", BackupStatus.Completed);
            await _repository.AddAsync(failed);
            await _repository.AddAsync(completed);

            var result = await _repository.GetFailedBackupsAsync("db1");
            Assert.Single(result);
            Assert.Equal("f1", result[0].BackupId);
        }

        [Fact]
        public async Task GetLatestBackupAsync_ReturnsMostRecentByCreatedAt()
        {
            var older = CreateSampleBackup("old", "db1", createdAt: DateTime.UtcNow.AddHours(-2));
            var newer = CreateSampleBackup("new", "db1", createdAt: DateTime.UtcNow.AddHours(-1));
            await _repository.AddAsync(older);
            await _repository.AddAsync(newer);

            var latest = await _repository.GetLatestBackupAsync("db1");
            Assert.NotNull(latest);
            Assert.Equal("new", latest!.BackupId);
        }

        [Fact]
        public async Task AddAsync_InvalidBackup_ThrowsArgumentException()
        {
            var invalid = new Backup
            {
                // Intentionally omit required fields such as BackupId.
                DatabaseId = "db1",
                BackupPath = "/tmp/invalid.bak",
                BackupType = BackupType.Full,
                Status = BackupStatus.Completed,
                CreatedAt = DateTime.UtcNow
            };

            await Assert.ThrowsAsync<ArgumentException>(async () => await _repository.AddAsync(invalid));
        }

        [Fact]
        public async Task GetByIdAsync_NonExisting_ReturnsNull()
        {
            var result = await _repository.GetByIdAsync("nonexistent");
            Assert.Null(result);
        }
    }
}
