using FluentAssertions;
using SqliteMultiTenant.Exceptions;
using Xunit;

namespace SqliteMultiTenant.Tests;

public class BackupExceptionExtensionsTests
{
    [Fact]
    public void IsCreationFailure_WithCreationMessage_ShouldReturnTrue()
    {
        // Arrange
        var exception = new BackupException("Backup creation failed for database 'db-123'", "backup-1", "db-123");

        // Act
        var result = exception.IsCreationFailure();

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsCreationFailure_WithNonCreationMessage_ShouldReturnFalse()
    {
        // Arrange
        var exception = new BackupException("Backup verification failed for backup 'backup-1'");

        // Act
        var result = exception.IsCreationFailure();

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsCreationFailure_WithCaseInsensitiveCreationMessage_ShouldReturnTrue()
    {
        // Arrange
        var exception = new BackupException("CREATION failed to create backup");

        // Act
        var result = exception.IsCreationFailure();

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsCreationFailure_WithNullException_ShouldThrowArgumentNullException()
    {
        // Arrange
        BackupException? exception = null;

        // Act
        Action act = () => exception!.IsCreationFailure();

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void IsVerificationFailure_WithVerificationMessage_ShouldReturnTrue()
    {
        // Arrange
        var exception = BackupException.VerificationFailed("backup-456", "db-123");

        // Act
        var result = exception.IsVerificationFailure();

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsVerificationFailure_WithNonVerificationMessage_ShouldReturnFalse()
    {
        // Arrange
        var exception = new BackupException("Backup creation failed");

        // Act
        var result = exception.IsVerificationFailure();

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsVerificationFailure_WithCaseInsensitiveVerificationMessage_ShouldReturnTrue()
    {
        // Arrange
        var exception = new BackupException("VERIFICATION of backup failed");

        // Act
        var result = exception.IsVerificationFailure();

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsVerificationFailure_WithNullException_ShouldThrowArgumentNullException()
    {
        // Arrange
        BackupException? exception = null;

        // Act
        Action act = () => exception!.IsVerificationFailure();

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void IsRestoreFailure_WithRestoreMessage_ShouldReturnTrue()
    {
        // Arrange
        var exception = BackupException.RestoreFailed("backup-789", "db-456", new Exception("Invalid format"));

        // Act
        var result = exception.IsRestoreFailure();

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsRestoreFailure_WithNonRestoreMessage_ShouldReturnFalse()
    {
        // Arrange
        var exception = new BackupException("Backup creation failed");

        // Act
        var result = exception.IsRestoreFailure();

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsRestoreFailure_WithCaseInsensitiveRestoreMessage_ShouldReturnTrue()
    {
        // Arrange
        var exception = new BackupException("failed to RESTORE backup");

        // Act
        var result = exception.IsRestoreFailure();

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsRestoreFailure_WithNullException_ShouldThrowArgumentNullException()
    {
        // Arrange
        BackupException? exception = null;

        // Act
        Action act = () => exception!.IsRestoreFailure();

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void GetErrorDetails_WithValidException_ShouldReturnFormattedString()
    {
        // Arrange
        var exception = new BackupException("Test error message", "backup-123", "db-456");

        // Act
        var result = exception.GetErrorDetails();

        // Assert
        result.Should().Be("BackupId: backup-123, DatabaseId: db-456, Message: Test error message");
    }

    [Fact]
    public void GetErrorDetails_WithNullBackupId_ShouldIncludeNullInOutput()
    {
        // Arrange
        var exception = new BackupException("Test error", null, "db-456");

        // Act
        var result = exception.GetErrorDetails();

        // Assert
        result.Should().Be("BackupId: , DatabaseId: db-456, Message: Test error");
    }

    [Fact]
    public void GetErrorDetails_WithNullDatabaseId_ShouldIncludeNullInOutput()
    {
        // Arrange
        var exception = new BackupException("Test error", "backup-123", null);

        // Act
        var result = exception.GetErrorDetails();

        // Assert
        result.Should().Be("BackupId: backup-123, DatabaseId: , Message: Test error");
    }

    [Fact]
    public void GetErrorDetails_WithEmptyStrings_ShouldReturnFormattedString()
    {
        // Arrange
        var exception = new BackupException(string.Empty, string.Empty, string.Empty);

        // Act
        var result = exception.GetErrorDetails();

        // Assert
        result.Should().Be("BackupId: , DatabaseId: , Message: ");
    }

    [Fact]
    public void GetErrorDetails_WithNullException_ShouldThrowArgumentNullException()
    {
        // Arrange
        BackupException? exception = null;

        // Act
        Action act = () => exception!.GetErrorDetails();

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void IsCreationFailure_WithEmptyMessage_ShouldReturnFalse()
    {
        // Arrange
        var exception = new BackupException(string.Empty);

        // Act
        var result = exception.IsCreationFailure();

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsVerificationFailure_WithEmptyMessage_ShouldReturnFalse()
    {
        // Arrange
        var exception = new BackupException(string.Empty);

        // Act
        var result = exception.IsVerificationFailure();

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsRestoreFailure_WithEmptyMessage_ShouldReturnFalse()
    {
        // Arrange
        var exception = new BackupException(string.Empty);

        // Act
        var result = exception.IsRestoreFailure();

        // Assert
        result.Should().BeFalse();
    }
}
