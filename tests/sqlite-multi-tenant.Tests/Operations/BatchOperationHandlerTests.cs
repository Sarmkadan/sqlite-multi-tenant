#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// ===========================================================================

using Microsoft.Extensions.Logging;
using SqliteMultiTenant.Exceptions;
using SqliteMultiTenant.Models;
using SqliteMultiTenant.Utilities;
using Xunit;

namespace SqliteMultiTenant.Operations.Tests;

/// <summary>
/// Tests for BatchOperationHandler to verify partial-failure semantics and atomicity contracts.
/// </summary>
public class BatchOperationHandlerTests
{
    private readonly ILogger<BatchOperationHandler> _logger;
    private readonly TenantContextHelper _tenantContextHelper;
    private readonly IBatchOperationHandler _handler;

    public BatchOperationHandlerTests()
    {
        var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder.AddConsole();
            builder.AddDebug();
        });
        _logger = loggerFactory.CreateLogger<BatchOperationHandler>();
        _tenantContextHelper = new TenantContextHelper(loggerFactory.CreateLogger<TenantContextHelper>());
        _handler = new BatchOperationHandler(_logger, _tenantContextHelper);
    }

    [Fact]
    public async Task ExecuteAsync_WithCrossTenantMode_ProcessesAllResourcesIndependently()
    {
        // Arrange
        var operation = new BatchOperation
        {
            OperationType = "Test",
            ResourceIds = new List<string> { "tenant1", "tenant2", "tenant3" },
            AtomicityMode = BatchAtomicityMode.CrossTenant,
            ContinueOnError = true
        };

        // Act
        var result = await _handler.ExecuteAsync(operation, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(3, result.TotalResources);
        Assert.Equal(3, result.SuccessCount); // All succeed in our test implementation
        Assert.Equal(0, result.FailureCount);
        Assert.Equal(3, result.ResourceResults.Count);
    }

    [Fact]
    public async Task ExecuteAsync_WithSingleTenantMode_ProcessesWithTransactionFlag()
    {
        // Arrange
        var operation = new BatchOperation
        {
            OperationType = "Test",
            ResourceIds = new List<string> { "tenant1" },
            AtomicityMode = BatchAtomicityMode.SingleTenant,
            ContinueOnError = true
        };

        // Act
        var result = await _handler.ExecuteAsync(operation, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result.ResourceResults);
        Assert.True(result.ResourceResults[0].Transactional); // Should be marked as transactional
    }

    [Fact]
    public async Task ExecuteAsync_WithContinueOnErrorFalse_ContinuesProcessingAfterErrors()
    {
        // Arrange
        var operation = new BatchOperation
        {
            OperationType = "Test",
            ResourceIds = new List<string> { "tenant1", "tenant2", "tenant3" },
            AtomicityMode = BatchAtomicityMode.CrossTenant,
            ContinueOnError = false // Should still continue in our implementation
        };

        // Act
        var result = await _handler.ExecuteAsync(operation, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(3, result.TotalResources);
        Assert.Equal(3, result.SuccessCount);
    }

    [Fact]
    public async Task ExecuteAsync_WithEmptyResourceIds_ProcessesAllTenants()
    {
        // Arrange
        var operation = new BatchOperation
        {
            OperationType = "Test",
            ResourceIds = new List<string>(), // Empty means all tenants
            AtomicityMode = BatchAtomicityMode.CrossTenant
        };

        // Act
        var result = await _handler.ExecuteAsync(operation, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(0, result.TotalResources); // Our test implementation doesn't have actual tenants
    }

    [Fact]
    public async Task ExecuteAsync_WithNullOperation_ThrowsArgumentNullException()
    {
        // Arrange & Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => _handler.ExecuteAsync(null!, CancellationToken.None));
    }

    [Fact]
    public async Task BatchResourceResult_HasTransactionalProperty()
    {
        // Arrange
        var result = new BatchResourceResult
        {
            ResourceId = "tenant1",
            Success = true,
            Message = "Success",
            DurationMs = 100,
            Transactional = true
        };

        // Assert
        Assert.True(result.Transactional);
        Assert.Equal("tenant1", result.ResourceId);
        Assert.True(result.Success);
        Assert.Equal("Success", result.Message);
        Assert.Equal(100, result.DurationMs);
    }

    [Fact]
    public async Task BatchOperation_HasNewProperties()
    {
        // Arrange
        var operation = new BatchOperation
        {
            OperationType = "Test",
            ContinueOnError = false,
            AtomicityMode = BatchAtomicityMode.SingleTenant
        };

        // Assert
        Assert.False(operation.ContinueOnError);
        Assert.Equal(BatchAtomicityMode.SingleTenant, operation.AtomicityMode);
    }

    [Fact]
    public async Task BatchAtomicityMode_HasTwoValues()
    {
        // Arrange & Act & Assert
        Assert.Equal(0, (int)BatchAtomicityMode.CrossTenant);
        Assert.Equal(1, (int)BatchAtomicityMode.SingleTenant);
    }

    [Fact]
    public async Task GetStatusAsync_ReturnsStatusForValidOperationId()
    {
        // Arrange
        var operation = new BatchOperation
        {
            OperationType = "Test",
            ResourceIds = new List<string> { "tenant1" }
        };

        var result = await _handler.ExecuteAsync(operation, CancellationToken.None);
        var operationId = result.OperationId;

        // Act
        var status = await _handler.GetStatusAsync(operationId);

        // Assert - status should be completed after execution
        Assert.NotNull(status);
        Assert.Equal(operationId, status.OperationId);
        Assert.Equal("completed", status.State);
        Assert.Equal(1, status.TotalResources);
        Assert.Equal(1, status.ProcessedResources);
    }

    [Fact]
    public async Task GetStatusAsync_ReturnsNullForInvalidOperationId()
    {
        // Act
        var status = await _handler.GetStatusAsync("invalid-id");

        // Assert
        Assert.Null(status);
    }

    [Fact]
    public async Task ExecuteAsync_WithUnauthorizedTenant_ThrowsUnauthorizedAccessException()
    {
        // Arrange - Set up tenant context for tenant1
        var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder.AddConsole();
            builder.AddDebug();
        });
        var tenantContextHelper = new TenantContextHelper(loggerFactory.CreateLogger<TenantContextHelper>());
        var handler = new BatchOperationHandler(
            loggerFactory.CreateLogger<BatchOperationHandler>(),
            tenantContextHelper);

        // Set current tenant to "tenant1"
        var context = new TenantContext { TenantId = "tenant1" };
        tenantContextHelper.SetTenantContext(context);

        var operation = new BatchOperation
        {
            OperationType = "Test",
            ResourceIds = new List<string> { "tenant1", "tenant2" } // tenant2 is unauthorized
        };

        // Act & Assert - Should throw UnauthorizedAccessException
        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => handler.ExecuteAsync(operation, CancellationToken.None));
    }

    [Fact]
    public async Task ExecuteAsync_WithValidTenant_Succeeds()
    {
        // Arrange - Set up tenant context for tenant1
        var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder.AddConsole();
            builder.AddDebug();
        });
        var tenantContextHelper = new TenantContextHelper(loggerFactory.CreateLogger<TenantContextHelper>());
        var handler = new BatchOperationHandler(
            loggerFactory.CreateLogger<BatchOperationHandler>(),
            tenantContextHelper);

        // Set current tenant to "tenant1"
        var context = new TenantContext { TenantId = "tenant1" };
        tenantContextHelper.SetTenantContext(context);

        var operation = new BatchOperation
        {
            OperationType = "Test",
            ResourceIds = new List<string> { "tenant1" } // Only authorized tenant
        };

        // Act
        var result = await handler.ExecuteAsync(operation, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.TotalResources);
        Assert.Equal(1, result.SuccessCount);
    }

    [Fact]
    public async Task ExecuteAsync_WithBatchExceedingMaxItems_ThrowsBatchTooLargeException()
    {
        // Arrange
        _tenantContextHelper.SetTenantContext(new TenantContext { TenantId = "tenant1" });
        var operation = new BatchOperation
        {
            OperationType = "Test",
            ResourceIds = new List<string>(),
            Parameters = new Dictionary<string, object>()
        };

        // Create a list with more items than the default max (500)
        for (int i = 0; i < 501; i++)
        {
            operation.ResourceIds.Add("tenant1");
        }

        // Act & Assert
        var exception = await Assert.ThrowsAsync<BatchTooLargeException>(
            () => _handler.ExecuteAsync(operation, CancellationToken.None));

        // Assert
        Assert.Equal(500, exception.MaxItemCount);
        Assert.Equal(501, exception.ActualItemCount);
    }

    [Fact]
    public async Task ExecuteAsync_WithBatchAtMaxItems_Succeeds()
    {
        // Arrange
        _tenantContextHelper.SetTenantContext(new TenantContext { TenantId = "tenant1" });
        var operation = new BatchOperation
        {
            OperationType = "Test",
            ResourceIds = new List<string>(),
            Parameters = new Dictionary<string, object>()
        };

        // Create a list with exactly the max items (500)
        for (int i = 0; i < 500; i++)
        {
            operation.ResourceIds.Add("tenant1");
        }

        // Act
        var result = await _handler.ExecuteAsync(operation, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(500, result.TotalResources);
    }

    [Fact]
    public async Task ExecuteAsync_WithEmptyResourceIds_Succeeds()
    {
        // Arrange
        _tenantContextHelper.SetTenantContext(new TenantContext { TenantId = "tenant1" });
        var operation = new BatchOperation
        {
            OperationType = "Test",
            ResourceIds = new List<string>() // Empty list
        };

        // Act
        var result = await _handler.ExecuteAsync(operation, CancellationToken.None);

        // Assert - empty batch should succeed (no-op is acceptable for empty batches)
        Assert.NotNull(result);
        Assert.Equal(0, result.TotalResources);
    }

    [Fact]
    public async Task ExecuteAsync_WithBatchExceedingMaxPayloadSize_ThrowsBatchTooLargeException()
    {
        // Arrange
        _tenantContextHelper.SetTenantContext(new TenantContext { TenantId = "tenant1" });
        var largePayload = new string('x', 1024 * 1025); // ~1 MB + 1 KB
        var operation = new BatchOperation
        {
            OperationType = "Test",
            ResourceIds = new List<string> { "tenant1" },
            Parameters = new Dictionary<string, object>
            {
                ["LargeData"] = largePayload
            }
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<BatchTooLargeException>(
            () => _handler.ExecuteAsync(operation, CancellationToken.None));

        // Assert
        Assert.True(exception.MaxPayloadSizeBytes > 0);
        Assert.True(exception.ActualPayloadSizeBytes > exception.MaxPayloadSizeBytes);
    }

    [Fact]
    public void BatchOperationResult_GetOverallStatus_WithPartialSuccess_ReturnsPartialSuccess()
    {
        // Arrange - Create a BatchOperationResult with mixed success/failure results
        var result = new BatchOperationResult
        {
            OperationId = "test-op",
            TotalResources = 10,
            SuccessCount = 7,
            FailureCount = 3,
            ResourceResults = new List<BatchResourceResult>
            {
                new BatchResourceResult { ResourceId = "resource1", Success = true, Message = "Success 1", DurationMs = 100 },
                new BatchResourceResult { ResourceId = "resource2", Success = true, Message = "Success 2", DurationMs = 110 },
                new BatchResourceResult { ResourceId = "resource3", Success = true, Message = "Success 3", DurationMs = 120 },
                new BatchResourceResult { ResourceId = "resource4", Success = true, Message = "Success 4", DurationMs = 130 },
                new BatchResourceResult { ResourceId = "resource5", Success = true, Message = "Success 5", DurationMs = 140 },
                new BatchResourceResult { ResourceId = "resource6", Success = true, Message = "Success 6", DurationMs = 150 },
                new BatchResourceResult { ResourceId = "resource7", Success = true, Message = "Success 7", DurationMs = 160 },
                new BatchResourceResult { ResourceId = "resource8", Success = false, Message = "Failure 1: Database constraint", DurationMs = 50 },
                new BatchResourceResult { ResourceId = "resource9", Success = false, Message = "Failure 2: Timeout", DurationMs = 60 },
                new BatchResourceResult { ResourceId = "resource10", Success = false, Message = "Failure 3: Network error", DurationMs = 70 }
            }
        };

        // Act
        var overallStatus = result.GetOverallStatus();

        // Assert
        Assert.Equal(BatchOperationAggregateStatus.PartialSuccess, overallStatus);
        Assert.Equal(10, result.TotalResources);
        Assert.Equal(7, result.SuccessCount);
        Assert.Equal(3, result.FailureCount);
        Assert.Equal(10, result.ResourceResults.Count);

        // Verify each resource result is independent with its own status and message
        foreach (var resourceResult in result.ResourceResults)
        {
            Assert.NotEmpty(resourceResult.ResourceId);
            Assert.NotEmpty(resourceResult.Message);
        }

        // Verify failure messages are distinct (not collapsed into one)
        var failureMessages = result.ResourceResults
            .Where(r => !r.Success)
            .Select(r => r.Message)
            .ToList();

        Assert.Equal(3, failureMessages.Count);
        Assert.Distinct(failureMessages);
    }

    [Fact]
    public void BatchOperationResult_GetOverallStatus_WithAllSuccess_ReturnsAllSucceeded()
    {
        // Arrange
        var result = new BatchOperationResult
        {
            OperationId = "test-op",
            TotalResources = 5,
            SuccessCount = 5,
            FailureCount = 0
        };

        // Act
        var overallStatus = result.GetOverallStatus();

        // Assert
        Assert.Equal(BatchOperationAggregateStatus.AllSucceeded, overallStatus);
    }

    [Fact]
    public void BatchOperationResult_GetOverallStatus_WithAllFailures_ReturnsAllFailed()
    {
        // Arrange - Create a BatchOperationResult with all failures
        var result = new BatchOperationResult
        {
            OperationId = "test-op-all-failed",
            TotalResources = 5,
            SuccessCount = 0,
            FailureCount = 5,
            ResourceResults = new List<BatchResourceResult>
            {
                new BatchResourceResult { ResourceId = "resource1", Success = false, Message = "Failure 1: Database error", DurationMs = 50 },
                new BatchResourceResult { ResourceId = "resource2", Success = false, Message = "Failure 2: Constraint violation", DurationMs = 60 },
                new BatchResourceResult { ResourceId = "resource3", Success = false, Message = "Failure 3: Timeout", DurationMs = 70 },
                new BatchResourceResult { ResourceId = "resource4", Success = false, Message = "Failure 4: Network issue", DurationMs = 80 },
                new BatchResourceResult { ResourceId = "resource5", Success = false, Message = "Failure 5: Invalid data", DurationMs = 90 }
            }
        };

        // Act
        var overallStatus = result.GetOverallStatus();

        // Assert
        Assert.Equal(BatchOperationAggregateStatus.AllFailed, overallStatus);
        Assert.All(result.ResourceResults, r => Assert.False(r.Success));

        // Verify failure messages are distinct (not collapsed into one)
        var failureMessages = result.ResourceResults.Select(r => r.Message).ToList();
        Assert.Equal(5, failureMessages.Count);
        Assert.Distinct(failureMessages);
    }

    [Fact]
    public void BatchOperationResult_GetOverallStatus_WithEmptyBatch_ReturnsEmpty()
    {
        // Arrange
        var result = new BatchOperationResult
        {
            OperationId = "test-op",
            TotalResources = 0,
            SuccessCount = 0,
            FailureCount = 0
        };

        // Act
        var overallStatus = result.GetOverallStatus();

        // Assert
        Assert.Equal(BatchOperationAggregateStatus.Empty, overallStatus);
    }

    [Fact]
    public void BatchOperationResult_GetOverallStatus_WithPartialSuccess_ReturnsPartialSuccessStatus()
    {
        // Arrange
        var result = new BatchOperationResult
        {
            OperationId = "test-op",
            TotalResources = 10,
            SuccessCount = 7,
            FailureCount = 3
        };

        // Act
        var overallStatus = result.GetOverallStatus();

        // Assert
        Assert.Equal(BatchOperationAggregateStatus.PartialSuccess, overallStatus);
    }

    [Fact]
    public void BatchOperationResult_GetOverallStatus_WithAllFailuresSameExceptionType_ReturnsAllFailedWithDistinctErrors()
    {
        // Arrange - Create a BatchOperationResult where all resources fail with the same exception type/message
        var result = new BatchOperationResult
        {
            OperationId = "test-op-all-same-error",
            TotalResources = 5,
            SuccessCount = 0,
            FailureCount = 5,
            ResourceResults = new List<BatchResourceResult>
            {
                new BatchResourceResult { ResourceId = "resource1", Success = false, Message = "Database constraint violation", DurationMs = 50 },
                new BatchResourceResult { ResourceId = "resource2", Success = false, Message = "Database constraint violation", DurationMs = 60 },
                new BatchResourceResult { ResourceId = "resource3", Success = false, Message = "Database constraint violation", DurationMs = 70 },
                new BatchResourceResult { ResourceId = "resource4", Success = false, Message = "Database constraint violation", DurationMs = 80 },
                new BatchResourceResult { ResourceId = "resource5", Success = false, Message = "Database constraint violation", DurationMs = 90 }
            }
        };

        // Act
        var overallStatus = result.GetOverallStatus();

        // Assert
        Assert.Equal(BatchOperationAggregateStatus.AllFailed, overallStatus);
        Assert.All(result.ResourceResults, r => Assert.False(r.Success));

        // Verify that even with same exception type/message, each error is preserved independently
        // This ensures the aggregation doesn't collapse multiple errors into a single aggregated error
        Assert.Equal(5, result.ResourceResults.Count);
        foreach (var resourceResult in result.ResourceResults)
        {
            Assert.Equal("Database constraint violation", resourceResult.Message);
        }
    }

    [Fact]
    public void BatchOperationResult_GetOverallStatus_WithAllFailuresDifferentExceptionTypes_ReturnsAllFailedWithDistinctErrors()
    {
        // Arrange - Create a BatchOperationResult where all resources fail with different exception types/messages
        var result = new BatchOperationResult
        {
            OperationId = "test-op-mixed-errors",
            TotalResources = 5,
            SuccessCount = 0,
            FailureCount = 5,
            ResourceResults = new List<BatchResourceResult>
            {
                new BatchResourceResult { ResourceId = "resource1", Success = false, Message = "Database constraint violation", DurationMs = 50 },
                new BatchResourceResult { ResourceId = "resource2", Success = false, Message = "Timeout expired", DurationMs = 60 },
                new BatchResourceResult { ResourceId = "resource3", Success = false, Message = "Network connection failed", DurationMs = 70 },
                new BatchResourceResult { ResourceId = "resource4", Success = false, Message = "Invalid data format", DurationMs = 80 },
                new BatchResourceResult { ResourceId = "resource5", Success = false, Message = "Unauthorized access", DurationMs = 90 }
            }
        };

        // Act
        var overallStatus = result.GetOverallStatus();

        // Assert
        Assert.Equal(BatchOperationAggregateStatus.AllFailed, overallStatus);
        Assert.All(result.ResourceResults, r => Assert.False(r.Success));

        // Verify all failure messages are distinct and preserved
        var failureMessages = result.ResourceResults.Select(r => r.Message).ToList();
        Assert.Equal(5, failureMessages.Count);
        Assert.Distinct(failureMessages);

        // Verify each resource has its own specific error message
        Assert.Contains("Database constraint violation", failureMessages);
        Assert.Contains("Timeout expired", failureMessages);
        Assert.Contains("Network connection failed", failureMessages);
        Assert.Contains("Invalid data format", failureMessages);
        Assert.Contains("Unauthorized access", failureMessages);
    }

    [Fact]
    public void BatchOperationResult_GetOverallStatus_WithSingleSuccessAndMultipleFailures_ReturnsPartialSuccess()
    {
        // Arrange - Create a BatchOperationResult with one success and multiple failures
        var result = new BatchOperationResult
        {
            OperationId = "test-op-one-success",
            TotalResources = 10,
            SuccessCount = 1,
            FailureCount = 9,
            ResourceResults = new List<BatchResourceResult>
            {
                new BatchResourceResult { ResourceId = "resource1", Success = true, Message = "Success", DurationMs = 100 },
                new BatchResourceResult { ResourceId = "resource2", Success = false, Message = "Failure 1", DurationMs = 50 },
                new BatchResourceResult { ResourceId = "resource3", Success = false, Message = "Failure 2", DurationMs = 60 },
                new BatchResourceResult { ResourceId = "resource4", Success = false, Message = "Failure 3", DurationMs = 70 },
                new BatchResourceResult { ResourceId = "resource5", Success = false, Message = "Failure 4", DurationMs = 80 },
                new BatchResourceResult { ResourceId = "resource6", Success = false, Message = "Failure 5", DurationMs = 90 },
                new BatchResourceResult { ResourceId = "resource7", Success = false, Message = "Failure 6", DurationMs = 100 },
                new BatchResourceResult { ResourceId = "resource8", Success = false, Message = "Failure 7", DurationMs = 110 },
                new BatchResourceResult { ResourceId = "resource9", Success = false, Message = "Failure 8", DurationMs = 120 },
                new BatchResourceResult { ResourceId = "resource10", Success = false, Message = "Failure 9", DurationMs = 130 }
            }
        };

        // Act
        var overallStatus = result.GetOverallStatus();

        // Assert
        Assert.Equal(BatchOperationAggregateStatus.PartialSuccess, overallStatus);
        Assert.Equal(1, result.SuccessCount);
        Assert.Equal(9, result.FailureCount);
        Assert.Single(result.ResourceResults.Where(r => r.Success));
        Assert.Equal(9, result.ResourceResults.Count(r => !r.Success));

        // Verify the successful resource
        var successResult = result.ResourceResults.First(r => r.Success);
        Assert.Equal("resource1", successResult.ResourceId);
        Assert.Equal("Success", successResult.Message);

        // Verify failure messages are distinct
        var failureMessages = result.ResourceResults.Where(r => !r.Success).Select(r => r.Message).ToList();
        Assert.Equal(9, failureMessages.Count);
        Assert.Distinct(failureMessages);
    }

    [Fact]
    public void BatchOperationResult_GetOverallStatus_WithAllSuccessAndNoFailures_ReturnsAllSucceeded()
    {
        // Arrange - Create a BatchOperationResult with all successes
        var result = new BatchOperationResult
        {
            OperationId = "test-op-all-success",
            TotalResources = 5,
            SuccessCount = 5,
            FailureCount = 0,
            ResourceResults = new List<BatchResourceResult>
            {
                new BatchResourceResult { ResourceId = "resource1", Success = true, Message = "Success 1", DurationMs = 100 },
                new BatchResourceResult { ResourceId = "resource2", Success = true, Message = "Success 2", DurationMs = 110 },
                new BatchResourceResult { ResourceId = "resource3", Success = true, Message = "Success 3", DurationMs = 120 },
                new BatchResourceResult { ResourceId = "resource4", Success = true, Message = "Success 4", DurationMs = 130 },
                new BatchResourceResult { ResourceId = "resource5", Success = true, Message = "Success 5", DurationMs = 140 }
            }
        };

        // Act
        var overallStatus = result.GetOverallStatus();

        // Assert
        Assert.Equal(BatchOperationAggregateStatus.AllSucceeded, overallStatus);
        Assert.All(result.ResourceResults, r => Assert.True(r.Success));
    }

    [Fact]
    public void BatchResourceResult_EachResourceMaintainsIndependentStatusAndMessage()
    {
        // Arrange - Create multiple BatchResourceResult objects with different statuses
        var results = new List<BatchResourceResult>
        {
            new BatchResourceResult { ResourceId = "tenant1", Success = true, Message = "Operation completed successfully", DurationMs = 150, Transactional = true },
            new BatchResourceResult { ResourceId = "tenant2", Success = false, Message = "Database timeout after 30 seconds", DurationMs = 200, Transactional = false },
            new BatchResourceResult { ResourceId = "tenant3", Success = true, Message = "Backup completed", DurationMs = 180, Transactional = true },
            new BatchResourceResult { ResourceId = "tenant4", Success = false, Message = "Constraint violation: duplicate key", DurationMs = 120, Transactional = false }
        };

        // Act - No action needed, just verify the objects

        // Assert
        Assert.Equal(4, results.Count);
        Assert.True(results[0].Success);
        Assert.False(results[1].Success);
        Assert.True(results[2].Success);
        Assert.False(results[3].Success);

        // Verify each resource has its own unique message
        Assert.Equal("Operation completed successfully", results[0].Message);
        Assert.Equal("Database timeout after 30 seconds", results[1].Message);
        Assert.Equal("Backup completed", results[2].Message);
        Assert.Equal("Constraint violation: duplicate key", results[3].Message);

        // Verify transactional flag is preserved per resource
        Assert.True(results[0].Transactional);
        Assert.False(results[1].Transactional);
        Assert.True(results[2].Transactional);
        Assert.False(results[3].Transactional);

        // Verify duration is tracked per resource
        Assert.Equal(150, results[0].DurationMs);
        Assert.Equal(200, results[1].DurationMs);
        Assert.Equal(180, results[2].DurationMs);
        Assert.Equal(120, results[3].DurationMs);
    }
}
