#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using FluentAssertions;
using SqliteMultiTenant.Constants;
using SqliteMultiTenant.Models;
using Xunit;

/// <summary>
/// Tests for the Migration class.
/// </summary>
public sealed class MigrationTests
{
    /// <summary>
    /// Creates a new Migration instance with default values.
    /// </summary>
    /// <param name="migrationId">The migration ID.</param>
    /// <returns>A new Migration instance.</returns>
    private static Migration CreateMigration(string migrationId = "mgr-001") =>
        new()
        {
            MigrationId = migrationId,
            DatabaseId = "db-001",
            Version = "1.0.0",
            Name = "InitialMigration",
            Description = "Initial database setup",
            UpScript = "CREATE TABLE TestTable (Id INTEGER PRIMARY KEY);",
            DownScript = "DROP TABLE TestTable;",
            Status = MigrationStatus.Pending,
            CreatedAt = DateTime.UtcNow,
            ExecutedAt = null,
            CompletedAt = null,
            RolledBackAt = null,
            ExecutedBy = null,
            ErrorMessage = null,
            ExecutionTimeMs = 0,
            ExecutionOrder = 1,
            IsRollbackable = true
        };

    /// <summary>
    /// Tests that all public properties are initialized with default values.
    /// </summary>
    [Fact]
    public void Constructor_WithDefaultValues_InitializesPropertiesCorrectly()
    {
        // Arrange & Act
        var migration = new Migration();

        // Assert
        migration.MigrationId.Should().BeEmpty();
        migration.DatabaseId.Should().BeEmpty();
        migration.Version.Should().BeEmpty();
        migration.Name.Should().BeEmpty();
        migration.Description.Should().BeNull();
        migration.UpScript.Should().BeEmpty();
        migration.DownScript.Should().BeNull();
        migration.Status.Should().Be(MigrationStatus.Pending);
        migration.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        migration.ExecutedAt.Should().BeNull();
        migration.CompletedAt.Should().BeNull();
        migration.RolledBackAt.Should().BeNull();
        migration.ExecutedBy.Should().BeNull();
        migration.ErrorMessage.Should().BeNull();
        migration.ExecutionTimeMs.Should().Be(0);
        migration.ExecutionOrder.Should().Be(0);
        migration.IsRollbackable.Should().BeTrue();
    }

    /// <summary>
    /// Tests that Validate returns true for a valid migration.
    /// </summary>
    [Fact]
    public void Validate_WithValidMigration_ReturnsTrueAndNoErrors()
    {
        // Arrange
        var migration = CreateMigration();

        // Act
        var isValid = migration.Validate(out var errors);

        // Assert
        isValid.Should().BeTrue();
        errors.Should().BeEmpty();
    }

    /// <summary>
    /// Tests that Validate returns false and lists errors for a migration with null/empty required fields.
    /// </summary>
    [Fact]
    public void Validate_WithInvalidMigration_ReturnsFalseAndListsErrors()
    {
        // Arrange
        var migration = new Migration
        {
            MigrationId = "",
            DatabaseId = "",
            Version = "",
            Name = "",
            UpScript = ""
        };

        // Act
        var isValid = migration.Validate(out var errors);

        // Assert
        isValid.Should().BeFalse();
        errors.Should().HaveCount(5);
        errors.Should().Contain("MigrationId is required");
        errors.Should().Contain("DatabaseId is required");
        errors.Should().Contain("Version is required");
        errors.Should().Contain("Name is required");
        errors.Should().Contain("UpScript is required");
    }


    /// <summary>
    /// Tests that MarkAsStarted sets the correct status and timestamps.
    /// </summary>
    [Fact]
    public void MarkAsStarted_SetsStatusAndTimestamps()
    {
        // Arrange
        var migration = CreateMigration();
        var executedBy = "test-user";

        // Act
        migration.MarkAsStarted(executedBy);

        // Assert
        migration.Status.Should().Be(MigrationStatus.Running);
        migration.ExecutedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        migration.ExecutedBy.Should().Be(executedBy);
        migration.CompletedAt.Should().BeNull();
        migration.ErrorMessage.Should().BeNull();
    }

    /// <summary>
    /// Tests that MarkAsCompleted sets the correct status and clears error message.
    /// </summary>
    [Fact]
    public void MarkAsCompleted_SetsStatusAndClearsError()
    {
        // Arrange
        var migration = CreateMigration();
        migration.MarkAsStarted("test-user");
        migration.ErrorMessage = "Previous error";

        // Act
        migration.MarkAsCompleted(250);

        // Assert
        migration.Status.Should().Be(MigrationStatus.Completed);
        migration.CompletedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        migration.ExecutionTimeMs.Should().Be(250);
        migration.ErrorMessage.Should().BeNull();
    }

    /// <summary>
    /// Tests that MarkAsFailed sets the correct status and preserves error message.
    /// </summary>
    [Fact]
    public void MarkAsFailed_SetsFailedStatusAndPreservesError()
    {
        // Arrange
        var migration = CreateMigration();
        var errorMessage = "Database connection failed";

        // Act
        migration.MarkAsFailed(errorMessage);

        // Assert
        migration.Status.Should().Be(MigrationStatus.Failed);
        migration.ErrorMessage.Should().Be(errorMessage);
        migration.CompletedAt.Should().BeNull();
    }

    /// <summary>
    /// Tests that MarkAsRolledBack sets the correct status and timestamps.
    /// </summary>
    [Fact]
    public void MarkAsRolledBack_SetsStatusAndTimestamps()
    {
        // Arrange
        var migration = CreateMigration();
        migration.MarkAsCompleted(200);

        // Act
        migration.MarkAsRolledBack(150);

        // Assert
        migration.Status.Should().Be(MigrationStatus.RolledBack);
        migration.RolledBackAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        migration.ExecutionTimeMs.Should().Be(150);
        migration.ErrorMessage.Should().BeNull();
    }

    /// <summary>
    /// Tests that CanRollback returns true when all conditions are met.
    /// </summary>
    [Fact]
    public void CanRollback_WhenAllConditionsMet_ReturnsTrue()
    {
        // Arrange
        var migration = CreateMigration();
        migration.DownScript = "DROP TABLE Test;";
        migration.Status = MigrationStatus.Completed;
        migration.IsRollbackable = true;

        // Act
        var canRollback = migration.CanRollback();

        // Assert
        canRollback.Should().BeTrue();
    }


    /// <summary>
    /// Tests that GetDisplayName returns the correct format.
    /// </summary>
    [Fact]
    public void GetDisplayName_ReturnsCorrectFormat()
    {
        // Arrange
        var migration = CreateMigration();
        migration.Version = "1.2.3";
        migration.Name = "AddUserTable";

        // Act
        var displayName = migration.GetDisplayName();

        // Assert
        displayName.Should().Be("1.2.3_AddUserTable");
    }
}