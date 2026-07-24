#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using SqliteMultiTenant.Logging;
using System.Collections.Generic;
using Xunit;

namespace SqliteMultiTenant.Tests.Logging;

/// <summary>
/// Tests for the LoggingExtensions class.
/// </summary>
public sealed class LoggingExtensionsTests
{
    private readonly ILogger _logger = Substitute.For<ILogger>();

    #region LogTenantOperation Tests

    [Fact]
    public void LogTenantOperation_WithSuccessResult_DoesNotThrow()
    {
        // Act - just verify it doesn't throw
        var act = () => _logger.LogTenantOperation("CreateTenant", "tenant-123", "success", 150);

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void LogTenantOperation_WithFailureResult_LogsAtWarningLevel()
    {
        // Act
        _logger.LogTenantOperation("DeleteTenant", "tenant-456", "failed", null);

        // Assert
        _logger.Received(1).Log(
            LogLevel.Warning,
            Arg.Any<EventId>(),
            Arg.Any<object[]>(),
            null,
            Arg.Any<Func<object, Exception?, string>>());
    }

    [Fact]
    public void LogTenantOperation_WithNullLogger_ThrowsArgumentNullException()
    {
        // Act
        Action act = () => LoggingExtensions.LogTenantOperation(null!, "operation", "tenant-id", "success");

        // Assert
        act.Should().Throw<ArgumentNullException>().WithParameterName("logger");
    }

    [Fact]
    public void LogTenantOperation_WithNullOperation_ThrowsArgumentNullException()
    {
        // Act
        Action act = () => _logger.LogTenantOperation(null!, "tenant-id", "success");

        // Assert
        act.Should().Throw<ArgumentNullException>().WithParameterName("operation");
    }

    [Fact]
    public void LogTenantOperation_WithNullTenantId_ThrowsArgumentNullException()
    {
        // Act
        Action act = () => _logger.LogTenantOperation("operation", null!, "success");

        // Assert
        act.Should().Throw<ArgumentNullException>().WithParameterName("tenantId");
    }

    [Fact]
    public void LogTenantOperation_WithNullResult_ThrowsArgumentNullException()
    {
        // Act
        Action act = () => _logger.LogTenantOperation("operation", "tenant-id", null!);

        // Assert
        act.Should().Throw<ArgumentNullException>().WithParameterName("result");
    }

    #endregion

    #region LogDatabaseOperation Tests

    [Fact]
    public void LogDatabaseOperation_WithFastSuccessfulQuery_LogsAtDebugLevel()
    {
        // Act
        _logger.LogDatabaseOperation("SELECT", "db-main", 50, true);

        // Assert
        _logger.Received(1).Log(
            LogLevel.Debug,
            Arg.Any<EventId>(),
            Arg.Any<object[]>(),
            null,
            Arg.Any<Func<object, Exception?, string>>());
    }

    [Fact]
    public void LogDatabaseOperation_WithSlowQuery_LogsAtWarningLevel()
    {
        // Act
        _logger.LogDatabaseOperation("INSERT", "db-log", 6000, true);

        // Assert
        _logger.Received(1).Log(
            LogLevel.Warning,
            Arg.Any<EventId>(),
            Arg.Any<object[]>(),
            null,
            Arg.Any<Func<object, Exception?, string>>());
    }

    [Fact]
    public void LogDatabaseOperation_WithFailedQuery_LogsAtErrorLevel()
    {
        // Act
        _logger.LogDatabaseOperation("UPDATE", "db-cache", 100, false);

        // Assert
        _logger.Received(1).Log(
            LogLevel.Error,
            Arg.Any<EventId>(),
            Arg.Any<object[]>(),
            null,
            Arg.Any<Func<object, Exception?, string>>());
    }

    [Fact]
    public void LogDatabaseOperation_WithNullLogger_ThrowsArgumentNullException()
    {
        // Act
        Action act = () => LoggingExtensions.LogDatabaseOperation(null!, "operation", "db-id", 100);

        // Assert
        act.Should().Throw<ArgumentNullException>().WithParameterName("logger");
    }

    [Fact]
    public void LogDatabaseOperation_WithNullOperation_ThrowsArgumentNullException()
    {
        // Act
        Action act = () => _logger.LogDatabaseOperation(null!, "db-id", 100);

        // Assert
        act.Should().Throw<ArgumentNullException>().WithParameterName("operation");
    }

    [Fact]
    public void LogDatabaseOperation_WithNullOrEmptyDatabaseId_ThrowsArgumentException()
    {
        // Act
        Action actNull = () => _logger.LogDatabaseOperation("operation", null!, 100);
        Action actEmpty = () => _logger.LogDatabaseOperation("operation", "", 100);

        // Assert
        actNull.Should().Throw<ArgumentException>().WithParameterName("databaseId");
        actEmpty.Should().Throw<ArgumentException>().WithParameterName("databaseId");
    }

    #endregion

    #region LogBackupOperation Tests

    [Fact]
    public void LogBackupOperation_WithSuccessfulBackup_LogsAtInformationLevel()
    {
        // Act
        _logger.LogBackupOperation("Create", "backup-001", 2_000_000, 3000, true);

        // Assert
        _logger.Received(1).Log(
            LogLevel.Information,
            Arg.Any<EventId>(),
            Arg.Any<object[]>(),
            null,
            Arg.Any<Func<object, Exception?, string>>());
    }

    [Fact]
    public void LogBackupOperation_WithFailedBackup_LogsAtErrorLevel()
    {
        // Act
        _logger.LogBackupOperation("Restore", "backup-002", 5_000_000, 15000, false);

        // Assert
        _logger.Received(1).Log(
            LogLevel.Error,
            Arg.Any<EventId>(),
            Arg.Any<object[]>(),
            null,
            Arg.Any<Func<object, Exception?, string>>());
    }

    [Fact]
    public void LogBackupOperation_WithNullLogger_ThrowsArgumentNullException()
    {
        // Act
        Action act = () => LoggingExtensions.LogBackupOperation(null!, "operation", "backup-id", 1000, 100);

        // Assert
        act.Should().Throw<ArgumentNullException>().WithParameterName("logger");
    }

    [Fact]
    public void LogBackupOperation_WithNullOperation_ThrowsArgumentNullException()
    {
        // Act
        Action act = () => _logger.LogBackupOperation(null!, "backup-id", 1000, 100);

        // Assert
        act.Should().Throw<ArgumentNullException>().WithParameterName("operation");
    }

    [Fact]
    public void LogBackupOperation_WithNullBackupId_ThrowsArgumentNullException()
    {
        // Act
        Action act = () => _logger.LogBackupOperation("operation", null!, 1000, 100);

        // Assert
        act.Should().Throw<ArgumentNullException>().WithParameterName("backupId");
    }

    #endregion

    #region LogMigrationOperation Tests

    [Fact]
    public void LogMigrationOperation_WithSuccessfulMigration_LogsAtInformationLevel()
    {
        // Act
        _logger.LogMigrationOperation("Apply", "mig-001", "1.0.0", "Add users table", 2500, true);

        // Assert
        _logger.Received(1).Log(
            LogLevel.Information,
            Arg.Any<EventId>(),
            Arg.Any<object[]>(),
            null,
            Arg.Any<Func<object, Exception?, string>>());
    }

    [Fact]
    public void LogMigrationOperation_WithFailedMigration_LogsAtErrorLevel()
    {
        // Act
        _logger.LogMigrationOperation("Rollback", "mig-002", "2.0.0", "Remove old index", 500, false);

        // Assert
        _logger.Received(1).Log(
            LogLevel.Error,
            Arg.Any<EventId>(),
            Arg.Any<object[]>(),
            null,
            Arg.Any<Func<object, Exception?, string>>());
    }

    [Fact]
    public void LogMigrationOperation_WithNullLogger_ThrowsArgumentNullException()
    {
        // Act
        Action act = () => LoggingExtensions.LogMigrationOperation(null!, "operation", "mig-id", "version", "name", 100);

        // Assert
        act.Should().Throw<ArgumentNullException>().WithParameterName("logger");
    }

    [Fact]
    public void LogMigrationOperation_WithAnyNullStringParameter_ThrowsArgumentNullException()
    {
        // Act
        Action act1 = () => _logger.LogMigrationOperation(null!, "mig-id", "version", "name", 100);
        Action act2 = () => _logger.LogMigrationOperation("operation", null!, "version", "name", 100);
        Action act3 = () => _logger.LogMigrationOperation("operation", "mig-id", null!, "name", 100);
        Action act4 = () => _logger.LogMigrationOperation("operation", "mig-id", "version", null!, 100);

        // Assert
        act1.Should().Throw<ArgumentNullException>().WithParameterName("operation");
        act2.Should().Throw<ArgumentNullException>().WithParameterName("migrationId");
        act3.Should().Throw<ArgumentNullException>().WithParameterName("version");
        act4.Should().Throw<ArgumentNullException>().WithParameterName("name");
    }

    #endregion

    #region LogApiRequest Tests

    [Fact]
    public void LogApiRequest_WithSuccessfulRequest_LogsAtInformationLevel()
    {
        // Act
        _logger.LogApiRequest("GET", "/api/users", 200, 120);

        // Assert
        _logger.Received(1).Log(
            LogLevel.Information,
            Arg.Any<EventId>(),
            Arg.Any<object[]>(),
            null,
            Arg.Any<Func<object, Exception?, string>>());
    }

    [Fact]
    public void LogApiRequest_WithClientError_LogsAtWarningLevel()
    {
        // Act
        _logger.LogApiRequest("POST", "/api/orders", 400, 80);

        // Assert
        _logger.Received(1).Log(
            LogLevel.Warning,
            Arg.Any<EventId>(),
            Arg.Any<object[]>(),
            null,
            Arg.Any<Func<object, Exception?, string>>());
    }

    [Fact]
    public void LogApiRequest_WithServerError_LogsAtWarningLevel()
    {
        // Act
        _logger.LogApiRequest("PUT", "/api/products/1", 500, 200);

        // Assert
        _logger.Received(1).Log(
            LogLevel.Warning,
            Arg.Any<EventId>(),
            Arg.Any<object[]>(),
            null,
            Arg.Any<Func<object, Exception?, string>>());
    }

    [Fact]
    public void LogApiRequest_WithNullLogger_ThrowsArgumentNullException()
    {
        // Act
        Action act = () => LoggingExtensions.LogApiRequest(null!, "method", "/path", 200, 100);

        // Assert
        act.Should().Throw<ArgumentNullException>().WithParameterName("logger");
    }

    [Fact]
    public void LogApiRequest_WithNullMethod_ThrowsArgumentNullException()
    {
        // Act
        Action act = () => _logger.LogApiRequest(null!, "/path", 200, 100);

        // Assert
        act.Should().Throw<ArgumentNullException>().WithParameterName("method");
    }

    [Fact]
    public void LogApiRequest_WithNullPath_ThrowsArgumentNullException()
    {
        // Act
        Action act = () => _logger.LogApiRequest("GET", null!, 200, 100);

        // Assert
        act.Should().Throw<ArgumentNullException>().WithParameterName("path");
    }

    #endregion

    #region LogCacheOperation Tests

    [Fact]
    public void LogCacheOperation_WithCacheHit_LogsAtDebugLevel()
    {
        // Act
        _logger.LogCacheOperation("Get", "user-profile-123", true, 5);

        // Assert
        _logger.Received(1).Log(
            LogLevel.Debug,
            Arg.Any<EventId>(),
            Arg.Any<object[]>(),
            null,
            Arg.Any<Func<object, Exception?, string>>());
    }

    [Fact]
    public void LogCacheOperation_WithCacheMiss_LogsAtInformationLevel()
    {
        // Act
        _logger.LogCacheOperation("Get", "config-settings", false, null);

        // Assert
        _logger.Received(1).Log(
            LogLevel.Information,
            Arg.Any<EventId>(),
            Arg.Any<object[]>(),
            null,
            Arg.Any<Func<object, Exception?, string>>());
    }

    [Fact]
    public void LogCacheOperation_WithNullLogger_ThrowsArgumentNullException()
    {
        // Act
        Action act = () => LoggingExtensions.LogCacheOperation(null!, "operation", "cache-key", true);

        // Assert
        act.Should().Throw<ArgumentNullException>().WithParameterName("logger");
    }

    [Fact]
    public void LogCacheOperation_WithNullOperation_ThrowsArgumentNullException()
    {
        // Act
        Action act = () => _logger.LogCacheOperation(null!, "cache-key", true);

        // Assert
        act.Should().Throw<ArgumentNullException>().WithParameterName("operation");
    }

    [Fact]
    public void LogCacheOperation_WithNullCacheKey_ThrowsArgumentNullException()
    {
        // Act
        Action act = () => _logger.LogCacheOperation("operation", null!, true);

        // Assert
        act.Should().Throw<ArgumentNullException>().WithParameterName("cacheKey");
    }

    #endregion

    #region LogValidationError Tests

    [Fact]
    public void LogValidationError_WithMultipleErrors_LogsAllErrors()
    {
        // Arrange
        var errors = new Dictionary<string, string>
        {
            ["Email"] = "Invalid email format",
            ["Password"] = "Password too short",
            ["Age"] = "Must be 18 or older"
        };

        // Act
        _logger.LogValidationError("UserRegistration", errors);

        // Assert
        _logger.Received(1).Log(
            LogLevel.Warning,
            Arg.Any<EventId>(),
            Arg.Any<object[]>(),
            null,
            Arg.Any<Func<object, Exception?, string>>());
    }

    [Fact]
    public void LogValidationError_WithSingleError_LogsSingleError()
    {
        // Arrange
        var errors = new Dictionary<string, string> { ["Username"] = "Username already taken" };

        // Act
        _logger.LogValidationError("UserLogin", errors);

        // Assert
        _logger.Received(1).Log(
            LogLevel.Warning,
            Arg.Any<EventId>(),
            Arg.Any<object[]>(),
            null,
            Arg.Any<Func<object, Exception?, string>>());
    }

    [Fact]
    public void LogValidationError_WithNullLogger_ThrowsArgumentNullException()
    {
        // Act
        Action act = () => LoggingExtensions.LogValidationError(null!, "entity", new Dictionary<string, string>());

        // Assert
        act.Should().Throw<ArgumentNullException>().WithParameterName("logger");
    }

    [Fact]
    public void LogValidationError_WithNullEntityType_ThrowsArgumentNullException()
    {
        // Act
        Action act = () => _logger.LogValidationError(null!, new Dictionary<string, string>());

        // Assert
        act.Should().Throw<ArgumentNullException>().WithParameterName("entityType");
    }

    [Fact]
    public void LogValidationError_WithNullErrors_ThrowsArgumentNullException()
    {
        // Act
        Action act = () => _logger.LogValidationError("entity", null!);

        // Assert
        act.Should().Throw<ArgumentNullException>().WithParameterName("errors");
    }

    #endregion

    #region LogWebhookDelivery Tests

    [Fact]
    public void LogWebhookDelivery_WithSuccessfulDelivery_LogsAtInformationLevel()
    {
        // Act
        _logger.LogWebhookDelivery("wh-123", "https://example.com/webhook", 0, 3, true);

        // Assert
        _logger.Received(1).Log(
            LogLevel.Information,
            Arg.Any<EventId>(),
            Arg.Any<object[]>(),
            null,
            Arg.Any<Func<object, Exception?, string>>());
    }

    [Fact]
    public void LogWebhookDelivery_WithFailedDelivery_LogsAtWarningLevel()
    {
        // Act
        _logger.LogWebhookDelivery("wh-456", "https://api.service.com/hook", 2, 3, false);

        // Assert
        _logger.Received(1).Log(
            LogLevel.Warning,
            Arg.Any<EventId>(),
            Arg.Any<object[]>(),
            null,
            Arg.Any<Func<object, Exception?, string>>());
    }

    [Fact]
    public void LogWebhookDelivery_WithNullLogger_ThrowsArgumentNullException()
    {
        // Act
        Action act = () => LoggingExtensions.LogWebhookDelivery(null!, "webhook-id", "url", 0, 3);

        // Assert
        act.Should().Throw<ArgumentNullException>().WithParameterName("logger");
    }

    [Fact]
    public void LogWebhookDelivery_WithNullWebhookId_ThrowsArgumentNullException()
    {
        // Act
        Action act = () => _logger.LogWebhookDelivery(null!, "url", 0, 3);

        // Assert
        act.Should().Throw<ArgumentNullException>().WithParameterName("webhookId");
    }

    [Fact]
    public void LogWebhookDelivery_WithNullUrl_ThrowsArgumentNullException()
    {
        // Act
        Action act = () => _logger.LogWebhookDelivery("webhook-id", null!, 0, 3);

        // Assert
        act.Should().Throw<ArgumentNullException>().WithParameterName("url");
    }

    #endregion

    #region LogBackgroundJob Tests

    [Fact]
    public void LogBackgroundJob_WithSuccessfulJob_LogsAtInformationLevel()
    {
        // Act
        _logger.LogBackgroundJob("ProcessOrders", 45000, 150, true);

        // Assert
        _logger.Received(1).Log(
            LogLevel.Information,
            Arg.Any<EventId>(),
            Arg.Any<object[]>(),
            null,
            Arg.Any<Func<object, Exception?, string>>());
    }

    [Fact]
    public void LogBackgroundJob_WithFailedJob_LogsAtErrorLevel()
    {
        // Act
        _logger.LogBackgroundJob("SendEmails", 120000, 0, false);

        // Assert
        _logger.Received(1).Log(
            LogLevel.Error,
            Arg.Any<EventId>(),
            Arg.Any<object[]>(),
            null,
            Arg.Any<Func<object, Exception?, string>>());
    }

    [Fact]
    public void LogBackgroundJob_WithNullLogger_ThrowsArgumentNullException()
    {
        // Act
        Action act = () => LoggingExtensions.LogBackgroundJob(null!, "job-name", 1000);

        // Assert
        act.Should().Throw<ArgumentNullException>().WithParameterName("logger");
    }

    [Fact]
    public void LogBackgroundJob_WithNullJobName_ThrowsArgumentNullException()
    {
        // Act
        Action act = () => _logger.LogBackgroundJob(null!, 1000);

        // Assert
        act.Should().Throw<ArgumentNullException>().WithParameterName("jobName");
    }

    #endregion

    #region LogHealthCheck Tests

    [Fact]
    public void LogHealthCheck_WithHealthyComponent_LogsAtDebugLevel()
    {
        // Act
        _logger.LogHealthCheck("Database", true, 50, "All connections healthy");

        // Assert
        _logger.Received(1).Log(
            LogLevel.Debug,
            Arg.Any<EventId>(),
            Arg.Any<object[]>(),
            null,
            Arg.Any<Func<object, Exception?, string>>());
    }

    [Fact]
    public void LogHealthCheck_WithUnhealthyComponent_LogsAtWarningLevel()
    {
        // Act
        _logger.LogHealthCheck("CacheService", false, 200, "Connection timeout");

        // Assert
        _logger.Received(1).Log(
            LogLevel.Warning,
            Arg.Any<EventId>(),
            Arg.Any<object[]>(),
            null,
            Arg.Any<Func<object, Exception?, string>>());
    }

    [Fact]
    public void LogHealthCheck_WithNullLogger_ThrowsArgumentNullException()
    {
        // Act
        Action act = () => LoggingExtensions.LogHealthCheck(null!, "component", true, 100);

        // Assert
        act.Should().Throw<ArgumentNullException>().WithParameterName("logger");
    }

    [Fact]
    public void LogHealthCheck_WithNullComponentName_ThrowsArgumentNullException()
    {
        // Act
        Action act = () => _logger.LogHealthCheck(null!, true, 100);

        // Assert
        act.Should().Throw<ArgumentNullException>().WithParameterName("componentName");
    }

    #endregion
}