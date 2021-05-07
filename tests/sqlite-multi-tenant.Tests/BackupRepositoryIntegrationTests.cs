// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;
using FluentAssertions;
using SqliteMultiTenant.Models;
using SqliteMultiTenant.Repositories;
using SqliteMultiTenant.Constants;

namespace SqliteMultiTenant.Tests
{
    public class BackupRepositoryIntegrationTests : IDisposable
    {
        private readonly SqliteConnection _connection;
        private readonly DbContextOptions<TenantContext> _dbContextOptions;
        private readonly BackupRepository _backupRepository;

        public BackupRepositoryIntegrationTests()
        {
            _connection = new SqliteConnection("DataSource=:memory:");
            _connection.Open();

            _dbContextOptions = new DbContextOptionsBuilder<TenantContext>()
                .UseSqlite(_connection)
                .Options;

            using (var context = new TenantContext(_dbContextOptions))
            {
                context.Database.EnsureCreated();
                SeedData(context);
            }

            _backupRepository = new BackupRepository(new TenantContext(_dbContextOptions));
        }

        private void SeedData(TenantContext context)
        {
            if (!context.Backups.Any())
            {
                context.Backups.Add(new Backup { BackupId = "backup1", DatabaseId = "db1", CreatedAt = DateTime.UtcNow.AddHours(-2), Status = BackupStatus.Completed, ExpiresAt = DateTime.UtcNow.AddDays(7) });
                context.Backups.Add(new Backup { BackupId = "backup2", DatabaseId = "db1", CreatedAt = DateTime.UtcNow.AddHours(-1), Status = BackupStatus.Pending, ExpiresAt = DateTime.UtcNow.AddDays(7) });
                context.Backups.Add(new Backup { BackupId = "backup3", DatabaseId = "db2", CreatedAt = DateTime.UtcNow.AddHours(-3), Status = BackupStatus.Completed, ExpiresAt = DateTime.UtcNow.AddDays(-1) }); // Expired backup
                context.SaveChanges();
            }
        }

        [Fact]
        public async Task GetAllAsync_ShouldReturnAllBackups()
        {
            // Arrange
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
            backup.BackupId.Should().Be(backupId);
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
            var newBackup = new Backup { BackupId = "backup4", DatabaseId = "db2", CreatedAt = DateTime.UtcNow, Status = BackupStatus.Pending };

            // Act
            var addedBackup = await _backupRepository.AddAsync(newBackup);

            // Assert
            addedBackup.Should().NotBeNull();
            addedBackup.BackupId.Should().Be("backup4");

            using (var context = new TenantContext(_dbContextOptions))
            {
                var backupInDb = await context.Backups.FirstOrDefaultAsync(b => b.BackupId == "backup4");
                backupInDb.Should().NotBeNull();
                backupInDb.DatabaseId.Should().Be("db2");
            }
        }

        [Fact]
        public async Task UpdateAsync_ShouldUpdateBackupInDatabase()
        {
            // Arrange
            Backup backupToUpdate;
            using (var context = new TenantContext(_dbContextOptions))
            {
                backupToUpdate = await context.Backups.FirstAsync(b => b.BackupId == "backup1");
                backupToUpdate.Status = BackupStatus.Failed;
                backupToUpdate.ErrorMessage = "Disk full";
            }

            // Act
            var updatedBackup = await _backupRepository.UpdateAsync(backupToUpdate);

            // Assert
            updatedBackup.Should().NotBeNull();
            updatedBackup.Status.Should().Be(BackupStatus.Failed);
            updatedBackup.ErrorMessage.Should().Be("Disk full");

            using (var context = new TenantContext(_dbContextOptions))
            {
                var backupInDb = await context.Backups.FirstOrDefaultAsync(b => b.BackupId == "backup1");
                backupInDb.Should().NotBeNull();
                backupInDb.Status.Should().Be(BackupStatus.Failed);
            }
        }

        [Fact]
        public async Task DeleteAsync_ShouldRemoveBackupFromDatabase()
        {
            // Arrange
            var backupIdToDelete = "backup2";

            // Act
            await _backupRepository.DeleteAsync(backupIdToDelete);

            // Assert
            using (var context = new TenantContext(_dbContextOptions))
            {
                var backupInDb = await context.Backups.FirstOrDefaultAsync(b => b.BackupId == backupIdToDelete);
                backupInDb.Should().BeNull();
            }
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
            latestBackup.BackupId.Should().Be("backup2"); // 'backup2' has a later CreatedAt than 'backup1'
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
            _connection.Close();
            _connection.Dispose();
        }
    }
}
