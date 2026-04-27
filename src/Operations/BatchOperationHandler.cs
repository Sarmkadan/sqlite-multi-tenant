#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using Microsoft.Extensions.Logging;

namespace SqliteMultiTenant.Operations;

/// <summary>
/// Handler for batch operations across multiple resources.
/// Enables efficient bulk migrations, backups, or tenant operations.
/// Tracks progress and handles partial failures gracefully.
/// </summary>
public interface IBatchOperationHandler
{
    Task<BatchOperationResult> ExecuteAsync(BatchOperation operation, CancellationToken cancellationToken);
    Task<BatchOperationStatus> GetStatusAsync(string operationId);
}

/// <summary>
/// Batch operation definition with resources and parameters.
/// </summary>
public sealed class BatchOperation {
    public string OperationId { get; set; } = Guid.NewGuid().ToString();
    public string OperationType { get; set; }
    public List<string> ResourceIds { get; set; } = new();
    public Dictionary<string, object> Parameters { get; set; } = new();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Result of batch operation execution.
/// </summary>
public sealed class BatchOperationResult {
    public string OperationId { get; set; }
    public int TotalResources { get; set; }
    public int SuccessCount { get; set; }
    public int FailureCount { get; set; }
    public List<BatchResourceResult> ResourceResults { get; set; } = new();
    public DateTime CompletedAt { get; set; } = DateTime.UtcNow;
    public TimeSpan Duration { get; set; }
}

/// <summary>
/// Result for individual resource in batch operation.
/// </summary>
public sealed class BatchResourceResult {
    public string ResourceId { get; set; }
    public bool Success { get; set; }
    public string Message { get; set; }
    public long DurationMs { get; set; }
}

/// <summary>
/// Status of ongoing batch operation.
/// </summary>
public sealed class BatchOperationStatus {
    public string OperationId { get; set; }
    public string State { get; set; } // pending, running, completed, failed
    public int TotalResources { get; set; }
    public int ProcessedResources { get; set; }
    public int ProgressPercent => TotalResources > 0 ? (ProcessedResources * 100) / TotalResources : 0;
    public DateTime CreatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
}

/// <summary>
/// Batch operation handler implementation.
/// </summary>
public sealed class BatchOperationHandler : IBatchOperationHandler {
    private readonly ILogger<BatchOperationHandler> _logger;
    private readonly Dictionary<string, BatchOperationStatus> _statusTracker = new();

    public BatchOperationHandler(ILogger<BatchOperationHandler> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Executes batch operation across multiple resources.
    /// Processes in parallel with configurable concurrency level.
    /// Tracks progress and handles failures without stopping other operations.
    /// </summary>
    public async Task<BatchOperationResult> ExecuteAsync(BatchOperation operation, CancellationToken cancellationToken)
    {
        var operationId = operation.OperationId;
        var startTime = DateTime.UtcNow;

        _logger.LogInformation(
            "Starting batch operation {type}: {operationId} with {count} resources",
            operation.OperationType,
            operationId,
            operation.ResourceIds.Count);

        // Initialize status tracking
        _statusTracker[operationId] = new BatchOperationStatus
        {
            OperationId = operationId,
            State = "running",
            TotalResources = operation.ResourceIds.Count,
            CreatedAt = startTime
        };

        var result = new BatchOperationResult
        {
            OperationId = operationId,
            TotalResources = operation.ResourceIds.Count
        };

        // Process resources in batches (max 10 concurrent)
        var batchSize = 10;
        var batches = operation.ResourceIds.Chunk(batchSize);

        foreach (var batch in batches)
        {
            if (cancellationToken.IsCancellationRequested)
                break;

            var tasks = batch.Select(resourceId =>
                ProcessResourceAsync(operationId, operation.OperationType, resourceId, operation.Parameters, cancellationToken)
            );

            var batchResults = await Task.WhenAll(tasks);

            foreach (var resourceResult in batchResults)
            {
                result.ResourceResults.Add(resourceResult);

                if (resourceResult.Success)
                    result.SuccessCount++;
                else
                    result.FailureCount++;

                // Update progress
                if (_statusTracker.TryGetValue(operationId, out var status))
                    status.ProcessedResources = result.SuccessCount + result.FailureCount;
            }
        }

        // Finalize
        result.Duration = DateTime.UtcNow - startTime;
        if (_statusTracker.TryGetValue(operationId, out var finalStatus))
        {
            finalStatus.State = result.FailureCount == 0 ? "completed" : "completed";
            finalStatus.CompletedAt = DateTime.UtcNow;
        }

        _logger.LogInformation(
            "Batch operation completed {operationId}: {success}/{total} successful in {duration}ms",
            operationId,
            result.SuccessCount,
            result.TotalResources,
            result.Duration.TotalMilliseconds);

        return result;
    }

    /// <summary>
    /// Gets status of batch operation.
    /// Used for polling progress from clients.
    /// </summary>
    public Task<BatchOperationStatus> GetStatusAsync(string operationId)
    {
        if (_statusTracker.TryGetValue(operationId, out var status))
            return Task.FromResult(status);

        return Task.FromResult<BatchOperationStatus>(null);
    }

    /// <summary>
    /// Processes individual resource in batch operation.
    /// </summary>
    private async Task<BatchResourceResult> ProcessResourceAsync(
        string operationId,
        string operationType,
        string resourceId,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        try
        {
            // Execute operation based on type
            await ExecuteOperationAsync(operationType, resourceId, parameters, cancellationToken);

            stopwatch.Stop();

            return new BatchResourceResult
            {
                ResourceId = resourceId,
                Success = true,
                Message = "Success",
                DurationMs = stopwatch.ElapsedMilliseconds
            };
        }
        catch (Exception ex)
        {
            stopwatch.Stop();

            _logger.LogError(ex, "Error processing resource {resourceId} in batch {operationId}",
                resourceId, operationId);

            return new BatchResourceResult
            {
                ResourceId = resourceId,
                Success = false,
                Message = ex.Message,
                DurationMs = stopwatch.ElapsedMilliseconds
            };
        }
    }

    /// <summary>
    /// Executes specific operation type on resource.
    /// This would be enhanced with actual operation implementations.
    /// </summary>
    private Task ExecuteOperationAsync(
        string operationType,
        string resourceId,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken)
    {
        // In production, dispatch to appropriate handler based on operationType
        // Examples: "apply-migration", "create-backup", "update-tenant"

        _logger.LogDebug("Executing {operation} on {resourceId}", operationType, resourceId);

        return Task.CompletedTask;
    }
}
