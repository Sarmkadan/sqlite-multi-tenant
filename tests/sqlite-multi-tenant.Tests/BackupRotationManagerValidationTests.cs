#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;
using SqliteMultiTenant.BackgroundWorkers;
using Xunit;

namespace SqliteMultiTenant.Tests;

/// <summary>
/// Tests for the <see cref="BackupRotationManagerValidation"/> class.
/// </summary>
public sealed class BackupRotationManagerValidationTests
{
    private const long OneHundredTerabytes = 100L * 1024 * 1024 * 1024 * 1024;

    [Fact]
    public void Validate_PolicyAtMaximumBoundaries_ReturnsNoErrors()
    {
        // Arrange
        var policy = new BackupRotationPolicy
        {
            MaxBackupAge = TimeSpan.FromDays(365 * 5),
            MaxBackupCount = 1000,
            MaxDiskUsage = OneHundredTerabytes
        };

        // Act
        var errors = BackupRotationManagerValidation.Validate(policy);

        // Assert
        Assert.Empty(errors);
        Assert.True(BackupRotationManagerValidation.IsValid(policy));
    }

    [Fact]
    public void Validate_PolicyJustOutsideBounds_ReportsEachInvalidField()
    {
        // Arrange
        var belowMinimum = new BackupRotationPolicy { MaxBackupAge = TimeSpan.Zero, MaxBackupCount = 0, MaxDiskUsage = 0 };
        var aboveMaximum = new BackupRotationPolicy
        {
            MaxBackupAge = TimeSpan.FromDays(365 * 5 + 1),
            MaxBackupCount = 1001,
            MaxDiskUsage = OneHundredTerabytes + 1
        };

        // Act
        var lowErrors = BackupRotationManagerValidation.Validate(belowMinimum);
        var highErrors = BackupRotationManagerValidation.Validate(aboveMaximum);

        // Assert
        Assert.Equal(3, lowErrors.Count);
        Assert.Contains(lowErrors, e => e.StartsWith("MaxBackupAge must be greater than zero", StringComparison.Ordinal));
        Assert.Contains(lowErrors, e => e.StartsWith("MaxBackupCount must be greater than zero", StringComparison.Ordinal));
        Assert.Contains(lowErrors, e => e.StartsWith("MaxDiskUsage must be greater than zero", StringComparison.Ordinal));

        Assert.Equal(3, highErrors.Count);
        Assert.Contains(highErrors, e => e.Contains("exceeds maximum allowed value of 5 years", StringComparison.Ordinal));
        Assert.Contains(highErrors, e => e.Contains("exceeds maximum allowed value of 1000", StringComparison.Ordinal));
        Assert.Contains(highErrors, e => e.Contains("exceeds maximum allowed value of 100 TB", StringComparison.Ordinal));
    }

    [Fact]
    public void Validate_NullInput_ThrowsArgumentNullException()
    {
        // Arrange
        BackupRotationPolicy? policy = null;
        BackupRotationResult? result = null;
        BackupVerificationResult? verification = null;
        BackupStatistics? statistics = null;

        // Act & Assert
        Action validatePolicy = () => BackupRotationManagerValidation.Validate(policy);
        Action validateResult = () => BackupRotationManagerValidation.Validate(result);
        Action validateVerification = () => BackupRotationManagerValidation.Validate(verification);
        Action validateStatistics = () => BackupRotationManagerValidation.Validate(statistics);
        Action ensurePolicy = () => BackupRotationManagerValidation.EnsureValid(policy);

        Assert.Throws<ArgumentNullException>(validatePolicy);
        Assert.Throws<ArgumentNullException>(validateResult);
        Assert.Throws<ArgumentNullException>(validateVerification);
        Assert.Throws<ArgumentNullException>(validateStatistics);
        Assert.Throws<ArgumentNullException>(ensurePolicy);
    }

    [Fact]
    public void Validate_ResultMarkedSuccessfulWithError_ReportsBothProblems()
    {
        // Arrange
        var result = new BackupRotationResult
        {
            IsSuccessful = true,
            ExecutedAt = DateTime.UtcNow.AddMinutes(-1),
            Error = "first line\nsecond line"
        };

        // Act
        var errors = BackupRotationManagerValidation.Validate(result);

        // Assert
        Assert.Equal(2, errors.Count);
        Assert.Contains(errors, e => e.Contains("IsSuccessful must be false", StringComparison.Ordinal));
        Assert.Contains(errors, e => e.Contains("line breaks", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData("   ", "FilePath cannot be null or whitespace.")]
    [InlineData("relative/tenant.db", "FilePath must be a valid absolute path or URI")]
    public void Validate_VerificationResultWithUnusablePath_ReportsSingleFilePathError(string filePath, string expectedError)
    {
        // Arrange
        var result = new BackupVerificationResult
        {
            FilePath = filePath,
            FileName = "tenant.db",
            IsValid = true,
            IsReadable = true,
            CreatedAt = DateTime.UtcNow.AddHours(-1),
            LastModified = DateTime.UtcNow.AddHours(-1)
        };

        // Act
        var errors = BackupRotationManagerValidation.Validate(result);

        // Assert
        var error = Assert.Single(errors);
        Assert.StartsWith(expectedError, error, StringComparison.Ordinal);
    }

    [Fact]
    public void Validate_StatisticsWithReversedDateRange_ReportsSingleError()
    {
        // Arrange
        var now = DateTime.UtcNow;
        var statistics = new BackupStatistics
        {
            TotalBackups = 2,
            TotalDiskUsage = 2048,
            AverageBackupSize = 1024,
            OldestBackup = now.AddDays(-1),
            NewestBackup = now.AddDays(-2)
        };

        // Act
        var errors = BackupRotationManagerValidation.Validate(statistics);

        // Assert
        Assert.Equal("NewestBackup cannot be older than OldestBackup.", Assert.Single(errors));
    }

    [Fact]
    public void IsValid_NullOrInvalidInput_ReturnsFalse()
    {
        // Arrange
        BackupRotationPolicy? nullPolicy = null;
        BackupStatistics? nullStatistics = null;
        var invalidPolicy = new BackupRotationPolicy { MaxBackupCount = 0 };

        // Act & Assert
        Assert.False(BackupRotationManagerValidation.IsValid(nullPolicy));
        Assert.False(BackupRotationManagerValidation.IsValid(nullStatistics));
        Assert.False(BackupRotationManagerValidation.IsValid(invalidPolicy));
    }

    [Fact]
    public void EnsureValid_InvalidPolicy_ThrowsArgumentExceptionListingErrors()
    {
        // Arrange
        var invalidPolicy = new BackupRotationPolicy { MaxBackupCount = 0 };
        Action invalid = () => BackupRotationManagerValidation.EnsureValid(invalidPolicy);

        // Act
        var exception = Assert.Throws<ArgumentException>(invalid);

        // Assert
        Assert.StartsWith("BackupRotationPolicy is invalid:", exception.Message, StringComparison.Ordinal);
        Assert.Contains("MaxBackupCount must be greater than zero", exception.Message, StringComparison.Ordinal);
    }
}
