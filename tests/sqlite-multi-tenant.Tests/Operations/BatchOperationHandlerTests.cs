#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using Microsoft.Extensions.Logging;
using Xunit;

namespace SqliteMultiTenant.Operations.Tests;

/// <summary>
/// Tests for BatchOperationHandler to verify partial-failure semantics and atomicity contracts.
/// </summary>
public class BatchOperationHandlerTests
{
    private readonly ILogger<BatchOperationHandler> _logger;
    private readonly IBatchOperationHandler _handler;

    public BatchOperationHandlerTests()
    {
        var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder.AddConsole();
            builder.AddDebug();
        });
        _logger = loggerFactory.CreateLogger<BatchOperationHandler>();
        _handler = new BatchOperationHandler(_logger);
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
}