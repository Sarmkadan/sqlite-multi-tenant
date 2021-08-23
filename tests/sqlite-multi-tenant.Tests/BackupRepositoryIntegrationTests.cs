#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Data.SQLite;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;
using FluentAssertions;
using SqliteMultiTenant.Constants;
using SqliteMultiTenant.Models;
using SqliteMultiTenant.Repositories;

namespace SqliteMultiTenant.Tests
{
    public sealed class BackupRepositoryIntegrationTests : IDisposable {
        private readonly string _dbPath;
        private readonly string _connectionString;
        private readonly BackupRepository _backupRepository;

        public BackupRepositoryIntegrationTests()
        {
            _dbPath = Path.Combine(Path.GetTempPath(), $"backup_repo_tests_{Guid.NewGuid():N}.db");
            _connectionString = $"Data Source={_dbPath};Version=3;";

            _backupRepository = new BackupRepository(_connectionString, NullLogger<BackupRepository>.Instance);

            SeedData();
        }

        private void SeedData()
        {
            using var connection = new SQLiteConnection(_connectionString);
            connection.Open();

            InsertBackup(connection, "backup1", "db1", "/backups/backup1.bak", BackupStatus.Completed, DateTime.UtcNow.AddHours(-2), DateTime.UtcNow.AddDays(7));
            InsertBackup(connection, "backup2", "db1", "/backups/backup2.bak", BackupStatus.Pending, DateTime.UtcNow.AddHours(-1), DateTime.UtcNow.AddDays(7));
            InsertBackup(connection, "backup3", "db2", "/backups/backup3.bak", BackupStatus.Completed, DateTime.UtcNow.AddHours(-3), DateTime.UtcNow.AddDays(-1)); // Expired backup
        }

        private static void InsertBackup(SQLiteConnection connection, string backupId, string databaseId, string backupPath, BackupStatus status, DateTime createdAt, DateTime expiresAt)
        {
            using var command = connection.CreateCommand();
            command.CommandText = @"
                INSERT INTO Backups (BackupId, DatabaseId, BackupPath, BackupType, Status, CreatedAt, SizeBytes, OriginalSizeBytes, CompressionRatio, DurationMs, IsEncrypted, IsVerified, ExpiresAt)
                VALUES (@BackupId, @DatabaseId, @BackupPath, @BackupType, @Status, @CreatedAt, 0, 0, 0, 0, 0, @IsVerified, @ExpiresAt)";
            command.Parameters.AddWithValue("@BackupId", backupId);
            command.Parameters.AddWithValue("@DatabaseId", databaseId);
            command.Parameters.AddWithValue("@BackupPath", backupPath);
            command.Parameters.AddWithValue("@BackupType", (int)BackupType.Full);
            command.Parameters.AddWithValue("@Status", (int)status);
            command.Parameters.AddWithValue("@CreatedAt", createdAt);
            command.Parameters.AddWithValue("@IsVerified", status == BackupStatus.Completed ? 1 : 0);
            command.Parameters.AddWithValue("@ExpiresAt", expiresAt);
            command.ExecuteNonQuery();
        }

        [Fact]
        public async Task GetAllAsync_ShouldReturnAllBackups()
        {
            // Act
            var backups = await _backupRepository.GetAllAsync();

            // Assert
            backups.Should().NotBeNull();
            backups.Should().HaveCount(3);
        }

        [Fact]
        public async Task GetByIdAsync_ShouldReturnCorrectBackup_WhenBackupExists()
        {
            // Arrange
            var backupId = "backup1";

            // Act
            var backup = await _backupRepository.GetByIdAsync(backupId);

            // Assert
            backup.Should().NotBeNull();
            backup!.BackupId.Should().Be(backupId);
            backup.DatabaseId.Should().Be("db1");
        }

        [Fact]
        public async Task GetByIdAsync_ShouldReturnNull_WhenBackupDoesNotExist()
        {
            // Arrange
            var nonExistingId = "non_existent_backup";

            // Act
            var backup = await _backupRepository.GetByIdAsync(nonExistingId);

            // Assert
            backup.Should().BeNull();
        }

        [Fact]
        public async Task AddAsync_ShouldAddBackupToDatabase()
        {
            // Arrange
            var newBackup = new Backup { BackupId = "backup4", DatabaseId = "db2", BackupPath = "/backups/backup4.bak", CreatedAt = DateTime.UtcNow, Status = BackupStatus.Pending };

            // Act
            var addedBackup = await _backupRepository.AddAsync(newBackup);

            // Assert
            addedBackup.Should().NotBeNull();
            addedBackup.BackupId.Should().Be("backup4");

            var backupInDb = await _backupRepository.GetByIdAsync("backup4");
            backupInDb.Should().NotBeNull();
            backupInDb!.DatabaseId.Should().Be("db2");
        }

        [Fact]
        public async Task UpdateAsync_ShouldUpdateBackupInDatabase()
        {
            // Arrange
            var backupToUpdate = await _backupRepository.GetByIdAsync("backup1");
            backupToUpdate.Should().NotBeNull();
            backupToUpdate!.Status = BackupStatus.Failed;
            backupToUpdate.ErrorMessage = "Disk full";

            // Act
            await _backupRepository.UpdateAsync(backupToUpdate);

            // Assert
            var backupInDb = await _backupRepository.GetByIdAsync("backup1");
            backupInDb.Should().NotBeNull();
            backupInDb!.Status.Should().Be(BackupStatus.Failed);
            backupInDb.ErrorMessage.Should().Be("Disk full");
        }

        [Fact]
        public async Task DeleteAsync_ShouldRemoveBackupFromDatabase()
        {
            // Arrange
            var backupIdToDelete = "backup2";

            // Act
            await _backupRepository.DeleteAsync(backupIdToDelete);

            // Assert
            var backupInDb = await _backupRepository.GetByIdAsync(backupIdToDelete);
            backupInDb.Should().BeNull();
        }

        [Fact]
        public async Task GetByDatabaseAsync_ShouldReturnBackupsForGivenDatabase()
        {
            // Arrange
            var databaseId = "db1";

            // Act
            var backups = await _backupRepository.GetByDatabaseAsync(databaseId);

            // Assert
            backups.Should().NotBeNull();
            backups.Should().HaveCount(2);
            backups.Should().Contain(b => b.BackupId == "backup1");
            backups.Should().Contain(b => b.BackupId == "backup2");
            backups.Should().NotContain(b => b.BackupId == "backup3");
        }

        [Fact]
        public async Task GetCompletedBackupsAsync_ShouldReturnOnlyCompletedBackups()
        {
            // Arrange
            var databaseId = "db1";

            // Act
            var completedBackups = await _backupRepository.GetCompletedBackupsAsync(databaseId);

            // Assert
            completedBackups.Should().NotBeNull();
            completedBackups.Should().HaveCount(1);
            completedBackups.Should().ContainSingle(b => b.BackupId == "backup1" && b.Status == BackupStatus.Completed);
        }

        [Fact]
        public async Task GetLatestBackupAsync_ShouldReturnLatestBackupForDatabase()
        {
            // Arrange
            var databaseId = "db1";

            // Act
            var latestBackup = await _backupRepository.GetLatestBackupAsync(databaseId);

            // Assert
            latestBackup.Should().NotBeNull();
            latestBackup!.BackupId.Should().Be("backup2"); // 'backup2' has a later CreatedAt than 'backup1'
        }

        [Fact]
        public async Task GetExpiredBackupsAsync_ShouldReturnBackupsPastExpirationDate()
        {
            // Arrange
            // Backup3 is seeded with ExpiresAt in the past

            // Act
            var expiredBackups = await _backupRepository.GetExpiredBackupsAsync();

            // Assert
            expiredBackups.Should().NotBeNull();
            expiredBackups.Should().HaveCount(1);
            expiredBackups.Should().ContainSingle(b => b.BackupId == "backup3");
        }

        [Fact]
        public async Task GetCountByDatabaseAsync_ShouldReturnCorrectCount()
        {
            // Arrange
            var databaseId = "db1";

            // Act
            var count = await _backupRepository.GetCountByDatabaseAsync(databaseId);

            // Assert
            count.Should().Be(2);
        }

        public void Dispose()
        {
            if (File.Exists(_dbPath))
            {
                File.Delete(_dbPath);
            }
        }
    }
}
