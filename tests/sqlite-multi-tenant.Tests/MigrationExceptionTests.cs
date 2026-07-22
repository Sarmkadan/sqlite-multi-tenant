using FluentAssertions;
using SqliteMultiTenant.Exceptions;
using Xunit;

namespace SqliteMultiTenant.Tests;

public class MigrationExceptionTests
{
    [Fact]
    public void Constructor_WithMessage_ShouldSetMessage()
    {
        // Arrange
        var message = "Test migration failed";

        // Act
        var exception = new MigrationException(message);

        // Assert
        exception.Message.Should().Be(message);
        exception.MigrationId.Should().BeNull();
        exception.MigrationVersion.Should().BeNull();
        exception.InnerException.Should().BeNull();
    }

    [Fact]
    public void Constructor_WithMessageAndInnerException_ShouldSetProperties()
    {
        // Arrange
        var message = "Test migration failed";
        var innerException = new InvalidOperationException("Inner error");

        // Act
        var exception = new MigrationException(message, innerException);

        // Assert
        exception.Message.Should().Be(message);
        exception.InnerException.Should().BeSameAs(innerException);
        exception.MigrationId.Should().BeNull();
        exception.MigrationVersion.Should().BeNull();
    }

    [Fact]
    public void Constructor_WithAllParameters_ShouldSetAllProperties()
    {
        // Arrange
        var message = "Migration failed";
        var migrationId = "migration-123";
        var version = "1.0.0";
        var innerException = new Exception("Inner error");

        // Act
        var exception = new MigrationException(message, migrationId, version, innerException);

        // Assert
        exception.Message.Should().Be(message);
        exception.MigrationId.Should().Be(migrationId);
        exception.MigrationVersion.Should().Be(version);
        exception.InnerException.Should().BeSameAs(innerException);
    }

    [Fact]
    public void Constructor_WithNullVersion_ShouldHandleNullVersion()
    {
        // Arrange
        var message = "Migration failed";
        var migrationId = "migration-123";
        string? version = null;
        var innerException = new Exception("Inner error");

        // Act
        var exception = new MigrationException(message, migrationId, version, innerException);

        // Assert
        exception.Message.Should().Be(message);
        exception.MigrationId.Should().Be(migrationId);
        exception.MigrationVersion.Should().BeNull();
        exception.InnerException.Should().BeSameAs(innerException);
    }

    [Fact]
    public void ExecutionFailed_ShouldCreateProperException()
    {
        // Arrange
        var migrationId = "test-migration";
        var version = "2.1.0";
        var innerException = new Exception("Database connection failed");

        // Act
        var exception = MigrationException.ExecutionFailed(migrationId, version, innerException);

        // Assert
        exception.Message.Should().Be($"Migration '{version}' (ID: {migrationId}) failed to execute");
        exception.MigrationId.Should().Be(migrationId);
        exception.MigrationVersion.Should().Be(version);
        exception.InnerException.Should().BeSameAs(innerException);
    }

    [Fact]
    public void ExecutionFailed_WithEmptyMigrationId_ShouldCreateException()
    {
        // Arrange
        var migrationId = string.Empty;
        var version = "1.0.0";
        var innerException = new Exception("Error");

        // Act
        var exception = MigrationException.ExecutionFailed(migrationId, version, innerException);

        // Assert
        exception.Message.Should().Be($"Migration '{version}' (ID: {migrationId}) failed to execute");
        exception.MigrationId.Should().Be(migrationId);
        exception.MigrationVersion.Should().Be(version);
        exception.InnerException.Should().BeSameAs(innerException);
    }

    [Fact]
    public void RollbackFailed_ShouldCreateProperException()
    {
        // Arrange
        var migrationId = "rollback-migration";
        var version = "3.0.0";
        var innerException = new InvalidOperationException("Rollback not allowed");

        // Act
        var exception = MigrationException.RollbackFailed(migrationId, version, innerException);

        // Assert
        exception.Message.Should().Be($"Migration '{version}' (ID: {migrationId}) failed to rollback");
        exception.MigrationId.Should().Be(migrationId);
        exception.MigrationVersion.Should().Be(version);
        exception.InnerException.Should().BeSameAs(innerException);
    }

    [Fact]
    public void RollbackFailed_WithNullInnerException_ShouldCreateException()
    {
        // Arrange
        var migrationId = "rollback-migration";
        var version = "3.0.0";

        // Act
        var exception = MigrationException.RollbackFailed(migrationId, version, null);

        // Assert
        exception.Message.Should().Be($"Migration '{version}' (ID: {migrationId}) failed to rollback");
        exception.MigrationId.Should().Be(migrationId);
        exception.MigrationVersion.Should().Be(version);
        exception.InnerException.Should().BeNull();
    }

    [Fact]
    public void NotFound_ShouldCreateProperException()
    {
        // Arrange
        var migrationId = "missing-migration";

        // Act
        var exception = MigrationException.NotFound(migrationId);

        // Assert
        exception.Message.Should().Be($"Migration with ID '{migrationId}' was not found");
        exception.MigrationId.Should().Be(migrationId);
        exception.MigrationVersion.Should().BeNull();
        exception.InnerException.Should().BeNull();
    }

    [Fact]
    public void NotFound_WithEmptyMigrationId_ShouldCreateException()
    {
        // Arrange
        var migrationId = string.Empty;

        // Act
        var exception = MigrationException.NotFound(migrationId);

        // Assert
        exception.Message.Should().Be($"Migration with ID '{migrationId}' was not found");
        exception.MigrationId.Should().Be(migrationId);
        exception.MigrationVersion.Should().BeNull();
        exception.InnerException.Should().BeNull();
    }

    [Fact]
    public void AlreadyApplied_ShouldCreateProperException()
    {
        // Arrange
        var version = "1.2.3";

        // Act
        var exception = MigrationException.AlreadyApplied(version);

        // Assert
        exception.Message.Should().Be($"Migration '{version}' has already been applied");
        exception.MigrationId.Should().BeNull();
        exception.MigrationVersion.Should().BeNull();
        exception.InnerException.Should().BeNull();
    }

    [Fact]
    public void AlreadyApplied_WithEmptyVersion_ShouldCreateException()
    {
        // Arrange
        var version = string.Empty;

        // Act
        var exception = MigrationException.AlreadyApplied(version);

        // Assert
        exception.Message.Should().Be($"Migration '{version}' has already been applied");
        exception.MigrationId.Should().BeNull();
        exception.MigrationVersion.Should().BeNull();
        exception.InnerException.Should().BeNull();
    }

}