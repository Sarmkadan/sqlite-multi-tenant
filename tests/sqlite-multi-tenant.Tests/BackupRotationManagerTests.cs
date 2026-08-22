#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =====================================================================

using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using SqliteMultiTenant.BackgroundWorkers;
using Xunit;

namespace SqliteMultiTenant.Tests.BackgroundWorkers
{
    /// <summary>
    /// Contains unit tests for the <see cref="BackupRotationManager"/> class.
    /// Tests retention count and date comparison logic for backup rotation.
    /// </summary>
    public sealed class BackupRotationManagerTests
    {
        private readonly ILogger<BackupRotationManager> _mockLogger;
        private readonly BackupRotationManager _manager;

        public BackupRotationManagerTests()
        {
            _mockLogger = Substitute.For<ILogger<BackupRotationManager>>();
            _manager = new BackupRotationManager(_mockLogger);
        }

        [Fact]
        /// <summary>
        /// Tests that exactly N newest backups are kept when exceeding max count.
        /// Verifies the count-based retention logic.
        /// </summary>
        public async Task RotateBackupsAsync_ShouldKeepExactlyMaxBackupCount_WhenExceedingCount()
        {
            // Arrange
            var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(tempDir);

            _mockLogger.LogInformation("Starting backup rotation test for directory {Directory}", tempDir);

            try
            {
                // Create 25 backup files (exceeds default max of 20)
                for (int i = 0; i < 25; i++)
                {
                    var filePath = Path.Combine(tempDir, $"backup_{i:D3}.db");
                    File.WriteAllText(filePath, "test backup data");
                    // Set creation time to simulate different backup times
                    // i=0 is oldest, i=24 is newest (when ordered descending)
                    File.SetCreationTimeUtc(filePath, DateTime.UtcNow.AddMinutes(-(24 - i)));
                }

                var policy = new BackupRotationPolicy
                {
                    MaxBackupAge = TimeSpan.FromDays(30),
                    MaxBackupCount = 20
                };

                // Act
                var result = await _manager.RotateBackupsAsync(tempDir, policy);

                _mockLogger.LogInformation(
                    "Backup rotation completed: total {TotalBackups}, remaining {RemainingBackups}, deleted by count {DeletedByCount}, deleted by age {DeletedByAge}",
                    result.TotalBackups, result.RemainingBackups, result.DeletedByCount, result.DeletedByAge);

                // Assert
                result.IsSuccessful.Should().BeTrue();
                result.TotalBackups.Should().Be(25);
                result.RemainingBackups.Should().Be(20);
                result.DeletedByCount.Should().Be(5);
                result.DeletedByAge.Should().Be(0);

                // Verify exactly 20 files remain
                var remainingFiles = Directory.GetFiles(tempDir, "*.db");
                remainingFiles.Length.Should().Be(20);

                // Verify oldest 5 files were deleted (files 0-4)
                var fileNames = remainingFiles.Select(Path.GetFileName).ToList();
                for (int i = 0; i < 5; i++)
                {
                    var expectedName = $"backup_{i:D3}.db";
                    fileNames.Should().NotContain(expectedName);
                }
            }
            finally
            {
                // Cleanup
                if (Directory.Exists(tempDir))
                {
                    Directory.Delete(tempDir, true);
                }
            }
        }

        [Fact]
        /// <summary>
        /// Tests that backups older than max age are deleted.
        /// Verifies the age-based retention logic.
        /// </summary>
        public async Task RotateBackupsAsync_ShouldDeleteBackupsOlderThanMaxAge()
        {
            // Arrange
            var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(tempDir);

            _mockLogger.LogInformation("Starting backup rotation test for directory {Directory}", tempDir);

            try
            {
                // Create backups with different ages
                // 10 recent backups (within 29 days)
                for (int i = 0; i < 10; i++)
                {
                    var filePath = Path.Combine(tempDir, $"recent_{i:D3}.db");
                    File.WriteAllText(filePath, "test backup data");
                    // i=0 is oldest recent, i=9 is newest recent
                    File.SetCreationTimeUtc(filePath, DateTime.UtcNow.AddDays(-29).AddMinutes(-(9 - i)));
                }

                // 5 old backups (31+ days old)
                for (int i = 0; i < 5; i++)
                {
                    var filePath = Path.Combine(tempDir, $"old_{i:D3}.db");
                    File.WriteAllText(filePath, "test backup data");
                    // i=0 is oldest old, i=4 is newest old
                    File.SetCreationTimeUtc(filePath, DateTime.UtcNow.AddDays(-31).AddMinutes(-(4 - i)));
                }

                var policy = new BackupRotationPolicy
                {
                    MaxBackupAge = TimeSpan.FromDays(30),
                    MaxBackupCount = 100 // High enough to not trigger count-based deletion
                };

                // Act
                var result = await _manager.RotateBackupsAsync(tempDir, policy);

                _mockLogger.LogInformation(
                    "Backup rotation completed: total {TotalBackups}, remaining {RemainingBackups}, deleted by count {DeletedByCount}, deleted by age {DeletedByAge}",
                    result.TotalBackups, result.RemainingBackups, result.DeletedByCount, result.DeletedByAge);

                // Assert
                result.IsSuccessful.Should().BeTrue();
                result.TotalBackups.Should().Be(15);
                result.RemainingBackups.Should().Be(10);
                result.DeletedByAge.Should().Be(5);
                result.DeletedByCount.Should().Be(0);

                // Verify only recent backups remain
                var remainingFiles = Directory.GetFiles(tempDir, "*.db");
                remainingFiles.Length.Should().Be(10);

                // Verify all remaining files are recent (not old_*)
                var fileNames = remainingFiles.Select(Path.GetFileName).ToList();
                foreach (var name in fileNames)
                {
                    name.Should().StartWith("recent_");
                }
            }
            finally
            {
                // Cleanup
                if (Directory.Exists(tempDir))
                {
                    Directory.Delete(tempDir, true);
                }
            }
        }

        [Fact]
        /// <summary>
        /// Tests that boundary case: backup created exactly at cutoff date is deleted.
        /// Verifies that files AT the cutoff date are deleted (not just older than).
        /// This is the main bug fix test - previously used '<' which excluded boundary files.
        /// </summary>
        public async Task RotateBackupsAsync_ShouldDeleteBackupAtCutoffDate()
        {
            // Arrange
            var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(tempDir);

            _mockLogger.LogInformation("Starting backup rotation test for directory {Directory}", tempDir);

            try
            {
                // Create a backup file with creation time exactly at cutoff date
                var cutoffTime = DateTime.UtcNow.AddDays(-30); // Exactly 30 days ago
                var filePath = Path.Combine(tempDir, "boundary.db");
                File.WriteAllText(filePath, "test backup data");
                File.SetCreationTimeUtc(filePath, cutoffTime);

                var policy = new BackupRotationPolicy
                {
                    MaxBackupAge = TimeSpan.FromDays(30),
                    MaxBackupCount = 100
                };

                // Act
                var result = await _manager.RotateBackupsAsync(tempDir, policy);

                _mockLogger.LogInformation(
                    "Backup rotation completed: total {TotalBackups}, remaining {RemainingBackups}, deleted by count {DeletedByCount}, deleted by age {DeletedByAge}",
                    result.TotalBackups, result.RemainingBackups, result.DeletedByCount, result.DeletedByAge);

                // Assert
                result.IsSuccessful.Should().BeTrue();
                result.TotalBackups.Should().Be(1);
                result.RemainingBackups.Should().Be(0);
                result.DeletedByAge.Should().Be(1);
                result.DeletedByCount.Should().Be(0);

                // Verify file was deleted
                var remainingFiles = Directory.GetFiles(tempDir, "*.db");
                remainingFiles.Length.Should().Be(0);
            }
            finally
            {
                // Cleanup
                if (Directory.Exists(tempDir))
                {
                    Directory.Delete(tempDir, true);
                }
            }
        }

        [Fact]
        /// <summary>
        /// Tests that same-day backups are handled correctly.
        /// Verifies that backups created on the same day but at different times are handled properly.
        /// </summary>
        public async Task RotateBackupsAsync_ShouldHandleSameDayBackupsCorrectly()
        {
            // Arrange
            var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(tempDir);

            _mockLogger.LogInformation("Starting backup rotation test for directory {Directory}", tempDir);

            try
            {
                // Create multiple backups on the same day (within 24 hours)
                for (int i = 0; i < 25; i++)
                {
                    var filePath = Path.Combine(tempDir, $"same_day_{i:D3}.db");
                    File.WriteAllText(filePath, "test backup data");
                    // All created within same day but different times
                    // i=0 is oldest, i=24 is newest
                    File.SetCreationTimeUtc(filePath, DateTime.UtcNow.AddHours(-(24 - i)));
                }

                var policy = new BackupRotationPolicy
                {
                    MaxBackupAge = TimeSpan.FromHours(25), // Keep only 25 hours of backups (more than 24)
                    MaxBackupCount = 10
                };

                // Act
                var result = await _manager.RotateBackupsAsync(tempDir, policy);

                // Assert
                result.IsSuccessful.Should().BeTrue();
                result.TotalBackups.Should().Be(25);
                result.RemainingBackups.Should().Be(10);
                result.DeletedByAge.Should().Be(0); // All within 1 day, so none deleted by age
                result.DeletedByCount.Should().Be(15); // 15 deleted by count limit

                // Verify exactly 10 files remain
                var remainingFiles = Directory.GetFiles(tempDir, "*.db");
                remainingFiles.Length.Should().Be(10);
            }
            finally
            {
                // Cleanup
                if (Directory.Exists(tempDir))
                {
                    Directory.Delete(tempDir, true);
                }
            }
        }

        [Fact]
        /// <summary>
        /// Tests that when both age and count limits apply, both are enforced correctly.
        /// </summary>
        public async Task RotateBackupsAsync_ShouldEnforceBothAgeAndCountLimits()
        {
            // Arrange
            var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(tempDir);

            try
            {
                // Create 30 backups: 15 old (35 days), 15 recent (5 days)
                for (int i = 0; i < 15; i++)
                {
                    var filePath = Path.Combine(tempDir, $"old_{i:D3}.db");
                    File.WriteAllText(filePath, "test backup data");
                    File.SetCreationTimeUtc(filePath, DateTime.UtcNow.AddDays(-35).AddMinutes(-(14 - i)));
                }

                for (int i = 0; i < 15; i++)
                {
                    var filePath = Path.Combine(tempDir, $"recent_{i:D3}.db");
                    File.WriteAllText(filePath, "test backup data");
                    File.SetCreationTimeUtc(filePath, DateTime.UtcNow.AddDays(-5).AddMinutes(-(14 - i)));
                }

                var policy = new BackupRotationPolicy
                {
                    MaxBackupAge = TimeSpan.FromDays(30),
                    MaxBackupCount = 10
                };

                // Act
                var result = await _manager.RotateBackupsAsync(tempDir, policy);

                // Assert
                result.IsSuccessful.Should().BeTrue();
                result.TotalBackups.Should().Be(30);
                result.RemainingBackups.Should().Be(10);

                // Should delete 15 old backups by age + 5 oldest recent backups by count = 20 total
                // (only 5 more needed to reach count limit of 10)
                result.DeletedByAge.Should().Be(15);
                result.DeletedByCount.Should().Be(5);

                // Verify exactly 10 files remain (the 10 most recent)
                var remainingFiles = Directory.GetFiles(tempDir, "*.db");
                remainingFiles.Length.Should().Be(10);

                // All remaining should be recent files
                var fileNames = remainingFiles.Select(Path.GetFileName).ToList();
                foreach (var name in fileNames)
                {
                    name.Should().StartWith("recent_");
                }
            }
            finally
            {
                // Cleanup
                if (Directory.Exists(tempDir))
                {
                    Directory.Delete(tempDir, true);
                }
            }
        }

        [Fact]
        /// <summary>
        /// Tests that empty directory is handled gracefully.
        /// </summary>
        public async Task RotateBackupsAsync_ShouldHandleEmptyDirectory()
        {
            // Arrange
            var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(tempDir);

            try
            {
                var policy = new BackupRotationPolicy
                {
                    MaxBackupAge = TimeSpan.FromDays(30),
                    MaxBackupCount = 10
                };

                // Act
                var result = await _manager.RotateBackupsAsync(tempDir, policy);

                // Assert
                result.IsSuccessful.Should().BeTrue();
                result.TotalBackups.Should().Be(0);
                result.RemainingBackups.Should().Be(0);
                result.DeletedByAge.Should().Be(0);
                result.DeletedByCount.Should().Be(0);
            }
            finally
            {
                // Cleanup
                if (Directory.Exists(tempDir))
                {
                    Directory.Delete(tempDir, true);
                }
            }
        }

        [Fact]
        /// <summary>
        /// Tests that non-existent directory is handled gracefully.
        /// </summary>
        public async Task RotateBackupsAsync_ShouldHandleNonExistentDirectory()
        {
            // Arrange
            var nonExistentDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString(), "does_not_exist");
            var policy = new BackupRotationPolicy
            {
                MaxBackupAge = TimeSpan.FromDays(30),
                MaxBackupCount = 10
            };

            // Act
            var result = await _manager.RotateBackupsAsync(nonExistentDir, policy);

            // Assert - returns unsuccessful result for non-existent directory
            result.IsSuccessful.Should().BeFalse();
            result.TotalBackups.Should().Be(0);
            result.RemainingBackups.Should().Be(0);
            result.DeletedByAge.Should().Be(0);
            result.DeletedByCount.Should().Be(0);
        }

        [Fact]
        /// <summary>
        /// Tests that exactly N backups are kept when count is the limiting factor.
        /// </summary>
        public async Task RotateBackupsAsync_ShouldKeepExactlyNNewest_WhenCountIsLimit()
        {
            // Arrange
            var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(tempDir);

            try
            {
                // Create exactly N backups
                int backupCount = 5;
                for (int i = 0; i < backupCount; i++)
                {
                    var filePath = Path.Combine(tempDir, $"backup_{i:D3}.db");
                    File.WriteAllText(filePath, "test backup data");
                    File.SetCreationTimeUtc(filePath, DateTime.UtcNow.AddMinutes(-(backupCount - 1 - i)));
                }

                var policy = new BackupRotationPolicy
                {
                    MaxBackupAge = TimeSpan.FromDays(30),
                    MaxBackupCount = 5
                };

                // Act
                var result = await _manager.RotateBackupsAsync(tempDir, policy);

                // Assert
                result.IsSuccessful.Should().BeTrue();
                result.TotalBackups.Should().Be(5);
                result.RemainingBackups.Should().Be(5);
                result.DeletedByAge.Should().Be(0);
                result.DeletedByCount.Should().Be(0);

                // Verify all 5 files remain
                var remainingFiles = Directory.GetFiles(tempDir, "*.db");
                remainingFiles.Length.Should().Be(5);
            }
            finally
            {
                // Cleanup
                if (Directory.Exists(tempDir))
                {
                    Directory.Delete(tempDir, true);
                }
            }
        }

        [Fact]
        /// <summary>
        /// Tests that policy with zero values is handled gracefully (no exception thrown).
        /// The method doesn't validate policy values, so it should handle them gracefully.
        /// </summary>
        public async Task RotateBackupsAsync_ShouldHandleZeroPolicyValuesGracefully()
        {
            // Arrange
            var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(tempDir);

            try
            {
                var invalidPolicy = new BackupRotationPolicy
                {
                    MaxBackupAge = TimeSpan.Zero,
                    MaxBackupCount = 0
                };

                // Act
                var result = await _manager.RotateBackupsAsync(tempDir, invalidPolicy);

                // Assert - should complete without throwing, even with invalid policy
                result.IsSuccessful.Should().BeTrue();
            }
            finally
            {
                // Cleanup
                if (Directory.Exists(tempDir))
                {
                    Directory.Delete(tempDir, true);
                }
            }
        }
    }
}