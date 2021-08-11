#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using SqliteMultiTenant.Exceptions;
using SqliteMultiTenant.Models;
using SqliteMultiTenant.Repositories;
using SqliteMultiTenant.Services;
using Xunit;

namespace SqliteMultiTenant.Tests
{
    public sealed class MigrationServiceTests {
        private readonly IMigrationRepository _mockMigrationRepository;
        private readonly ILogger<MigrationService> _mockLogger;
        private readonly MigrationService _migrationService;

        public MigrationServiceTests()
        {
            _mockMigrationRepository = Substitute.For<IMigrationRepository>();
            _mockLogger = Substitute.For<ILogger<MigrationService>>();
            _migrationService = new MigrationService(_mockMigrationRepository, _mockLogger);
        }

        [Fact]
        public async Task GetMigrationAsync_ShouldReturnMigration_WhenMigrationExists()
        {
            // Arrange
            var migrationId = "test_migration_id";
            var expectedMigration = new Migration { MigrationId = migrationId, DatabaseId = "db1", Version = "1.0" };
            _mockMigrationRepository.GetByIdAsync(migrationId, Arg.Any<CancellationToken>())
                .Returns(expectedMigration);

            // Act
            var result = await _migrationService.GetMigrationAsync(migrationId);

            // Assert
            result.Should().BeEquivalentTo(expectedMigration);
        }

        [Fact]
        public async Task GetMigrationAsync_ShouldReturnNull_WhenMigrationDoesNotExist()
        {
            // Arrange
            var migrationId = "non_existent_id";
            _mockMigrationRepository.GetByIdAsync(migrationId, Arg.Any<CancellationToken>())
                .Returns((Migration)null);

            // Act
            var result = await _migrationService.GetMigrationAsync(migrationId);

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public async Task GetMigrationAsync_ShouldThrowArgumentException_WhenMigrationIdIsEmpty()
        {
            // Arrange
            var migrationId = "";

            // Act
            Func<Task> action = async () => await _migrationService.GetMigrationAsync(migrationId);

            // Assert
            await action.Should().ThrowAsync<ArgumentException>()
                .WithMessage("Migration ID cannot be empty (Parameter 'migrationId')");
        }

        [Fact]
        public async Task CreateMigrationAsync_ShouldCreateNewMigration()
        {
            // Arrange
            var databaseId = "test_db";
            var version = "1.0";
            var name = "Initial Schema";
            var upScript = "CREATE TABLE Test (Id INT)";
            var newMigration = new Migration { DatabaseId = databaseId, Version = version, Name = name, UpScript = upScript };

            _mockMigrationRepository.GetByVersionAsync(databaseId, version, Arg.Any<CancellationToken>())
                .Returns((Migration)null); // No existing migration
            _mockMigrationRepository.GetByDatabaseAsync(databaseId, Arg.Any<CancellationToken>())
                .Returns(new List<Migration>()); // No existing migrations for execution order
            _mockMigrationRepository.AddAsync(Arg.Any<Migration>(), Arg.Any<CancellationToken>())
                .Returns(callInfo => callInfo.Arg<Migration>());

            // Act
            var result = await _migrationService.CreateMigrationAsync(databaseId, version, name, upScript);

            // Assert
            result.Should().NotBeNull();
            result.DatabaseId.Should().Be(databaseId);
            result.Version.Should().Be(version);
            result.Name.Should().Be(name);
            result.UpScript.Should().Be(upScript);
            result.Status.Should().Be(MigrationStatus.Pending);
            result.ExecutionOrder.Should().Be(1);
            _mockLogger.Received(1).LogInformation(Arg.Any<string>());
        }

        [Fact]
        public async Task CreateMigrationAsync_ShouldThrowMigrationException_WhenMigrationVersionAlreadyExists()
        {
            // Arrange
            var databaseId = "test_db";
            var version = "1.0";
            var name = "Initial Schema";
            var upScript = "CREATE TABLE Test (Id INT)";
            var existingMigration = new Migration { MigrationId = Guid.NewGuid().ToString(), DatabaseId = databaseId, Version = version };

            _mockMigrationRepository.GetByVersionAsync(databaseId, version, Arg.Any<CancellationToken>())
                .Returns(existingMigration);

            // Act
            Func<Task> action = async () => await _migrationService.CreateMigrationAsync(databaseId, version, name, upScript);

            // Assert
            await action.Should().ThrowAsync<MigrationException>()
                .WithMessage($"Migration with version '{version}' already applied to database '{databaseId}'.");
            _mockLogger.Received(1).LogError(Arg.Any<string>());
        }

        [Fact]
        public async Task MarkMigrationAsCompletedAsync_ShouldUpdateMigrationStatus()
        {
            // Arrange
            var migrationId = "pending_migration";
            var migration = new Migration { MigrationId = migrationId, Status = MigrationStatus.Pending };
            _mockMigrationRepository.GetByIdAsync(migrationId, Arg.Any<CancellationToken>())
                .Returns(migration);

            // Act
            await _migrationService.MarkMigrationAsCompletedAsync(migrationId, 100);

            // Assert
            migration.Status.Should().Be(MigrationStatus.Completed);
            migration.ExecutionTimeMs.Should().Be(100);
            _mockMigrationRepository.Received(1).UpdateAsync(migration, Arg.Any<CancellationToken>());
            _mockLogger.Received(1).LogInformation(Arg.Any<string>());
        }

        [Fact]
        public async Task MarkMigrationAsCompletedAsync_ShouldThrowMigrationException_WhenMigrationNotFound()
        {
            // Arrange
            var migrationId = "non_existent_id";
            _mockMigrationRepository.GetByIdAsync(migrationId, Arg.Any<CancellationToken>())
                .Returns((Migration)null);

            // Act
            Func<Task> action = async () => await _migrationService.MarkMigrationAsCompletedAsync(migrationId, 100);

            // Assert
            await action.Should().ThrowAsync<MigrationException>()
                .WithMessage($"Migration with ID '{migrationId}' not found.");
            _mockLogger.Received(1).LogError(Arg.Any<string>());
        }

        [Fact]
        public async Task MarkMigrationAsFailedAsync_ShouldUpdateMigrationStatusAndMessage()
        {
            // Arrange
            var migrationId = "pending_migration";
            var errorMessage = "Syntax error";
            var migration = new Migration { MigrationId = migrationId, Status = MigrationStatus.Pending };
            _mockMigrationRepository.GetByIdAsync(migrationId, Arg.Any<CancellationToken>())
                .Returns(migration);

            // Act
            await _migrationService.MarkMigrationAsFailedAsync(migrationId, errorMessage);

            // Assert
            migration.Status.Should().Be(MigrationStatus.Failed);
            migration.ErrorMessage.Should().Be(errorMessage);
            _mockMigrationRepository.Received(1).UpdateAsync(migration, Arg.Any<CancellationToken>());
            _mockLogger.Received(1).LogError(Arg.Any<string>());
        }
    }
}
