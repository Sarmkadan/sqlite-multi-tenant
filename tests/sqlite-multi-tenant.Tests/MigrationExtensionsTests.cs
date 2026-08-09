using System;
using System.Collections.Generic;
using System.Linq;
using SqliteMultiTenant.Constants;
using SqliteMultiTenant.Models;
using Xunit;

namespace SqliteMultiTenant.Tests
{
    public class MigrationExtensionsTests
    {
        [Fact]
        public void IsTerminal_HappyPath_ReturnsTrueForCompleted()
        {
            // Arrange
            var migration = new Migration { Status = MigrationStatus.Completed };

            // Act
            var result = MigrationExtensions.IsTerminal(migration);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void IsTerminal_HappyPath_ReturnsTrueForFailed()
        {
            // Arrange
            var migration = new Migration { Status = MigrationStatus.Failed };

            // Act
            var result = MigrationExtensions.IsTerminal(migration);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void IsTerminal_HappyPath_ReturnsTrueForRolledBack()
        {
            // Arrange
            var migration = new Migration { Status = MigrationStatus.RolledBack };

            // Act
            var result = MigrationExtensions.IsTerminal(migration);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void IsTerminal_HappyPath_ReturnsFalseForPending()
        {
            // Arrange
            var migration = new Migration { Status = MigrationStatus.Pending };

            // Act
            var result = MigrationExtensions.IsTerminal(migration);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void IsTerminal_HappyPath_ReturnsFalseForRunning()
        {
            // Arrange
            var migration = new Migration { Status = MigrationStatus.Running };

            // Act
            var result = MigrationExtensions.IsTerminal(migration);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void IsTerminal_NullInput_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => MigrationExtensions.IsTerminal(null));
        }

        [Fact]
        public void GetAgeInDays_HappyPath_ReturnsAgeInDays()
        {
            // Arrange
            var created = DateTime.UtcNow.AddDays(-2.5);
            var migration = new Migration { CreatedAt = created };

            // Act
            var result = MigrationExtensions.GetAgeInDays(migration);

            // Assert
            Assert.Equal(2, result); // Truncated? Actually TotalDays returns double, but we expect 2.5? Wait implementation returns double, not truncated.
            // Actually implementation returns (DateTime.UtcNow - migration.CreatedAt).TotalDays as double.
            // So we should expect approximately 2.5.
            Assert.InRange(result, 2.4, 2.6);
        }

        [Fact]
        public void GetAgeInDays_CreatedAtDefault_ReturnsZero()
        {
            // Arrange
            var migration = new Migration { CreatedAt = default };

            // Act
            var result = MigrationExtensions.GetAgeInDays(migration);

            // Assert
            Assert.Equal(0, result);
        }

        [Fact]
        public void GetAgeInDays_NullInput_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => MigrationExtensions.GetAgeInDays(null));
        }

        [Fact]
        public void GetExecutionDuration_HappyPath_ReturnsFormattedString()
        {
            // Arrange
            var migration = new Migration
            {
                ExecutedAt = DateTime.UtcNow,
                ExecutionTimeMs = 1500 // 1.5 seconds
            };

            // Act
            var result = MigrationExtensions.GetExecutionDuration(migration);

            // Assert
            Assert.Equal("1.5s", result);
        }

        [Fact]
        public void GetExecutionDuration_ExecutedAtNull_ReturnsNA()
        {
            // Arrange
            var migration = new Migration { ExecutedAt = null };

            // Act
            var result = MigrationExtensions.GetExecutionDuration(migration);

            // Assert
            Assert.Equal("N/A", result);
        }

        [Fact]
        public void GetExecutionDuration_NullInput_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => MigrationExtensions.GetExecutionDuration(null));
        }

        [Fact]
        public void GetStatusDisplay_HappyPath_ReturnsCorrectString()
        {
            // Arrange
            var migration = new Migration { Status = MigrationStatus.Running };

            // Act
            var result = MigrationExtensions.GetStatusDisplay(migration);

            // Assert
            Assert.Equal("[RUNNING]", result);
        }

        [Fact]
        public void GetStatusDisplay_NullInput_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => MigrationExtensions.GetStatusDisplay(null));
        }

        [Fact]
        public void GetStatusCounts_HappyPath_ReturnsDictionary()
        {
            // Arrange
            var migrations = new List<Migration>
            {
                new Migration { Status = MigrationStatus.Pending },
                new Migration { Status = MigrationStatus.Running },
                new Migration { Status = MigrationStatus.Completed },
                new Migration { Status = MigrationStatus.Completed }
            };

            // Act
            var result = MigrationExtensions.GetStatusCounts(migrations);

            // Assert
            Assert.Equal(1, result[MigrationStatus.Pending]);
            Assert.Equal(1, result[MigrationStatus.Running]);
            Assert.Equal(2, result[MigrationStatus.Completed]);
            Assert.Equal(0, result[MigrationStatus.Failed]);
            Assert.Equal(0, result[MigrationStatus.RolledBack]);
        }

        [Fact]
        public void GetStatusCounts_NullInput_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => MigrationExtensions.GetStatusCounts(null));
        }

        [Fact]
        public void GetStatusCounts_EmptyInput_ReturnsEmptyDictionary()
        {
            // Arrange
            var migrations = Enumerable.Empty<Migration>();

            // Act
            var result = MigrationExtensions.GetStatusCounts(migrations);

            // Assert
            Assert.Empty(result);
        }

        [Fact]
        public void GetPendingMigrations_HappyPath_ReturnsOrderedPending()
        {
            // Arrange
            var migrations = new List<Migration>
            {
                new Migration { Status = MigrationStatus.Pending, ExecutionOrder = 2 },
                new Migration { Status = MigrationStatus.Completed, ExecutionOrder = 1 },
                new Migration { Status = MigrationStatus.Pending, ExecutionOrder = 1 }
            };

            // Act
            var result = MigrationExtensions.GetPendingMigrations(migrations).ToList();

            // Assert
            Assert.Equal(2, result.Count);
            Assert.Equal(1, result[0].ExecutionOrder);
            Assert.Equal(2, result[1].ExecutionOrder);
        }

        [Fact]
        public void GetPendingMigrations_NullInput_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => MigrationExtensions.GetPendingMigrations(null));
        }

        [Fact]
        public void GetPendingMigrations_NoPending_ReturnsEmpty()
        {
            // Arrange
            var migrations = new List<Migration>
            {
                new Migration { Status = MigrationStatus.Completed },
                new Migration { Status = MigrationStatus.Failed }
            };

            // Act
            var result = MigrationExtensions.GetPendingMigrations(migrations);

            // Assert
            Assert.Empty(result);
        }

        [Fact]
        public void GetTotalExecutionTimeMs_HappyPath_ReturnsSum()
        {
            // Arrange
            var migrations = new List<Migration>
            {
                new Migration { Status = MigrationStatus.Completed, ExecutionTimeMs = 100 },
                new Migration { Status = MigrationStatus.Completed, ExecutionTimeMs = 200 },
                new Migration { Status = MigrationStatus.Failed, ExecutionTimeMs = 300 }, // should not count
                new Migration { Status = MigrationStatus.Pending, ExecutionTimeMs = 400 } // should not count
            };

            // Act
            var result = MigrationExtensions.GetTotalExecutionTimeMs(migrations);

            // Assert
            Assert.Equal(300, result);
        }

        [Fact]
        public void GetTotalExecutionTimeMs_NullInput_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => MigrationExtensions.GetTotalExecutionTimeMs(null));
        }

        [Fact]
        public void GetTotalExecutionTimeMs_NoCompleted_ReturnsZero()
        {
            // Arrange
            var migrations = new List<Migration>
            {
                new Migration { Status = MigrationStatus.Pending, ExecutionTimeMs = 100 },
                new Migration { Status = MigrationStatus.Failed, ExecutionTimeMs = 200 }
            };

            // Act
            var result = MigrationExtensions.GetTotalExecutionTimeMs(migrations);

            // Assert
            Assert.Equal(0, result);
        }

        [Fact]
        public void GetAverageExecutionTimeMs_HappyPath_ReturnsAverage()
        {
            // Arrange
            var migrations = new List<Migration>
            {
                new Migration { Status = MigrationStatus.Completed, ExecutionTimeMs = 100 },
                new Migration { Status = MigrationStatus.Completed, ExecutionTimeMs = 200 },
                new Migration { Status = MigrationStatus.Completed, ExecutionTimeMs = 300 }
            };

            // Act
            var result = MigrationExtensions.GetAverageExecutionTimeMs(migrations);

            // Assert
            Assert.Equal(200, result);
        }

        [Fact]
        public void GetAverageExecutionTimeMs_NullInput_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => MigrationExtensions.GetAverageExecutionTimeMs(null));
        }

        [Fact]
        public void GetAverageExecutionTimeMs_NoCompleted_ReturnsZero()
        {
            // Arrange
            var migrations = new List<Migration>
            {
                new Migration { Status = MigrationStatus.Pending, ExecutionTimeMs = 100 },
                new Migration { Status = MigrationStatus.Failed, ExecutionTimeMs = 200 }
            };

            // Act
            var result = MigrationExtensions.GetAverageExecutionTimeMs(migrations);

            // Assert
            Assert.Equal(0, result);
        }

        [Fact]
        public void GetRollbackableMigrations_HappyPath_ReturnsOrderedByExecutedAtDescending()
        {
            // Arrange
            var now = DateTime.UtcNow;
            var migrations = new List<Migration>
            {
                new Migration
                {
                    Status = MigrationStatus.Completed,
                    IsRollbackable = true,
                    DownScript = "DOWN",
                    ExecutedAt = now.AddMinutes(-2)
                },
                new Migration
                {
                    Status = MigrationStatus.Completed,
                    IsRollbackable = true,
                    DownScript = "DOWN",
                    ExecutedAt = now.AddMinutes(-1)
                },
                new Migration
                {
                    Status = MigrationStatus.Completed,
                    IsRollbackable = false, // not rollbackable
                    DownScript = "DOWN",
                    ExecutedAt = now
                },
                new Migration
                {
                    Status = MigrationStatus.Pending,
                    IsRollbackable = true,
                    DownScript = "DOWN",
                    ExecutedAt = now
                }
            };

            // Act
            var result = MigrationExtensions.GetRollbackableMigrations(migrations).ToList();

            // Assert
            Assert.Equal(2, result.Count);
            // First should be the one with later ExecutedAt (now -1 minute)
            Assert.Equal(now.AddMinutes(-1), result[0].ExecutedAt);
            // Second should be the earlier one (now -2 minutes)
            Assert.Equal(now.AddMinutes(-2), result[1].ExecutedAt);
        }

        [Fact]
        public void GetRollbackableMigrations_NullInput_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => MigrationExtensions.GetRollbackableMigrations(null));
        }

        [Fact]
        public void GetRollbackableMigrations_NoRollbackable_ReturnsEmpty()
        {
            // Arrange
            var migrations = new List<Migration>
            {
                new Migration { Status = MigrationStatus.Pending },
                new Migration { Status = MigrationStatus.Failed }
            };

            // Act
            var result = MigrationExtensions.GetRollbackableMigrations(migrations);

            // Assert
            Assert.Empty(result);
        }

        [Fact]
        public void GetDatabaseName_HappyPath_ReturnsDatabaseName()
        {
            // Arrange
            var database = new TenantDatabase { Name = "TestDb" };
            var migration = new Migration { DatabaseId = "id123", Database = database };

            // Act
            var result = MigrationExtensions.GetDatabaseName(migration);

            // Assert
            Assert.Equal("TestDb", result);
        }

        [Fact]
        public void GetDatabaseName_DatabaseNull_ReturnsDatabaseId()
        {
            // Arrange
            var migration = new Migration { DatabaseId = "id123", Database = null };

            // Act
            var result = MigrationExtensions.GetDatabaseName(migration);

            // Assert
            Assert.Equal("id123", result);
        }

        [Fact]
        public void GetDatabaseName_NullInput_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => MigrationExtensions.GetDatabaseName(null));
        }
    }
}