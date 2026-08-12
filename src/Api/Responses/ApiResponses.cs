#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace SqliteMultiTenant.Api.Responses;

/// <summary>
/// Generic API response wrapper for consistent response format.
/// Implements Result pattern to provide status, message, and data in single object.
/// Eliminates HTTP status code ambiguity at application layer.
/// </summary>
public sealed class ApiResponse<T> {
    public int StatusCode { get; set; }
    public bool IsSuccess { get; set; }
    public string Message { get; set; } = string.Empty;
    public T? Data { get; set; }
    public Dictionary<string, string>? Errors { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    public static ApiResponse<T> Success(T data, string message = "Success")
    {
        ArgumentException.ThrowIfNullOrEmpty(message);
        return new() { StatusCode = 200, IsSuccess = true, Data = data, Message = message };
    }

    public static ApiResponse<T> Created(T data, string message = "Created")
    {
        ArgumentException.ThrowIfNullOrEmpty(message);
        return new() { StatusCode = 201, IsSuccess = true, Data = data, Message = message };
    }

    public static ApiResponse<T> BadRequest(string message, Dictionary<string, string>? errors = null)
    {
        ArgumentException.ThrowIfNullOrEmpty(message);
        return new() { StatusCode = 400, IsSuccess = false, Message = message, Errors = errors };
    }

    public static ApiResponse<T> NotFound(string message)
    {
        ArgumentException.ThrowIfNullOrEmpty(message);
        return new() { StatusCode = 404, IsSuccess = false, Message = message };
    }

    public static ApiResponse<T> Conflict(string message)
    {
        ArgumentException.ThrowIfNullOrEmpty(message);
        return new() { StatusCode = 409, IsSuccess = false, Message = message };
    }

    public static ApiResponse<T> InternalServerError(string message)
    {
        ArgumentException.ThrowIfNullOrEmpty(message);
        return new() { StatusCode = 500, IsSuccess = false, Message = message };
    }

    public static ApiResponse<T> Unauthorized(string message = "Unauthorized")
    {
        ArgumentException.ThrowIfNullOrEmpty(message);
        return new() { StatusCode = 401, IsSuccess = false, Message = message };
    }

    public static ApiResponse<T> Forbidden(string message = "Forbidden")
    {
        ArgumentException.ThrowIfNullOrEmpty(message);
        return new() { StatusCode = 403, IsSuccess = false, Message = message };
    }

    /// <summary>
    /// Generic error response. Callers typically wrap this with the appropriate
    /// HTTP status code (e.g. <c>StatusCode(500, ApiResponse&lt;object&gt;.Error(...))</c>).
    /// </summary>
    public static ApiResponse<T> Error(string message)
    {
        ArgumentException.ThrowIfNullOrEmpty(message);
        return new() { IsSuccess = false, Message = message };
    }
}

/// <summary>
/// Response DTO for tenant information.
/// Exposes only safe fields; sensitive data (connection strings) never in responses.
/// </summary>
public sealed class TenantResponse {
    public string TenantId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? LastAccessedAt { get; set; }
}

/// <summary>
/// Response DTO for backup information.
/// Includes all metadata needed for recovery decisions without exposing sensitive paths.
/// </summary>
public sealed class BackupResponse {
    public string BackupId { get; set; } = string.Empty;
    public string DatabaseId { get; set; } = string.Empty;
    public string BackupType { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public long SizeBytes { get; set; }
    public bool IsVerified { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public List<string> Tags { get; set; } = new();
}

/// <summary>
/// Response DTO for individual migration information.
/// Shows version history and rollback capability for schema evolution tracking.
/// </summary>
public sealed class MigrationResponse {
    public string MigrationId { get; set; } = string.Empty;
    public string DatabaseId { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public bool IsRollbackable { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? AppliedAt { get; set; }
}

/// <summary>
/// Response DTO for individual migration failure details.
/// Provides detailed information about a single migration failure.
/// </summary>
public sealed class MigrationFailureResponse
{
    public string MigrationId { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string ErrorMessage { get; set; } = string.Empty;
    public string? ExceptionDetails { get; set; }
    public DateTime FailedAt { get; set; }
    public string ErrorType { get; set; } = "Unknown";
    public string ErrorSummary { get; set; } = string.Empty;

    public static MigrationFailureResponse FromModel(Models.MigrationFailure failure)
    {
        ArgumentNullException.ThrowIfNull(failure);
        ArgumentException.ThrowIfNullOrEmpty(failure.MigrationId);
        ArgumentException.ThrowIfNullOrEmpty(failure.Version);
        ArgumentException.ThrowIfNullOrEmpty(failure.Name);
        ArgumentException.ThrowIfNullOrEmpty(failure.ErrorMessage);

        return new MigrationFailureResponse
        {
            MigrationId = failure.MigrationId,
            Version = failure.Version,
            Name = failure.Name,
            ErrorMessage = failure.ErrorMessage,
            ExceptionDetails = failure.ExceptionDetails,
            FailedAt = failure.FailedAt,
            ErrorType = failure.ExceptionDetails?.Contains("constraint", StringComparison.OrdinalIgnoreCase) == true ? "ConstraintViolation" :
                        failure.ExceptionDetails?.Contains("duplicate", StringComparison.OrdinalIgnoreCase) == true ? "DuplicateKey" :
                        failure.ExceptionDetails?.Contains("timeout", StringComparison.OrdinalIgnoreCase) == true ? "Timeout" :
                        "Unknown",
            ErrorSummary = failure.ExceptionDetails != null ?
                (failure.ExceptionDetails.Contains("constraint", StringComparison.OrdinalIgnoreCase) ? "Database constraint violation" :
                 failure.ExceptionDetails.Contains("duplicate", StringComparison.OrdinalIgnoreCase) ? "Duplicate key or already exists" :
                 failure.ExceptionDetails.Contains("timeout", StringComparison.OrdinalIgnoreCase) ? "Operation timeout" :
                 "Migration failed") : "Migration failed"
        };
    }
}

/// <summary>
/// Response DTO for tenant-specific migration results.
/// Provides detailed results for a single tenant/database migration operation.
/// </summary>
public sealed class TenantMigrationResultResponse
{
    public string DatabaseId { get; set; } = string.Empty;
    public string? TenantId { get; set; }
    public string? DatabaseName { get; set; }
    public int TotalMigrationsAttempted { get; set; }
    public int SuccessfulMigrations { get; set; }
    public int FailedMigrations => TotalMigrationsAttempted - SuccessfulMigrations;
    public bool IsSuccess => FailedMigrations == 0;
    public string? SchemaVersionReached { get; set; }
    public List<MigrationFailureResponse> Failures { get; set; } = new();
    public string ResultSummary => IsSuccess
        ? $"Success: {SuccessfulMigrations}/{TotalMigrationsAttempted} migrations applied"
        : $"Failed: {FailedMigrations} migration(s) failed, schema version reached: {SchemaVersionReached ?? "none"}";

    public static TenantMigrationResultResponse FromModel(Models.TenantMigrationResult result)
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentException.ThrowIfNullOrEmpty(result.DatabaseId);

        return new TenantMigrationResultResponse
        {
            DatabaseId = result.DatabaseId,
            TenantId = result.TenantId,
            DatabaseName = result.DatabaseName,
            TotalMigrationsAttempted = result.TotalMigrationsAttempted,
            SuccessfulMigrations = result.SuccessfulMigrations,
            SchemaVersionReached = result.SchemaVersionReached,
            Failures = result.Failures.Select(MigrationFailureResponse.FromModel).ToList()
        };
    }
}

/// <summary>
/// Response DTO for batch migration operations.
/// Allows clients to track bulk operations across tenants.
/// </summary>
public sealed class MigrationBatchResponse
{
    public string DatabaseId { get; set; } = string.Empty;
    public int TotalMigrations { get; set; }
    public int SuccessfulCount { get; set; }
    public int FailedCount => TotalMigrations - SuccessfulCount;
    public bool IsSuccess { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime AppliedAt { get; set; }
    public string AppliedBy { get; set; } = string.Empty;
    public List<TenantMigrationResultResponse> TenantResults { get; set; } = new();
    public int TotalTenants => TenantResults.Count;
    public int TotalSuccessfulTenants => TenantResults.Count(r => r.IsSuccess);
    public int TotalFailedTenants => TotalTenants - TotalSuccessfulTenants;
}

/// <summary>
/// Response DTO for migration history queries.
/// Used for status dashboards and schema evolution audits.
/// </summary>
public sealed class MigrationHistoryResponse {
    public string DatabaseId { get; set; } = string.Empty;
    public int PendingCount { get; set; }
    public int AppliedCount { get; set; }
    public DateTime? LastMigrationDate { get; set; }
}

/// <summary>
/// Response DTO for health check endpoint.
/// Standardizes monitoring and alerting across all components.
/// </summary>
public sealed class HealthCheckResponse {
    public string Status { get; set; } = string.Empty;
    public DateTime CheckedAt { get; set; } = DateTime.UtcNow;
    public Dictionary<string, ComponentHealth> Components { get; set; } = new();
}

/// <summary>
/// Component-level health status.
/// Enables granular monitoring of database, cache, file system.
/// </summary>
public sealed class ComponentHealth {
    public string Status { get; set; } = string.Empty;
    public string? Message { get; set; }
    public long? ResponseTimeMs { get; set; }
}

/// <summary>
/// Paginated response wrapper for list endpoints.
/// Provides metadata for client-side pagination UI rendering.
/// </summary>
public sealed class PaginatedResponse<T> {
    public List<T> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (TotalCount + PageSize - 1) / PageSize;
}

/// <summary>
/// Response DTO for async operation status.
/// Clients poll this endpoint to track long-running operations (backups, migrations).
/// </summary>
public sealed class AsyncOperationResponse {
    public string OperationId { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public int? ProgressPercentage { get; set; }
    public string? ResultId { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
}

/// <summary>
/// Error response DTO for validation and business rule violations.
/// Multiple errors returned together to improve client user experience.
/// </summary>
public sealed class ErrorResponse {
    public string Code { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public Dictionary<string, string>? Details { get; set; }
    public string? TraceId { get; set; }
}

/// <summary>
/// Response DTO for tenant quota information.
/// Includes tenant id, used bytes, quota bytes, and usage percentage.
/// </summary>
public sealed class TenantQuotaReport
{
    public string TenantId { get; set; } = string.Empty;
    public long UsedBytes { get; set; }
    public long? QuotaBytes { get; set; }
    public double UsagePercent { get; set; }
}

/// <summary>
/// Response DTO for aggregated tenant quota report.
/// Includes summary statistics and individual tenant reports.
/// </summary>
public sealed class TenantQuotaSummaryReport
{
    public long TotalUsedBytes { get; set; }
    public long TotalQuotaBytes { get; set; }
    public double OverallUsagePercent { get; set; }
    public int TotalTenants { get; set; }
    public int TenantsOverQuota { get; set; }
    public int TenantsNearQuota { get; set; }
    public List<TenantQuotaReport> TenantReports { get; set; } = new();
}