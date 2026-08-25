using FluentAssertions;
using SqliteMultiTenant.Exceptions;
using Xunit;

namespace SqliteMultiTenant.Tests;

/// <summary>
/// Contains unit tests for <c>MigrationException</c>, covering its constructors and the
/// static factory methods <c>ExecutionFailed</c>, <c>RollbackFailed</c>, <c>NotFound</c>
/// and <c>AlreadyApplied</c>.
/// </summary>
public class MigrationExceptionTests
{
    /// <summary>
    /// Verifies that creating a <c>MigrationException</c> with only a message sets the message
    /// and leaves the migration ID, migration version and inner exception unset.
    /// </summary>
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

    /// <summary>
    /// Verifies that creating a <c>MigrationException</c> with a message and an inner exception
    /// stores both values while leaving the migration ID and migration version unset.
    /// </summary>
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

    /// <summary>
    /// Verifies that creating a <c>MigrationException</c> with a message, migration ID,
    /// migration version and inner exception populates all four corresponding properties.
    /// </summary>
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

    /// <summary>
    /// Verifies that passing a null migration version to the full constructor preserves the
    /// message, migration ID and inner exception while keeping the migration version null.
    /// </summary>
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

    /// <summary>
    /// Verifies that the <c>ExecutionFailed</c> factory method returns an exception whose
    /// message contains the supplied version and migration ID and whose migration ID,
    /// migration version and inner exception properties match the arguments passed in.
    /// </summary>
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

    /// <summary>
    /// Verifies that the <c>ExecutionFailed</c> factory method accepts an empty migration ID
    /// and still produces an exception with the expected message, an empty migration ID and
    /// the supplied version and inner exception.
    /// </summary>
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

    /// <summary>
    /// Verifies that the <c>RollbackFailed</c> factory method returns an exception whose
    /// message contains the supplied version and migration ID and whose migration ID,
    /// migration version and inner exception properties match the arguments passed in.
    /// </summary>
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

    /// <summary>
    /// Verifies that the <c>RollbackFailed</c> factory method tolerates a null inner
    /// exception, producing an exception with the expected message and matching migration ID
    /// and version while leaving the inner exception null.
    /// </summary>
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

    /// <summary>
    /// Verifies that the <c>NotFound</c> factory method returns an exception whose message
    /// states that the migration with the given ID was not found, with the migration ID set
    /// and no migration version or inner exception.
    /// </summary>
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

    /// <summary>
    /// Verifies that the <c>NotFound</c> factory method accepts an empty migration ID and
    /// still produces an exception with the expected message and an empty migration ID.
    /// </summary>
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

    /// <summary>
    /// Verifies that the <c>AlreadyApplied</c> factory method returns an exception whose
    /// message states that the given version has already been applied, leaving the migration
    /// ID, migration version and inner exception unset.
    /// </summary>
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

    /// <summary>
    /// Verifies that the <c>AlreadyApplied</c> factory method accepts an empty version string
    /// and still produces an exception with the expected message.
    /// </summary>
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
