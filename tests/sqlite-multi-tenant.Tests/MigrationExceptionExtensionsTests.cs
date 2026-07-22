using FluentAssertions;
using SqliteMultiTenant.Exceptions;
using Xunit;

namespace SqliteMultiTenant.Tests;

public class MigrationExceptionExtensionsTests
{
    [Fact]
    public void IsExecutionFailure_WithExecutionFailedMessage_ShouldReturnTrue()
    {
        // Arrange
        var exception = new MigrationException("Migration execution failed: test migration");

        // Act
        var result = exception.IsExecutionFailure();

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsExecutionFailure_WithCaseInsensitiveExecutionFailedMessage_ShouldReturnTrue()
    {
        // Arrange
        var exception = new MigrationException("EXECUTION FAILED: Migration failed to execute");

        // Act
        var result = exception.IsExecutionFailure();

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsExecutionFailure_WithNonExecutionFailedMessage_ShouldReturnFalse()
    {
        // Arrange
        var exception = new MigrationException("Migration already applied");

        // Act
        var result = exception.IsExecutionFailure();

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsExecutionFailure_WithEmptyMessage_ShouldReturnFalse()
    {
        // Arrange
        var exception = new MigrationException(string.Empty);

        // Act
        var result = exception.IsExecutionFailure();

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsExecutionFailure_WithNullException_ShouldThrowArgumentNullException()
    {
        // Arrange
        MigrationException? exception = null;

        // Act
        Action act = () => exception!.IsExecutionFailure();

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void IsVersionAlreadyApplied_WithAlreadyAppliedMessage_ShouldReturnTrue()
    {
        // Arrange
        var exception = new MigrationException("Migration already applied: version 1.2.3");

        // Act
        var result = exception.IsVersionAlreadyApplied();

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsVersionAlreadyApplied_WithCaseInsensitiveAlreadyAppliedMessage_ShouldReturnTrue()
    {
        // Arrange
        var exception = new MigrationException("ALREADY APPLIED: Migration '1.0.0' was already applied");

        // Act
        var result = exception.IsVersionAlreadyApplied();

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsVersionAlreadyApplied_WithNonAlreadyAppliedMessage_ShouldReturnFalse()
    {
        // Arrange
        var exception = new MigrationException("Migration failed to execute");

        // Act
        var result = exception.IsVersionAlreadyApplied();

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsVersionAlreadyApplied_WithEmptyMessage_ShouldReturnFalse()
    {
        // Arrange
        var exception = new MigrationException(string.Empty);

        // Act
        var result = exception.IsVersionAlreadyApplied();

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsVersionAlreadyApplied_WithNullException_ShouldThrowArgumentNullException()
    {
        // Arrange
        MigrationException? exception = null;

        // Act
        Action act = () => exception!.IsVersionAlreadyApplied();

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void GetMigrationDetails_WithNonNullMigrationIdAndVersion_ShouldReturnFormattedString()
    {
        // Arrange
        var exception = new MigrationException("Test message", "migration-123", "2.1.0");

        // Act
        var result = exception.GetMigrationDetails();

        // Assert
        result.Should().Be("Migration ID: migration-123, Version: 2.1.0");
    }

    [Fact]
    public void GetMigrationDetails_WithNullMigrationId_ShouldReturnNullForId()
    {
        // Arrange
        var exception = new MigrationException("Test message", null, "2.1.0");

        // Act
        var result = exception.GetMigrationDetails();

        // Assert
        result.Should().Be("Migration ID: null, Version: 2.1.0");
    }

    [Fact]
    public void GetMigrationDetails_WithNullMigrationVersion_ShouldReturnNullForVersion()
    {
        // Arrange
        var exception = new MigrationException("Test message", "migration-123", null);

        // Act
        var result = exception.GetMigrationDetails();

        // Assert
        result.Should().Be("Migration ID: migration-123, Version: null");
    }

    [Fact]
    public void GetMigrationDetails_WithBothNullMigrationIdAndVersion_ShouldReturnNullForBoth()
    {
        // Arrange
        var exception = new MigrationException("Test message", null, null);

        // Act
        var result = exception.GetMigrationDetails();

        // Assert
        result.Should().Be("Migration ID: null, Version: null");
    }

    [Fact]
    public void GetMigrationDetails_WithEmptyMigrationIdAndVersion_ShouldReturnEmptyStrings()
    {
        // Arrange
        var exception = new MigrationException("Test message", string.Empty, string.Empty);

        // Act
        var result = exception.GetMigrationDetails();

        // Assert
        result.Should().Be("Migration ID: , Version: ");
    }

    [Fact]
    public void GetMigrationDetails_WithNullException_ShouldThrowArgumentNullException()
    {
        // Arrange
        MigrationException? exception = null;

        // Act
        Action act = () => exception!.GetMigrationDetails();

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void GetMigrationDetails_FromExecutionFailedException_ShouldReturnCorrectDetails()
    {
        // Arrange
        var innerException = new Exception("Database error");
        var migrationException = MigrationException.ExecutionFailed("exec-mig-456", "3.0.0", innerException);

        // Act
        var result = migrationException.GetMigrationDetails();

        // Assert
        result.Should().Be("Migration ID: exec-mig-456, Version: 3.0.0");
    }

    [Fact]
    public void GetMigrationDetails_FromAlreadyAppliedException_ShouldReturnNullForIdAndVersion()
    {
        // Arrange
        var exception = MigrationException.AlreadyApplied("1.2.3");

        // Act
        var result = exception.GetMigrationDetails();

        // Assert
        result.Should().Be("Migration ID: null, Version: null");
    }
}
