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
using SqliteMultiTenant.Constants;
using SqliteMultiTenant.Exceptions;
using SqliteMultiTenant.Models;
using SqliteMultiTenant.Repositories;
using SqliteMultiTenant.Services;
using Xunit;

namespace SqliteMultiTenant.Tests
{
    /// <summary>
/// Contains unit tests for the <see cref="BackupService"/> class.
/// Tests various scenarios for backup operations including creation, retrieval, and status updates.
/// </summary>
public sealed class BackupServiceTests {
        private readonly IBackupRepository _mockBackupRepository;
        private readonly ILogger<BackupService> _mockLogger;
        private readonly BackupService _backupService;

        public BackupServiceTests()
        {
            _mockBackupRepository = Substitute.For<IBackupRepository>();
            _mockLogger.LogInformation("Test {TestName} started", "GetBackupAsync_ShouldReturnBackup_WhenBackupExists");
            _mockLogger = Substitute.For<ILogger<BackupService>>();
            _backupService = new BackupService(_mockBackupRepository, _mockLogger);
        }

        [Fact]
        /// <summary>
        /// Tests that <see cref="BackupService.GetBackupAsync"/> returns a backup when it exists in the repository.
        /// Verifies that the service correctly retrieves and returns backup data.
        /// </summary>
        public async Task GetBackupAsync_ShouldReturnBackup_WhenBackupExists()
        {
            _mockLogger.LogInformation("Starting {MethodName} test with backupId: {BackupId}", nameof(GetBackupAsync_ShouldReturnBackup_WhenBackupExists), "test_backup_id");

            // Arrange
            var backupId = "test_backup_id";
            var expectedBackup = new Backup { BackupId = backupId, DatabaseId = "db1" };
            _mockBackupRepository.GetByIdAsync(backupId, Arg.Any<CancellationToken>())
                .Returns(expectedBackup);

            // Act
            var result = await _backupService.GetBackupAsync(backupId);

            // Assert
            result.Should().BeEquivalentTo(expectedBackup);

            _mockLogger.LogInformation("Completed {MethodName} test successfully", nameof(GetBackupAsync_ShouldReturnBackup_WhenBackupExists));
        }

        [Fact]
        /// <summary>
        /// Tests that <see cref="BackupService.GetBackupAsync"/> returns null when the backup does not exist.
        /// Verifies proper null handling for non-existent backup IDs.
        /// </summary>
        public async Task GetBackupAsync_ShouldReturnNull_WhenBackupDoesNotExist()
        {
            _mockLogger.LogInformation("Starting {MethodName} test with backupId: {BackupId}", nameof(GetBackupAsync_ShouldReturnNull_WhenBackupDoesNotExist), "non_existent_id");

            // Arrange
            var backupId = "non_existent_id";
            _mockBackupRepository.GetByIdAsync(backupId, Arg.Any<CancellationToken>())
                .Returns((Backup)null);

            // Act
            var result = await _backupService.GetBackupAsync(backupId);

            // Assert
            result.Should().BeNull();

            _mockLogger.LogInformation("Completed {MethodName} test successfully", nameof(GetBackupAsync_ShouldReturnNull_WhenBackupDoesNotExist));
        }

        [Fact]
        /// <summary>
        /// Tests that <see cref="BackupService.GetBackupAsync"/> throws an <see cref="ArgumentException"/> when the backup ID is empty.
        /// Verifies parameter validation for empty backup IDs.
        /// </summary>
        public async Task GetBackupAsync_ShouldThrowArgumentException_WhenBackupIdIsEmpty()
        {
            // Arrange
            var backupId = "";

            // Act
            Func<Task> action = async () => await _backupService.GetBackupAsync(backupId);

            // Assert
            await action.Should().ThrowAsync<ArgumentException>()
                .WithMessage("Backup ID cannot be empty (Parameter 'backupId')");
        }

        [Fact]
        /// <summary>
        /// Tests that <see cref="BackupService.CreateBackupAsync"/> creates a new backup record.
        /// Verifies that the service creates a backup with correct default values and returns the created backup.
        /// </summary>
        public async Task CreateBackupAsync_ShouldCreateNewBackup()
        {
            _mockLogger.LogInformation("Starting {MethodName} test with databaseId: {DatabaseId}, createdBy: {CreatedBy}", nameof(CreateBackupAsync_ShouldCreateNewBackup), "test_db", "test_user");

            // Arrange
            var databaseId = "test_db";
            var createdBy = "test_user";
            var newBackup = new Backup { DatabaseId = databaseId, CreatedBy = createdBy };
            _mockBackupRepository.AddAsync(Arg.Any<Backup>(), Arg.Any<CancellationToken>())
                .Returns(callInfo => callInfo.Arg<Backup>());

            // Act
            var result = await _backupService.CreateBackupAsync(databaseId, BackupType.Full, createdBy);

            // Assert
            result.Should().NotBeNull();
            result.DatabaseId.Should().Be(databaseId);
            result.CreatedBy.Should().Be(createdBy);
            result.BackupType.Should().Be(BackupType.Full);
            result.Status.Should().Be(BackupStatus.Pending);
            result.BackupId.Should().NotBeEmpty();
            _mockLogger.AssertLoggedAny(LogLevel.Information);

            _mockLogger.LogInformation("Completed {MethodName} test successfully", nameof(CreateBackupAsync_ShouldCreateNewBackup));
        }

        [Fact]
        /// <summary>
        /// Tests that <see cref="BackupService.CreateBackupAsync"/> throws an <see cref="ArgumentException"/> when the database ID is empty.
        /// Verifies parameter validation for empty database IDs.
        /// </summary>
        public async Task CreateBackupAsync_ShouldThrowArgumentException_WhenDatabaseIdIsEmpty()
        {
            _mockLogger.LogInformation("Starting {MethodName} test with databaseId: {DatabaseId}, createdBy: {CreatedBy}", nameof(CreateBackupAsync_ShouldThrowArgumentException_WhenDatabaseIdIsEmpty), "", "test_user");

            // Arrange
            var databaseId = "";
            var createdBy = "test_user";

            // Act
            Func<Task> action = async () => await _backupService.CreateBackupAsync(databaseId, BackupType.Full, createdBy);

            // Assert
            await action.Should().ThrowAsync<ArgumentException>()
                .WithMessage("Database ID cannot be empty (Parameter 'databaseId')");

            _mockLogger.LogInformation("Completed {MethodName} test successfully", nameof(CreateBackupAsync_ShouldThrowArgumentException_WhenDatabaseIdIsEmpty));
        }

        [Fact]
        /// <summary>
        /// Tests that <see cref="BackupService.MarkBackupAsCompletedAsync"/> updates the backup status to completed.
        /// Verifies that the service updates the backup status, size, and duration when marking as completed.
        /// </summary>
        public async Task MarkBackupAsCompletedAsync_ShouldUpdateBackupStatus()
        {
            _mockLogger.LogInformation("Starting {MethodName} test with backupId: {BackupId}", nameof(MarkBackupAsCompletedAsync_ShouldUpdateBackupStatus), "pending_backup");

            // Arrange
            var backupId = "pending_backup";
            var backup = new Backup { BackupId = backupId, Status = BackupStatus.Pending };
            _mockBackupRepository.GetByIdAsync(backupId, Arg.Any<CancellationToken>())
                .Returns(backup);

            // Act
            await _backupService.MarkBackupAsCompletedAsync(backupId, 1024, 500);

            // Assert
            backup.Status.Should().Be(BackupStatus.Completed);
            backup.SizeBytes.Should().Be(1024);
            backup.DurationMs.Should().Be(500);
            _mockBackupRepository.Received(1).UpdateAsync(backup, Arg.Any<CancellationToken>());
            _mockLogger.AssertLoggedAny(LogLevel.Information);

            _mockLogger.LogInformation("Completed {MethodName} test successfully", nameof(MarkBackupAsCompletedAsync_ShouldUpdateBackupStatus));
        }

        [Fact]
        /// <summary>
        /// Tests that <see cref="BackupService.MarkBackupAsCompletedAsync"/> throws a <see cref="BackupException"/> when the backup is not found.
        /// Verifies proper error handling when attempting to mark a non-existent backup as completed.
        /// </summary>
        public async Task MarkBackupAsCompletedAsync_ShouldThrowBackupException_WhenBackupNotFound()
        {
            // Arrange
            var backupId = "non_existent_id";
            _mockBackupRepository.GetByIdAsync(backupId, Arg.Any<CancellationToken>())
                .Returns((Backup)null);

            // Act
            Func<Task> action = async () => await _backupService.MarkBackupAsCompletedAsync(backupId, 1024, 500);

            // Assert
            await action.Should().ThrowAsync<BackupException>()
                .WithMessage($"Backup with ID '{backupId}' was not found");
            _mockLogger.AssertLoggedAny(LogLevel.Error);
        }

        [Fact]
        /// <summary>
        /// Tests that <see cref="BackupService.MarkBackupAsFailedAsync"/> updates the backup status to failed and sets the error message.
        /// Verifies that the service updates the backup status and error message when marking as failed.
        /// </summary>
        public async Task MarkBackupAsFailedAsync_ShouldUpdateBackupStatusAndMessage()
        {
            // Arrange
            var backupId = "pending_backup";
            var errorMessage = "Disk full";
            var backup = new Backup { BackupId = backupId, Status = BackupStatus.Pending };
            _mockBackupRepository.GetByIdAsync(backupId, Arg.Any<CancellationToken>())
                .Returns(backup);

            // Act
            await _backupService.MarkBackupAsFailedAsync(backupId, errorMessage);

            // Assert
            backup.Status.Should().Be(BackupStatus.Failed);
            backup.ErrorMessage.Should().Be(errorMessage);
            _mockBackupRepository.Received(1).UpdateAsync(backup, Arg.Any<CancellationToken>());
            _mockLogger.AssertLoggedAny(LogLevel.Error);
        }
    }
}
