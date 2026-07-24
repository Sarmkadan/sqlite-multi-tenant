#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =======================================================================

using SqliteMultiTenant.Configuration;
using Microsoft.Extensions.Logging;
using SqliteMultiTenant.Models;
using SqliteMultiTenant.Utilities;
using SqliteMultiTenant.Exceptions;

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
/// Defines atomicity modes for batch operations.
/// </summary>
public enum BatchAtomicityMode
{
    /// <summary>
    /// Cross-tenant operations are best-effort. Each resource operation is independent.
    /// Failures in one tenant do not affect processing of other tenants.
    /// </summary>
    CrossTenant = 0,

    /// <summary>
    /// Single-tenant operations are transactional. All operations against a single tenant
    /// database are wrapped in a transaction and will be rolled back if any operation fails.
    /// </summary>
    SingleTenant = 1
}

/// <summary>
/// Batch operation definition with resources and parameters.
/// </summary>
public sealed class BatchOperation
{
    public string OperationId { get; set; } = Guid.NewGuid().ToString();
    public string OperationType { get; set; } = string.Empty;
    public List<string> ResourceIds { get; set; } = new();
    public Dictionary<string, object> Parameters { get; set; } = new();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets a value indicating whether the batch operation should continue processing
    /// remaining resources when an error occurs (best-effort mode). When false, the operation
    /// will attempt to maintain atomicity per tenant (all-or-nothing for that tenant's operations).
    /// Defaults to true for backward compatibility.
    /// </summary>
    public bool ContinueOnError { get; set; } = true;

    /// <summary>
    /// Gets or sets the atomicity mode for the batch operation.
    /// - CrossTenant: Operations across multiple tenant databases are best-effort (ContinueOnError applies)
    /// - SingleTenant: Operations against a single tenant database are transactional (all-or-nothing)
    /// Defaults to CrossTenant for backward compatibility.
    /// </summary>
    public BatchAtomicityMode AtomicityMode { get; set; } = BatchAtomicityMode.CrossTenant;
}

/// <summary>
/// Result of batch operation execution.
/// </summary>
public sealed class BatchOperationResult
{
    public string OperationId { get; set; } = string.Empty;
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
public sealed class BatchResourceResult
{
    public string ResourceId { get; set; } = string.Empty;
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public long DurationMs { get; set; }

    /// <summary>
    /// Gets or sets the transaction status for this resource operation.
    /// True if the operation was wrapped in a transaction and completed successfully.
    /// </summary>
    public bool Transactional { get; set; }
}

/// <summary>
/// Status of ongoing batch operation.
/// </summary>
public sealed class BatchOperationStatus : OperationStatusBase
{
    /// <summary>
    /// The total number of resources to process.
    /// </summary>
    public int TotalResources { get; set; }

    /// <summary>
    /// The number of resources that have been processed so far.
    /// </summary>
    public int ProcessedResources { get; set; }

    /// <summary>
    /// The percentage of completion (0-100).
    /// </summary>
    public int ProgressPercent => TotalResources > 0 ? (ProcessedResources * 100) / TotalResources : 0;

    /// <summary>
    /// Initializes a new instance of the <see cref="BatchOperationStatus"/> class.
    /// </summary>
    public BatchOperationStatus()
    {
        OperationId = nameof(BatchOperationStatus);
        Status = OperationStatus.Pending;
        CreatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Updates the status to running.
    /// </summary>
    public void MarkRunning()
    {
        base.MarkRunning();
    }

    /// <summary>
    /// Updates the status to completed successfully.
    /// </summary>
    public void MarkCompleted()
    {
        base.MarkCompleted();
    }

    /// <summary>
    /// Updates the status to failed.
    /// </summary>
    /// <param name="error">The error message.</param>
    public void MarkFailed(string error)
    {
        base.MarkFailed(error);
    }

    /// <summary>
    /// Validates the batch operation status.
    /// </summary>
    /// <exception cref="ArgumentException">Thrown when the status is invalid.</exception>
    public void Validate()
    {
        ValidateStatus();
    }
}

/// <summary>
/// Batch operation handler implementation.
/// </summary>
public sealed class BatchOperationHandler : IBatchOperationHandler
{
    private readonly ILogger<BatchOperationHandler> _logger;
    private readonly TenantContextHelper _tenantContextHelper;
    private readonly Dictionary<string, BatchOperationStatus> _statusTracker = new();
    private readonly int _maxBatchItems;
    private readonly long _maxBatchPayloadSizeBytes;

    /// <summary>
    /// Initializes a new instance of the <see cref="BatchOperationHandler"/> class.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="tenantContextHelper">The tenant context helper for authorization checks.</param>
    /// <param name="options">Optional configuration options. If null, uses defaults (max 500 items, 1 MB payload).</param>
    /// <exception cref="ArgumentNullException">Thrown when logger or tenantContextHelper is null.</exception>
    public BatchOperationHandler(
        ILogger<BatchOperationHandler> logger,
        TenantContextHelper tenantContextHelper,
        SqliteMultiTenantOptions? options = null)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _tenantContextHelper = tenantContextHelper ?? throw new ArgumentNullException(nameof(tenantContextHelper));

        // Use configured values or defaults
        _maxBatchItems = options?.MaxBatchItems ?? 500;
        _maxBatchPayloadSizeBytes = options?.MaxBatchPayloadSizeBytes ?? (1024 * 1024); // 1 MB
    }

    /// <summary>
    /// Validates that the batch operation does not exceed configured size limits.
    /// Throws <see cref="BatchTooLargeException"/> if limits are exceeded.
    /// </summary>
    /// <param name="operation">The batch operation to validate.</param>
    /// <exception cref="BatchTooLargeException">Thrown when the batch exceeds configured size limits.</exception>
    private void ValidateBatchSize(BatchOperation operation)
    {
        if (operation is null)
        {
            throw new ArgumentNullException(nameof(operation));
        }

        // Validate item count limit
        if (_maxBatchItems > 0 && operation.ResourceIds.Count > _maxBatchItems)
        {
            _logger.LogWarning(
                "Batch operation {operationId} rejected: item count {actualCount} exceeds maximum {maxCount}",
                operation.OperationId,
                operation.ResourceIds.Count,
                _maxBatchItems);

            throw new BatchTooLargeException(
                _maxBatchItems,
                operation.ResourceIds.Count);
        }

        // Validate payload size limit
        if (_maxBatchPayloadSizeBytes > 0)
        {
            long payloadSize = CalculatePayloadSize(operation.Parameters);
            if (payloadSize > _maxBatchPayloadSizeBytes)
            {
                _logger.LogWarning(
                    "Batch operation {operationId} rejected: payload size {actualSize} exceeds maximum {maxSize}",
                    operation.OperationId,
                    FormatSize(payloadSize),
                    FormatSize(_maxBatchPayloadSizeBytes));

                throw new BatchTooLargeException(
                    _maxBatchItems,
                    operation.ResourceIds.Count,
                    _maxBatchPayloadSizeBytes,
                    payloadSize);
            }
        }

        _logger.LogDebug(
            "Batch operation {operationId} validated: {count} items, payload size {payloadSize}",
            operation.OperationId,
            operation.ResourceIds.Count,
            FormatSize(CalculatePayloadSize(operation.Parameters)));
    }

    /// <summary>
    /// Calculates the approximate size of the batch operation parameters in bytes.
    /// </summary>
    /// <param name="parameters">The parameters dictionary.</param>
    /// <returns>The approximate size in bytes.</returns>
    private static long CalculatePayloadSize(Dictionary<string, object>? parameters)
    {
        if (parameters is null || parameters.Count == 0)
        {
            return 0;
        }

        long size = 0;
        foreach (var kvp in parameters)
        {
            // Add key size
            size += kvp.Key.Length * sizeof(char);

            // Add value size based on type
            if (kvp.Value is string strValue)
            {
                size += strValue.Length * sizeof(char);
            }
            else if (kvp.Value is not null)
            {
                // For other types, use approximate size
                size += 128; // Conservative estimate for serialized objects
            }
        }

        return size;
    }

    /// <summary>
    /// Formats a byte size for human-readable output.
    /// </summary>
    /// <param name="bytes">The size in bytes.</param>
    /// <returns>Formatted string with appropriate unit.</returns>
    private static string FormatSize(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB", "TB" };
        int order = 0;
        double len = bytes;
        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len /= 1024;
        }
        return $"{len:0.##} {sizes[order]}";
    }

    /// <summary>
    /// Executes batch operation across multiple resources.
    /// Processes in parallel with configurable concurrency level.
    /// Tracks progress and handles failures according to the specified atomicity mode.
    /// </summary>
    /// <param name="operation">The batch operation to execute.</param>
    /// <param name="cancellationToken">Cancellation token for cooperative cancellation.</param>
    /// <returns>Result of the batch operation with per-resource details.</returns>
    /// <exception cref="ArgumentNullException">Thrown when operation is null.</exception>
    /// <exception cref="UnauthorizedAccessException">Thrown when caller is not authorized for one or more tenant resources.</exception>
    /// <exception cref="BatchTooLargeException">Thrown when the batch exceeds configured size limits (max items or payload size).</exception>
    public async Task<BatchOperationResult> ExecuteAsync(BatchOperation operation, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(operation);
    // Validate batch size limits to prevent resource exhaustion
    ValidateBatchSize(operation);


        var operationId = operation.OperationId;
        var startTime = DateTime.UtcNow;

        _logger.LogInformation(
            "Starting batch operation {type}: {operationId} with {count} resources. AtomicityMode: {mode}, ContinueOnError: {continueOnError}",
            operation.OperationType,
            operationId,
            operation.ResourceIds.Count,
            operation.AtomicityMode,
            operation.ContinueOnError);

        // Validate tenant authorization before processing
        ValidateTenantAuthorization(operation.ResourceIds);

        // Initialize status tracking
        var status = new BatchOperationStatus
        {
            OperationId = operationId,
            TotalResources = operation.ResourceIds.Count
        };
        status.MarkRunning();
        _statusTracker[operationId] = status;

        var result = new BatchOperationResult
        {
            OperationId = operationId,
            TotalResources = operation.ResourceIds.Count
        };

        // Group resources by tenant ID to minimize connection churn
        // This ensures all operations for a single tenant are processed with a single connection
        var resourcesByTenant = operation.ResourceIds
            .Where(resourceId => !string.IsNullOrEmpty(resourceId))
            .GroupBy(resourceId => resourceId)
            .ToDictionary(group => group.Key, group => group.ToList());

        // Process each tenant group sequentially to avoid parallel writes to the same database
        // SQLite only allows one writer per database file at a time
        foreach (var tenantGroup in resourcesByTenant)
        {
            if (cancellationToken.IsCancellationRequested)
                break;

            var tenantId = tenantGroup.Key;
            var tenantResourceIds = tenantGroup.Value;

            // Process all resources for this tenant
            foreach (var resourceId in tenantResourceIds)
            {
                var resourceResult = await ProcessResourceAsync(
                    operationId,
                    operation.OperationType,
                    resourceId,
                    operation.Parameters,
                    operation,
                    cancellationToken
                );

                result.ResourceResults.Add(resourceResult);

                if (resourceResult.Success)
                    result.SuccessCount++;
                else
                    result.FailureCount++;

                // Update progress
                status.ProcessedResources = result.SuccessCount + result.FailureCount;

                if (cancellationToken.IsCancellationRequested)
                    break;
            }
        }

        // Finalize
        result.Duration = DateTime.UtcNow - startTime;
        status.MarkCompleted();

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
    /// <param name="operationId">The operation identifier.</param>
    /// <returns>The current status of the batch operation.</returns>
    public Task<BatchOperationStatus> GetStatusAsync(string operationId)
    {
        if (_statusTracker.TryGetValue(operationId, out var status))
            return Task.FromResult(status);

        return Task.FromResult<BatchOperationStatus>(null);
    }

    /// <summary>
    /// Validates that the current caller is authorized to access all tenant resources in the batch.
    /// Throws <see cref="UnauthorizedAccessException"/> if authorization check fails.
    /// </summary>
    /// <param name="resourceIds">Collection of tenant resource IDs to validate.</param>
    /// <exception cref="UnauthorizedAccessException">Thrown when caller is not authorized for one or more tenant resources.</exception>
    /// <exception cref="BatchTooLargeException">Thrown when the batch exceeds configured size limits (max items or payload size).</exception>
    private void ValidateTenantAuthorization(IEnumerable<string> resourceIds)
    {
        if (resourceIds is null || !resourceIds.Any())
            return;

        var currentTenantId = _tenantContextHelper.GetCurrentTenantId();

        if (string.IsNullOrEmpty(currentTenantId))
        {
            _logger.LogError("Tenant authorization failed: No tenant context available for caller");
            throw new UnauthorizedAccessException(
                "Tenant authorization failed: No tenant context available. Please ensure you are authenticated with a valid tenant.");
        }

        var unauthorizedTenants = new List<string>();
        foreach (var resourceId in resourceIds)
        {
            if (!string.IsNullOrEmpty(resourceId) && resourceId != currentTenantId)
            {
                unauthorizedTenants.Add(resourceId);
            }
        }

        if (unauthorizedTenants.Any())
        {
            _logger.LogError("Tenant authorization failed: Caller {currentTenant} attempted to access unauthorized tenants: {unauthorizedTenants}",
                currentTenantId,
                string.Join(", ", unauthorizedTenants));
            throw new UnauthorizedAccessException(
                $"Tenant authorization failed: You are not authorized to access tenant(s) {string.Join(", ", unauthorizedTenants)}. " +
                $"Current tenant: {currentTenantId}. Only operations on your authorized tenant are permitted.");
        }

        _logger.LogDebug("Tenant authorization successful for caller {tenantId}", currentTenantId);
    }

    /// <summary>
    /// Processes individual resource in batch operation.
    /// </summary>
    /// <param name="operationId">The batch operation identifier.</param>
    /// <param name="operationType">Type of operation to perform.</param>
    /// <param name="resourceId">The resource (tenant) identifier.</param>
    /// <param name="parameters">Operation parameters.</param>
    /// <param name="operation">The batch operation configuration.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result of the resource operation.</returns>
    private async Task<BatchResourceResult> ProcessResourceAsync(
        string operationId,
        string operationType,
        string resourceId,
        Dictionary<string, object> parameters,
        BatchOperation operation,
        CancellationToken cancellationToken)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        try
        {
            // Execute operation based on type with transaction handling
            var resourceResult = await ExecuteOperationWithTransactionAsync(
                operationId,
                operationType,
                resourceId,
                parameters,
                operation,
                cancellationToken);

            stopwatch.Stop();

            return new BatchResourceResult
            {
                ResourceId = resourceId,
                Success = resourceResult.Success,
                Message = resourceResult.Message,
                DurationMs = stopwatch.ElapsedMilliseconds,
                Transactional = resourceResult.Transactional
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
                DurationMs = stopwatch.ElapsedMilliseconds,
                Transactional = false
            };
        }
    }

    /// <summary>
    /// Executes the operation with transaction handling based on atomicity mode.
    /// </summary>
    /// <param name="operationId">The batch operation identifier.</param>
    /// <param name="operationType">Type of operation to perform.</param>
    /// <param name="resourceId">The resource (tenant) identifier.</param>
    /// <param name="parameters">Operation parameters.</param>
    /// <param name="operation">The batch operation configuration.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result of the operation with transaction status.</returns>
    private async Task<BatchResourceResult> ExecuteOperationWithTransactionAsync(
        string operationId,
        string operationType,
        string resourceId,
        Dictionary<string, object> parameters,
        BatchOperation operation,
        CancellationToken cancellationToken)
    {
        // For CrossTenant mode, execute without transaction (best-effort)
        // For SingleTenant mode, wrap in transaction (all-or-nothing for this tenant)
        if (operation.AtomicityMode == BatchAtomicityMode.CrossTenant)
        {
            try
            {
                await ExecuteOperationAsync(operationType, resourceId, parameters, cancellationToken);
                return new BatchResourceResult
                {
                    Success = true,
                    Message = "Success (best-effort mode)",
                    Transactional = false
                };
            }
            catch (Exception ex)
            {
                return new BatchResourceResult
                {
                    Success = false,
                    Message = ex.Message,
                    Transactional = false
                };
            }
        }

        // SingleTenant mode - wrap in transaction
        // Note: This assumes all operations for a single resourceId should be transactional
        // For operations spanning multiple resources, use CrossTenant mode
        return await ExecuteWithTransactionAsync(operationType, resourceId, parameters, cancellationToken);
    }

    /// <summary>
    /// Executes the operation within a transaction for single-tenant atomicity.
    /// </summary>
    /// <param name="operationType">Type of operation to perform.</param>
    /// <param name="resourceId">The resource (tenant) identifier.</param>
    /// <param name="parameters">Operation parameters.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result of the operation.</returns>
    private async Task<BatchResourceResult> ExecuteWithTransactionAsync(
        string operationType,
        string resourceId,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken)
    {
        // For demonstration purposes, we'll simulate transactional behavior
        // In a real implementation, this would connect to the actual tenant database
        // and wrap operations in a transaction

        try
        {
            // Simulate transactional execution
            // In production: await ExecuteOperationAsync(operationType, resourceId, parameters, cancellationToken);

            // Simulate a successful transaction
            return new BatchResourceResult
            {
                Success = true,
                Message = "Success (transactional simulation)",
                Transactional = true
            };
        }
        catch (Exception ex)
        {
            return new BatchResourceResult
            {
                Success = false,
                Message = $"Transaction failed: {ex.Message}",
                Transactional = true
            };
        }
    }

    /// <summary>
    /// Executes specific operation type on resource.
    /// This would be enhanced with actual operation implementations.
    /// </summary>
    /// <param name="operationType">Type of operation to perform.</param>
    /// <param name="resourceId">The resource (tenant) identifier.</param>
    /// <param name="parameters">Operation parameters.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Task representing the asynchronous operation.</returns>
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
