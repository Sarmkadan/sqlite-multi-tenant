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
using SqliteMultiTenant.Models;
using SqliteMultiTenant.Repositories;
using SqliteMultiTenant.Constants;
using System.Collections.Generic;

namespace SqliteMultiTenant.Tests
{
    public sealed class MigrationRepositoryIntegrationTests : IDisposable {
        private readonly string _dbPath;
        private readonly string _connectionString;
        private readonly MigrationRepository _migrationRepository;

        public MigrationRepositoryIntegrationTests()
        {
            _dbPath = Path.Combine(Path.GetTempPath(), $"migration_repo_tests_{Guid.NewGuid():N}.db");
            _connectionString = $"Data Source={_dbPath};Version=3;";

            _migrationRepository = new MigrationRepository(_connectionString, NullLogger<MigrationRepository>.Instance);

            SeedData();
        }

        private void SeedData()
        {
            using var connection = new SQLiteConnection(_connectionString);
            connection.Open();

            InsertMigration(connection, "mig1", "db1", "1.0", "Initial", MigrationStatus.Completed, DateTime.UtcNow.AddHours(-3), 1, null);
            InsertMigration(connection, "mig2", "db1", "1.1", "AddUserTable", MigrationStatus.Pending, DateTime.UtcNow.AddHours(-2), 2, null);
            InsertMigration(connection, "mig3", "db2", "1.0", "Initial", MigrationStatus.Completed, DateTime.UtcNow.AddHours(-1), 1, null);
            InsertMigration(connection, "mig4", "db1", "1.2", "AddIndex", MigrationStatus.Failed, DateTime.UtcNow.AddHours(-1), 3, "Failed to create index");
        }

        private static void InsertMigration(SQLiteConnection connection, string migrationId, string databaseId, string version, string name, MigrationStatus status, DateTime createdAt, int executionOrder, string? errorMessage)
        {
            using var command = connection.CreateCommand();
            command.CommandText = @"
                INSERT INTO Migrations (MigrationId, DatabaseId, Version, Name, UpScript, Status, CreatedAt, ExecutionTimeMs, ExecutionOrder, IsRollbackable, ErrorMessage)
                VALUES (@MigrationId, @DatabaseId, @Version, @Name, @UpScript, @Status, @CreatedAt, 0, @ExecutionOrder, 1, @ErrorMessage)";
            command.Parameters.AddWithValue("@MigrationId", migrationId);
            command.Parameters.AddWithValue("@DatabaseId", databaseId);
            command.Parameters.AddWithValue("@Version", version);
            command.Parameters.AddWithValue("@Name", name);
            command.Parameters.AddWithValue("@UpScript", $"-- up script for {name}");
            command.Parameters.AddWithValue("@Status", (int)status);
            command.Parameters.AddWithValue("@CreatedAt", createdAt);
            command.Parameters.AddWithValue("@ExecutionOrder", executionOrder);
            command.Parameters.AddWithValue("@ErrorMessage", (object?)errorMessage ?? DBNull.Value);
            command.ExecuteNonQuery();
        }

        [Fact]
        public async Task GetAllAsync_ShouldReturnAllMigrations()
        {
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
            migration!.MigrationId.Should().Be(migrationId);
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
            var newMigration = new Migration { MigrationId = "mig5", DatabaseId = "db3", Version = "1.0", Name = "NewDB", UpScript = "-- create db3", Status = MigrationStatus.Pending, CreatedAt = DateTime.UtcNow, ExecutionOrder = 1 };

            // Act
            var addedMigration = await _migrationRepository.AddAsync(newMigration);

            // Assert
            addedMigration.Should().NotBeNull();
            addedMigration.MigrationId.Should().Be("mig5");

            var migrationInDb = await _migrationRepository.GetByIdAsync("mig5");
            migrationInDb.Should().NotBeNull();
            migrationInDb!.DatabaseId.Should().Be("db3");
        }

        [Fact]
        public async Task UpdateAsync_ShouldUpdateMigrationInDatabase()
        {
            // Arrange
            var migrationToUpdate = await _migrationRepository.GetByIdAsync("mig2");
            migrationToUpdate.Should().NotBeNull();
            migrationToUpdate!.Status = MigrationStatus.Completed;
            migrationToUpdate.ExecutionTimeMs = 150;

            // Act
            await _migrationRepository.UpdateAsync(migrationToUpdate);

            // Assert
            var migrationInDb = await _migrationRepository.GetByIdAsync("mig2");
            migrationInDb.Should().NotBeNull();
            migrationInDb!.Status.Should().Be(MigrationStatus.Completed);
            migrationInDb.ExecutionTimeMs.Should().Be(150);
        }

        [Fact]
        public async Task DeleteAsync_ShouldRemoveMigrationFromDatabase()
        {
            // Arrange
            var migrationIdToDelete = "mig3";

            // Act
            await _migrationRepository.DeleteAsync(migrationIdToDelete);

            // Assert
            var migrationInDb = await _migrationRepository.GetByIdAsync(migrationIdToDelete);
            migrationInDb.Should().BeNull();
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
            migration!.MigrationId.Should().Be("mig2");
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
            if (File.Exists(_dbPath))
            {
                File.Delete(_dbPath);
            }
        }
    }
}
