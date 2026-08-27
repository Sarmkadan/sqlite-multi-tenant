using FluentAssertions;
using SqliteMultiTenant.Exceptions;
using Xunit;

namespace SqliteMultiTenant.Tests;

/// <summary>
/// Tests for the <see cref="MigrationExceptionExtensions"/> class.
/// </summary>
public class MigrationExceptionExtensionsTests
{
    /// <summary>
    /// Verifies that <c>IsExecutionFailure</c> returns true when the exception message starts with "Migration execution failed:".
    /// </summary>
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

    /// <summary>
    /// Verifies that <c>IsExecutionFailure</c> returns true when the exception message contains "EXECUTION FAILED:" (case-insensitive).
    /// </summary>
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

    /// <summary>
    /// Verifies that <c>IsExecutionFailure</c> returns false when the exception message does not indicate an execution failure (e.g., "Migration already applied").
    /// </summary>
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

    /// <summary>
    /// Verifies that <c>IsExecutionFailure</c> returns false when the exception message is empty.
    /// </summary>
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

    /// <summary>
    /// Verifies that <c>IsExecutionFailure</c> throws <see cref="ArgumentNullException" /> when the exception is null.
    /// </summary>
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

    /// <summary>
    /// Verifies that <c>IsVersionAlreadyApplied</c> returns true when the exception message starts with "Migration already applied:".
    /// </summary>
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

    /// <summary>
    /// Verifies that <c>IsVersionAlreadyApplied</c> returns true when the exception message contains "ALREADY APPLIED:" (case-insensitive).
    /// </summary>
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

    /// <summary>
    /// Verifies that <c>IsVersionAlreadyApplied</c> returns false when the exception message does not indicate that the migration is already applied (e.g., "Migration failed to execute").
    /// </summary>
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

    /// <summary>
    /// Verifies that <c>IsVersionAlreadyApplied</c> returns false when the exception message is empty.
    /// </summary>
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

    /// <summary>
    /// Verifies that <c>IsVersionAlreadyApplied</c> throws <see cref="ArgumentNullException" /> when the exception is null.
    /// </summary>
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

    /// <summary>
    /// Verifies that <c>GetMigrationDetails</c> returns the formatted string "Migration ID: {id}, Version: {version}" when both migration id and version are provided.
    /// </summary>
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

    /// <summary>
    /// Verifies that <c>GetMigrationDetails</c> returns "Migration ID: null, Version: {version}" when the migration id is null.
    /// </summary>
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

    /// <summary>
    /// Verifies that <c>GetMigrationDetails</c> returns "Migration ID: {id}, Version: null" when the migration version is null.
    /// </summary>
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

    /// <summary>
    /// Verifies that <c>GetMigrationDetails</c> returns "Migration ID: null, Version: null" when both migration id and version are null.
    /// </summary>
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

    /// <summary>
    /// Verifies that <c>GetMigrationDetails</c> returns "Migration ID: , Version: " when both migration id and version are empty strings.
    /// </summary>
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

    /// <summary>
    /// Verifies that <c>GetMigrationDetails</c> throws <see cref="ArgumentNullException" /> when the exception is null.
    /// </summary>
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

    /// <summary>
    /// Verifies that <c>GetMigrationDetails</c> returns the correct migration id and version when called on an exception created by <c>MigrationException.ExecutionFailed</c>.
    /// </summary>
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

    /// <summary>
    /// Verifies that <c>GetMigrationDetails</c> returns "Migration ID: null, Version: null" when called on an exception created by <c>MigrationException.AlreadyApplied</c>.
    /// </summary>
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