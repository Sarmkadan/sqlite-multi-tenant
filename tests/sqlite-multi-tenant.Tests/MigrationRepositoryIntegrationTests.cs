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
using System.Collections.Generic;

namespace SqliteMultiTenant.Tests
{
    public class MigrationRepositoryIntegrationTests : IDisposable
    {
        private readonly SqliteConnection _connection;
        private readonly DbContextOptions<TenantContext> _dbContextOptions;
        private readonly MigrationRepository _migrationRepository;

        public MigrationRepositoryIntegrationTests()
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

            _migrationRepository = new MigrationRepository(new TenantContext(_dbContextOptions));
        }

        private void SeedData(TenantContext context)
        {
            if (!context.Migrations.Any())
            {
                context.Migrations.Add(new Migration { MigrationId = "mig1", DatabaseId = "db1", Version = "1.0", Name = "Initial", Status = MigrationStatus.Completed, CreatedAt = DateTime.UtcNow.AddHours(-3), ExecutionOrder = 1 });
                context.Migrations.Add(new Migration { MigrationId = "mig2", DatabaseId = "db1", Version = "1.1", Name = "AddUserTable", Status = MigrationStatus.Pending, CreatedAt = DateTime.UtcNow.AddHours(-2), ExecutionOrder = 2 });
                context.Migrations.Add(new Migration { MigrationId = "mig3", DatabaseId = "db2", Version = "1.0", Name = "Initial", Status = MigrationStatus.Completed, CreatedAt = DateTime.UtcNow.AddHours(-1), ExecutionOrder = 1 });
                context.Migrations.Add(new Migration { MigrationId = "mig4", DatabaseId = "db1", Version = "1.2", Name = "AddIndex", Status = MigrationStatus.Failed, CreatedAt = DateTime.UtcNow.AddHours(-1), ExecutionOrder = 3, ErrorMessage = "Failed to create index" });
                context.SaveChanges();
            }
        }

        [Fact]
        public async Task GetAllAsync_ShouldReturnAllMigrations()
        {
            // Arrange
            // Act
            var migrations = await _migrationRepository.GetAllAsync();

            // Assert
            migrations.Should().NotBeNull();
            migrations.Should().HaveCount(4);
        }

        [Fact]
        public async Task GetByIdAsync_ShouldReturnCorrectMigration_WhenMigrationExists()
        {
            // Arrange
            var migrationId = "mig1";

            // Act
            var migration = await _migrationRepository.GetByIdAsync(migrationId);

            // Assert
            migration.Should().NotBeNull();
            migration.MigrationId.Should().Be(migrationId);
            migration.DatabaseId.Should().Be("db1");
        }

        [Fact]
        public async Task GetByIdAsync_ShouldReturnNull_WhenMigrationDoesNotExist()
        {
            // Arrange
            var nonExistingId = "non_existent_mig";

            // Act
            var migration = await _migrationRepository.GetByIdAsync(nonExistingId);

            // Assert
            migration.Should().BeNull();
        }

        [Fact]
        public async Task AddAsync_ShouldAddMigrationToDatabase()
        {
            // Arrange
            var newMigration = new Migration { MigrationId = "mig5", DatabaseId = "db3", Version = "1.0", Name = "NewDB", Status = MigrationStatus.Pending, CreatedAt = DateTime.UtcNow, ExecutionOrder = 1 };

            // Act
            var addedMigration = await _migrationRepository.AddAsync(newMigration);

            // Assert
            addedMigration.Should().NotBeNull();
            addedMigration.MigrationId.Should().Be("mig5");

            using (var context = new TenantContext(_dbContextOptions))
            {
                var migrationInDb = await context.Migrations.FirstOrDefaultAsync(m => m.MigrationId == "mig5");
                migrationInDb.Should().NotBeNull();
                migrationInDb.DatabaseId.Should().Be("db3");
            }
        }

        [Fact]
        public async Task UpdateAsync_ShouldUpdateMigrationInDatabase()
        {
            // Arrange
            Migration migrationToUpdate;
            using (var context = new TenantContext(_dbContextOptions))
            {
                migrationToUpdate = await context.Migrations.FirstAsync(m => m.MigrationId == "mig2");
                migrationToUpdate.Status = MigrationStatus.Completed;
                migrationToUpdate.ExecutionTimeMs = 150;
            }

            // Act
            var updatedMigration = await _migrationRepository.UpdateAsync(migrationToUpdate);

            // Assert
            updatedMigration.Should().NotBeNull();
            updatedMigration.Status.Should().Be(MigrationStatus.Completed);
            updatedMigration.ExecutionTimeMs.Should().Be(150);

            using (var context = new TenantContext(_dbContextOptions))
            {
                var migrationInDb = await context.Migrations.FirstOrDefaultAsync(m => m.MigrationId == "mig2");
                migrationInDb.Should().NotBeNull();
                migrationInDb.Status.Should().Be(MigrationStatus.Completed);
            }
        }

        [Fact]
        public async Task DeleteAsync_ShouldRemoveMigrationFromDatabase()
        {
            // Arrange
            var migrationIdToDelete = "mig3";

            // Act
            await _migrationRepository.DeleteAsync(migrationIdToDelete);

            // Assert
            using (var context = new TenantContext(_dbContextOptions))
            {
                var migrationInDb = await context.Migrations.FirstOrDefaultAsync(m => m.MigrationId == migrationIdToDelete);
                migrationInDb.Should().BeNull();
            }
        }

        [Fact]
        public async Task GetOrderedMigrationsAsync_ShouldReturnMigrationsInOrder()
        {
            // Arrange
            var databaseId = "db1";

            // Act
            var migrations = await _migrationRepository.GetOrderedMigrationsAsync(databaseId);

            // Assert
            migrations.Should().NotBeNull();
            migrations.Should().HaveCount(3); // mig1, mig2, mig4 for db1
            migrations[0].MigrationId.Should().Be("mig1");
            migrations[1].MigrationId.Should().Be("mig2");
            migrations[2].MigrationId.Should().Be("mig4");
        }

        [Fact]
        public async Task GetPendingMigrationsAsync_ShouldReturnOnlyPendingMigrations()
        {
            // Arrange
            var databaseId = "db1";

            // Act
            var pendingMigrations = await _migrationRepository.GetPendingMigrationsAsync(databaseId);

            // Assert
            pendingMigrations.Should().NotBeNull();
            pendingMigrations.Should().HaveCount(1);
            pendingMigrations.Should().ContainSingle(m => m.MigrationId == "mig2" && m.Status == MigrationStatus.Pending);
        }

        [Fact]
        public async Task GetAppliedMigrationsAsync_ShouldReturnOnlyAppliedMigrations()
        {
            // Arrange
            var databaseId = "db1";

            // Act
            var appliedMigrations = await _migrationRepository.GetAppliedMigrationsAsync(databaseId);

            // Assert
            appliedMigrations.Should().NotBeNull();
            appliedMigrations.Should().HaveCount(1);
            appliedMigrations.Should().ContainSingle(m => m.MigrationId == "mig1" && m.Status == MigrationStatus.Completed);
        }

        [Fact]
        public async Task GetByVersionAsync_ShouldReturnMigration_WhenVersionExists()
        {
            // Arrange
            var databaseId = "db1";
            var version = "1.1";

            // Act
            var migration = await _migrationRepository.GetByVersionAsync(databaseId, version);

            // Assert
            migration.Should().NotBeNull();
            migration.MigrationId.Should().Be("mig2");
        }

        [Fact]
        public async Task GetByVersionAsync_ShouldReturnNull_WhenVersionDoesNotExist()
        {
            // Arrange
            var databaseId = "db1";
            var version = "2.0";

            // Act
            var migration = await _migrationRepository.GetByVersionAsync(databaseId, version);

            // Assert
            migration.Should().BeNull();
        }

        [Fact]
        public async Task GetCountByDatabaseAsync_ShouldReturnCorrectCount()
        {
            // Arrange
            var databaseId = "db1";

            // Act
            var count = await _migrationRepository.GetCountByDatabaseAsync(databaseId);

            // Assert
            count.Should().Be(3);
        }

        [Fact]
        public async Task GetFailedMigrationsAsync_ShouldReturnOnlyFailedMigrations()
        {
            // Arrange
            var databaseId = "db1";

            // Act
            var failedMigrations = await _migrationRepository.GetFailedMigrationsAsync(databaseId);

            // Assert
            failedMigrations.Should().NotBeNull();
            failedMigrations.Should().HaveCount(1);
            failedMigrations.Should().ContainSingle(m => m.MigrationId == "mig4" && m.Status == MigrationStatus.Failed);
        }

        public void Dispose()
        {
            _connection.Close();
            _connection.Dispose();
        }
    }
}
