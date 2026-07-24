#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =====================================================================

using FluentAssertions;
using SqliteMultiTenant.Constants;
using SqliteMultiTenant.Models;
using Xunit;

/// <summary>
/// Tests for BackupExtensions extension methods.
/// </summary>
public sealed class BackupExtensionsTests
{
    /// <summary>
    /// Creates a test Backup instance with default values.
    /// </summary>
    /// <param name="id">The backup ID.</param>
    /// <returns>A new Backup instance.</returns>
    private static Backup CreateBackup(string id = "bkp-001") =>
        new()
        {
            BackupId = id,
            DatabaseId = "db-001",
            BackupPath = $"/data/{id}.sqlite",
            BackupType = BackupType.Full,
            Status = BackupStatus.Completed,
            SizeBytes = 500,
            OriginalSizeBytes = 1000,
            DurationMs = 125000,
            IsVerified = true
        };

    /// <summary>
    /// Tests that GetSavedSpaceBytes returns correct value for normal case.
    /// </summary>
    [Fact]
    public void GetSavedSpaceBytes_WithValidBackup_ReturnsCorrectValue()
    {
        // Arrange
        var backup = CreateBackup("bkp-saved-space");
        backup.OriginalSizeBytes = 10000;
        backup.SizeBytes = 6000;

        // Act
        var result = backup.GetSavedSpaceBytes();

        // Assert
        result.Should().Be(4000);
    }

    /// <summary>
    /// Tests that GetSavedSpaceBytes returns 0 when OriginalSizeBytes is 0.
    /// </summary>
    [Fact]
    public void GetSavedSpaceBytes_WhenOriginalSizeIsZero_ReturnsZero()
    {
        // Arrange
        var backup = CreateBackup("bkp-zero-original");
        backup.OriginalSizeBytes = 0;
        backup.SizeBytes = 100;

        // Act
        var result = backup.GetSavedSpaceBytes();

        // Assert
        result.Should().Be(0);
    }

    /// <summary>
    /// Tests that GetSavedSpaceBytes returns 0 when OriginalSizeBytes is negative.
    /// </summary>
    [Fact]
    public void GetSavedSpaceBytes_WhenOriginalSizeIsNegative_ReturnsZero()
    {
        // Arrange
        var backup = CreateBackup("bkp-negative-original");
        backup.OriginalSizeBytes = -100;
        backup.SizeBytes = 50;

        // Act
        var result = backup.GetSavedSpaceBytes();

        // Assert
        result.Should().Be(0);
    }

    /// <summary>
    /// Tests that GetSavedSpaceBytes returns negative value when SizeBytes is larger than OriginalSizeBytes.
    /// </summary>
    [Fact]
    public void GetSavedSpaceBytes_WhenSizeIsLargerThanOriginal_ReturnsNegativeValue()
    {
        // Arrange
        var backup = CreateBackup("bkp-larger-size");
        backup.OriginalSizeBytes = 1000;
        backup.SizeBytes = 2000;

        // Act
        var result = backup.GetSavedSpaceBytes();

        // Assert
        result.Should().Be(-1000);
    }

    /// <summary>
    /// Tests that GetSavedSpaceBytes throws ArgumentNullException when backup is null.
    /// </summary>
    [Fact]
    public void GetSavedSpaceBytes_WhenBackupIsNull_ThrowsArgumentNullException()
    {
        // Arrange
        Backup? backup = null;

        // Act
        Action act = () => backup.GetSavedSpaceBytes();

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    /// <summary>
    /// Tests that IsFullBackup returns true for Full backup type.
    /// </summary>
    [Fact]
    public void IsFullBackup_WhenBackupTypeIsFull_ReturnsTrue()
    {
        // Arrange
        var backup = CreateBackup("bkp-full");
        backup.BackupType = BackupType.Full;

        // Act
        var result = backup.IsFullBackup();

        // Assert
        result.Should().BeTrue();
    }

    /// <summary>
    /// Tests that IsFullBackup returns false for Incremental backup type.
    /// </summary>
    [Fact]
    public void IsFullBackup_WhenBackupTypeIsIncremental_ReturnsFalse()
    {
        // Arrange
        var backup = CreateBackup("bkp-incremental");
        backup.BackupType = BackupType.Incremental;

        // Act
        var result = backup.IsFullBackup();

        // Assert
        result.Should().BeFalse();
    }

    /// <summary>
    /// Tests that IsFullBackup returns false for Differential backup type.
    /// </summary>
    [Fact]
    public void IsFullBackup_WhenBackupTypeIsDifferential_ReturnsFalse()
    {
        // Arrange
        var backup = CreateBackup("bkp-differential");
        backup.BackupType = BackupType.Differential;

        // Act
        var result = backup.IsFullBackup();

        // Assert
        result.Should().BeFalse();
    }

    /// <summary>
    /// Tests that IsFullBackup throws ArgumentNullException when backup is null.
    /// </summary>
    [Fact]
    public void IsFullBackup_WhenBackupIsNull_ThrowsArgumentNullException()
    {
        // Arrange
        Backup? backup = null;

        // Act
        Action act = () => backup.IsFullBackup();

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    /// <summary>
    /// Tests that IsSystemBackup returns true for verified full backup.
    /// </summary>
    [Fact]
    public void IsSystemBackup_WhenBackupIsVerifiedFullBackup_ReturnsTrue()
    {
        // Arrange
        var backup = CreateBackup("bkp-system");
        backup.BackupType = BackupType.Full;
        backup.IsVerified = true;
        backup.Status = BackupStatus.Verified;

        // Act
        var result = backup.IsSystemBackup();

        // Assert
        result.Should().BeTrue();
    }

    /// <summary>
    /// Tests that IsSystemBackup returns false when backup is not verified.
    /// </summary>
    [Fact]
    public void IsSystemBackup_WhenBackupIsNotVerified_ReturnsFalse()
    {
        // Arrange
        var backup = CreateBackup("bkp-not-verified");
        backup.BackupType = BackupType.Full;
        backup.IsVerified = false;
        backup.Status = BackupStatus.Completed;

        // Act
        var result = backup.IsSystemBackup();

        // Assert
        result.Should().BeFalse();
    }

    /// <summary>
    /// Tests that IsSystemBackup returns false when backup is not a full backup.
    /// </summary>
    [Fact]
    public void IsSystemBackup_WhenBackupIsNotFullType_ReturnsFalse()
    {
        // Arrange
        var backup = CreateBackup("bkp-not-full");
        backup.BackupType = BackupType.Incremental;
        backup.IsVerified = true;
        backup.Status = BackupStatus.Verified;

        // Act
        var result = backup.IsSystemBackup();

        // Assert
        result.Should().BeFalse();
    }

    /// <summary>
    /// Tests that IsSystemBackup throws ArgumentNullException when backup is null.
    /// </summary>
    [Fact]
    public void IsSystemBackup_WhenBackupIsNull_ThrowsArgumentNullException()
    {
        // Arrange
        Backup? backup = null;

        // Act
        Action act = () => backup.IsSystemBackup();

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    /// <summary>
    /// Tests that GetHumanReadableSize returns correct format for bytes.
    /// </summary>
    [Fact]
    public void GetHumanReadableSize_WhenSizeIsInBytes_ReturnsCorrectFormat()
    {
        // Arrange
        var backup = CreateBackup("bkp-bytes");
        backup.SizeBytes = 500;

        // Act
        var result = backup.GetHumanReadableSize();

        // Assert
        result.Should().Be("500 B");
    }

    /// <summary>
    /// Tests that GetHumanReadableSize returns correct format for kilobytes.
    /// </summary>
    [Fact]
    public void GetHumanReadableSize_WhenSizeIsInKilobytes_ReturnsCorrectFormat()
    {
        // Arrange
        var backup = CreateBackup("bkp-kb");
        backup.SizeBytes = 1536; // 1.5 KB

        // Act
        var result = backup.GetHumanReadableSize();

        // Assert
        result.Should().Be("1.5 KB");
    }

    /// <summary>
    /// Tests that GetHumanReadableSize returns correct format for megabytes.
    /// </summary>
    [Fact]
    public void GetHumanReadableSize_WhenSizeIsInMegabytes_ReturnsCorrectFormat()
    {
        // Arrange
        var backup = CreateBackup("bkp-mb");
        backup.SizeBytes = 2500000; // 2.5 MB (approximately 2.38 MB with floating point division)

        // Act
        var result = backup.GetHumanReadableSize();

        // Assert
        result.Should().Be("2.38 MB");
    }

    /// <summary>
    /// Tests that GetHumanReadableSize returns correct format for gigabytes.
    /// </summary>
    [Fact]
    public void GetHumanReadableSize_WhenSizeIsInGigabytes_ReturnsCorrectFormat()
    {
        // Arrange
        var backup = CreateBackup("bkp-gb");
        backup.SizeBytes = 3221225472; // 3 GB

        // Act
        var result = backup.GetHumanReadableSize();

        // Assert
        result.Should().Be("3 GB");
    }

    /// <summary>
    /// Tests that GetHumanReadableSize throws ArgumentNullException when backup is null.
    /// </summary>
    [Fact]
    public void GetHumanReadableSize_WhenBackupIsNull_ThrowsArgumentNullException()
    {
        // Arrange
        Backup? backup = null;

        // Act
        Action act = () => backup.GetHumanReadableSize();

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    /// <summary>
    /// Tests that GetHumanReadableDuration returns seconds for small durations.
    /// </summary>
    [Fact]
    public void GetHumanReadableDuration_WhenDurationIsLessThan60Seconds_ReturnsSeconds()
    {
        // Arrange
        var backup = CreateBackup("bkp-seconds");
        backup.DurationMs = 30000; // 30 seconds

        // Act
        var result = backup.GetHumanReadableDuration();

        // Assert
        result.Should().Be("30s");
    }

    /// <summary>
    /// Tests that GetHumanReadableDuration returns minutes only when seconds is 0.
    /// </summary>
    [Fact]
    public void GetHumanReadableDuration_WhenDurationIsWholeMinutes_ReturnsMinutesOnly()
    {
        // Arrange
        var backup = CreateBackup("bkp-minutes");
        backup.DurationMs = 120000; // 2 minutes

        // Act
        var result = backup.GetHumanReadableDuration();

        // Assert
        result.Should().Be("2m");
    }

    /// <summary>
    /// Tests that GetHumanReadableDuration returns minutes and seconds when both are present.
    /// </summary>
    [Fact]
    public void GetHumanReadableDuration_WhenDurationHasMinutesAndSeconds_ReturnsBoth()
    {
        // Arrange
        var backup = CreateBackup("bkp-min-sec");
        backup.DurationMs = 150000; // 2 minutes 30 seconds

        // Act
        var result = backup.GetHumanReadableDuration();

        // Assert
        result.Should().Be("2m 30s");
    }

    /// <summary>
    /// Tests that GetHumanReadableDuration returns hours only when minutes is 0.
    /// </summary>
    [Fact]
    public void GetHumanReadableDuration_WhenDurationIsWholeHours_ReturnsHoursOnly()
    {
        // Arrange
        var backup = CreateBackup("bkp-hours");
        backup.DurationMs = 7200000; // 2 hours

        // Act
        var result = backup.GetHumanReadableDuration();

        // Assert
        result.Should().Be("2h");
    }

    /// <summary>
    /// Tests that GetHumanReadableDuration returns hours and minutes when both are present.
    /// </summary>
    [Fact]
    public void GetHumanReadableDuration_WhenDurationHasHoursAndMinutes_ReturnsBoth()
    {
        // Arrange
        var backup = CreateBackup("bkp-hour-min");
        backup.DurationMs = 7500000; // 2 hours 5 minutes

        // Act
        var result = backup.GetHumanReadableDuration();

        // Assert
        result.Should().Be("2h 5m");
    }

    /// <summary>
    /// Tests that GetHumanReadableDuration throws ArgumentNullException when backup is null.
    /// </summary>
    [Fact]
    public void GetHumanReadableDuration_WhenBackupIsNull_ThrowsArgumentNullException()
    {
        // Arrange
        Backup? backup = null;

        // Act
        Action act = () => backup.GetHumanReadableDuration();

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }
}