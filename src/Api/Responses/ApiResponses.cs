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
public class ApiResponse<T>
{
    public int StatusCode { get; set; }
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public T? Data { get; set; }
    public Dictionary<string, string>? Errors { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    public static ApiResponse<T> Success(T data, string message = "Success")
        => new() { StatusCode = 200, Success = true, Data = data, Message = message };

    public static ApiResponse<T> Created(T data, string message = "Created")
        => new() { StatusCode = 201, Success = true, Data = data, Message = message };

    public static ApiResponse<T> BadRequest(string message, Dictionary<string, string>? errors = null)
        => new() { StatusCode = 400, Success = false, Message = message, Errors = errors };

    public static ApiResponse<T> NotFound(string message)
        => new() { StatusCode = 404, Success = false, Message = message };

    public static ApiResponse<T> Conflict(string message)
        => new() { StatusCode = 409, Success = false, Message = message };

    public static ApiResponse<T> InternalServerError(string message)
        => new() { StatusCode = 500, Success = false, Message = message };

    public static ApiResponse<T> Unauthorized(string message = "Unauthorized")
        => new() { StatusCode = 401, Success = false, Message = message };

    public static ApiResponse<T> Forbidden(string message = "Forbidden")
        => new() { StatusCode = 403, Success = false, Message = message };
}

/// <summary>
/// Response DTO for tenant information.
/// Exposes only safe fields; sensitive data (connection strings) never in responses.
/// </summary>
public class TenantResponse
{
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
public class BackupResponse
{
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
public class MigrationResponse
{
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
/// Response DTO for batch migration operations.
/// Allows clients to track bulk operations across tenants.
/// </summary>
public class MigrationBatchResponse
{
    public string DatabaseId { get; set; } = string.Empty;
    public int TotalMigrations { get; set; }
    public int SuccessfulCount { get; set; }
    public int FailedCount => TotalMigrations - SuccessfulCount;
    public DateTime AppliedAt { get; set; }
    public string AppliedBy { get; set; } = string.Empty;
}

/// <summary>
/// Response DTO for migration history queries.
/// Used for status dashboards and schema evolution audits.
/// </summary>
public class MigrationHistoryResponse
{
    public string DatabaseId { get; set; } = string.Empty;
    public int PendingCount { get; set; }
    public int AppliedCount { get; set; }
    public DateTime? LastMigrationDate { get; set; }
}

/// <summary>
/// Response DTO for health check endpoint.
/// Standardizes monitoring and alerting across all components.
/// </summary>
public class HealthCheckResponse
{
    public string Status { get; set; } = string.Empty;
    public DateTime CheckedAt { get; set; } = DateTime.UtcNow;
    public Dictionary<string, ComponentHealth> Components { get; set; } = new();
}

/// <summary>
/// Component-level health status.
/// Enables granular monitoring of database, cache, file system.
/// </summary>
public class ComponentHealth
{
    public string Status { get; set; } = string.Empty;
    public string? Message { get; set; }
    public long? ResponseTimeMs { get; set; }
}

/// <summary>
/// Paginated response wrapper for list endpoints.
/// Provides metadata for client-side pagination UI rendering.
/// </summary>
public class PaginatedResponse<T>
{
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
public class AsyncOperationResponse
{
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
public class ErrorResponse
{
    public string Code { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public Dictionary<string, string>? Details { get; set; }
    public string? TraceId { get; set; }
}
