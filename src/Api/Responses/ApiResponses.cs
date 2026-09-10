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
    /// <summary>Gets or sets the StatusCode property of type int.</summary>
    public int StatusCode { get; set; }
    /// <summary>Gets or sets the IsSuccess property of type bool.</summary>
    public bool IsSuccess { get; set; }
    /// <summary>Gets or sets the Message property of type string, defaulting to string.Empty.</summary>
    public string Message { get; set; } = string.Empty;
    /// <summary>Gets or sets the Data property of type T?.</summary>
    public T? Data { get; set; }
    /// <summary>Gets or sets the Errors property of type Dictionary&lt;string, string&gt;?.</summary>
    public Dictionary<string, string>? Errors { get; set; }
    /// <summary>Gets or sets the Timestamp property of type DateTime, defaulting to DateTime.UtcNow.</summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>Creates a success response with the specified data and message.</summary>
    public static ApiResponse<T> Success(T data, string message = "Success")
    {
        ArgumentException.ThrowIfNullOrEmpty(message);
        return new() { StatusCode = 200, IsSuccess = true, Data = data, Message = message };
    }

    /// <summary>Creates a created response with the specified data and message.</summary>
    public static ApiResponse<T> Created(T data, string message = "Created")
    {
        ArgumentException.ThrowIfNullOrEmpty(message);
        return new() { StatusCode = 201, IsSuccess = true, Data = data, Message = message };
    }

    /// <summary>Creates a bad request response with the specified message and optional errors.</summary>
    public static ApiResponse<T> BadRequest(string message, Dictionary<string, string>? errors = null)
    {
        ArgumentException.ThrowIfNullOrEmpty(message);
        return new() { StatusCode = 400, IsSuccess = false, Message = message, Errors = errors };
    }

    /// <summary>Creates a not found response with the specified message.</summary>
    public static ApiResponse<T> NotFound(string message)
    {
        ArgumentException.ThrowIfNullOrEmpty(message);
        return new() { StatusCode = 404, IsSuccess = false, Message = message };
    }

    /// <summary>Creates a conflict response with the specified message.</summary>
    public static ApiResponse<T> Conflict(string message)
    {
        ArgumentException.ThrowIfNullOrEmpty(message);
        return new() { StatusCode = 409, IsSuccess = false, Message = message };
    }

    /// <summary>Creates an internal server error response with the specified message.</summary>
    public static ApiResponse<T> InternalServerError(string message)
    {
        ArgumentException.ThrowIfNullOrEmpty(message);
        return new() { StatusCode = 500, IsSuccess = false, Message = message };
    }

    /// <summary>Creates an unauthorized response with the specified message.</summary>
    public static ApiResponse<T> Unauthorized(string message = "Unauthorized")
    {
        ArgumentException.ThrowIfNullOrEmpty(message);
        return new() { StatusCode = 401, IsSuccess = false, Message = message };
    }

    /// <summary>Creates a forbidden response with the specified message.</summary>
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

    /// <summary>Returns a string representation of the ApiResponse instance.</summary>
    public override string ToString() => $"ApiResponse {{ StatusCode = {StatusCode}, IsSuccess = {IsSuccess}, Message = {Message}, Data = {Data}, Errors = {Errors}, Timestamp = {Timestamp} }}";
}

/// <summary>
/// Response DTO for tenant information.
/// Exposes only safe fields; sensitive data (connection strings) never in responses.
/// </summary>
public sealed class TenantResponse {
    /// <summary>Gets or sets the TenantId property of type string, defaulting to string.Empty.</summary>
    public string TenantId { get; set; } = string.Empty;
    /// <summary>Gets or sets the Name property of type string, defaulting to string.Empty.</summary>
    public string Name { get; set; } = string.Empty;
    /// <summary>Gets or sets the Status property of type string, defaulting to string.Empty.</summary>
    public string Status { get; set; } = string.Empty;
    /// <summary>Gets or sets the CreatedAt property of type DateTime.</summary>
    public DateTime CreatedAt { get; set; }
    /// <summary>Gets or sets the LastAccessedAt property of type DateTime?.</summary>
    public DateTime? LastAccessedAt { get; set; }
}

/// <summary>
/// Response DTO for backup information.
/// Includes all metadata needed for recovery decisions without exposing sensitive paths.
/// </summary>
public sealed class BackupResponse {
    /// <summary>Gets or sets the BackupId property of type string, defaulting to string.Empty.</summary>
    public string BackupId { get; set; } = string.Empty;
    /// <summary>Gets or sets the DatabaseId property of type string, defaulting to string.Empty.</summary>
    public string DatabaseId { get; set; } = string.Empty;
    /// <summary>Gets or sets the BackupType property of type string, defaulting to string.Empty.</summary>
    public string BackupType { get; set; } = string.Empty;
    /// <summary>Gets or sets the Status property of type string, defaulting to string.Empty.</summary>
    public string Status { get; set; } = string.Empty;
    /// <summary>Gets or sets the SizeBytes property of type long.</summary>
    public long SizeBytes { get; set; }
    /// <summary>Gets or sets the IsVerified property of type bool.</summary>
    public bool IsVerified { get; set; }
    /// <summary>Gets or sets the CreatedAt property of type DateTime.</summary>
    public DateTime CreatedAt { get; set; }
    /// <summary>Gets or sets the ExpiresAt property of type DateTime.</summary>
    public DateTime ExpiresAt { get; set; }
    /// <summary>Gets or sets the Tags property of type List&lt;string&gt;, defaulting to a new list.</summary>
    public List<string> Tags { get; set; } = new();
}

/// <summary>
/// Response DTO for individual migration information.
/// Shows version history and rollback capability for schema evolution tracking.
/// </summary>
public sealed class MigrationResponse {
    /// <summary>Gets or sets the MigrationId property of type string, defaulting to string.Empty.</summary>
    public string MigrationId { get; set; } = string.Empty;
    /// <summary>Gets or sets the DatabaseId property of type string, defaulting to string.Empty.</summary>
    public string DatabaseId { get; set; } = string.Empty;
    /// <summary>Gets or sets the Version property of type string, defaulting to string.Empty.</summary>
    public string Version { get; set; } = string.Empty;
    /// <summary>Gets or sets the Name property of type string, defaulting to string.Empty.</summary>
    public string Name { get; set; } = string.Empty;
    /// <summary>Gets or sets the Status property of type string, defaulting to string.Empty.</summary>
    public string Status { get; set; } = string.Empty;
    /// <summary>Gets or sets the IsRollbackable property of type bool.</summary>
    public bool IsRollbackable { get; set; }
    /// <summary>Gets or sets the CreatedAt property of type DateTime.</summary>
    public DateTime CreatedAt { get; set; }
    /// <summary>Gets or sets the AppliedAt property of type DateTime?.</summary>
    public DateTime? AppliedAt { get; set; }
}

/// <summary>
/// Response DTO for individual migration failure details.
/// Provides detailed information about a single migration failure.
/// </summary>
public sealed class MigrationFailureResponse
{
    /// <summary>Gets or sets the MigrationId property of type string, defaulting to string.Empty.</summary>
    public string MigrationId { get; set; } = string.Empty;
    /// <summary>Gets or sets the Version property of type string, defaulting to string.Empty.</summary>
    public string Version { get; set; } = string.Empty;
    /// <summary>Gets or sets the Name property of type string, defaulting to string.Empty.</summary>
    public string Name { get; set; } = string.Empty;
    /// <summary>Gets or sets the ErrorMessage property of type string, defaulting to string.Empty.</summary>
    public string ErrorMessage { get; set; } = string.Empty;
    /// <summary>Gets or sets the ExceptionDetails property of type string?.</summary>
    public string? ExceptionDetails { get; set; }
    /// <summary>Gets or sets the FailedAt property of type DateTime.</summary>
    public DateTime FailedAt { get; set; }
    /// <summary>Gets or sets the ErrorType property of type string, defaulting to "Unknown".</summary>
    public string ErrorType { get; set; } = "Unknown";
    /// <summary>Gets or sets the ErrorSummary property of type string, defaulting to string.Empty.</summary>
    public string ErrorSummary { get; set; } = string.Empty;

    /// <summary>Creates a MigrationFailureResponse from a MigrationFailure model.</summary>
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
    /// <summary>Gets or sets the DatabaseId property of type string, defaulting to string.Empty.</summary>
    public string DatabaseId { get; set; } = string.Empty;
    /// <summary>Gets or sets the TenantId property of type string?.</summary>
    public string? TenantId { get; set; }
    /// <summary>Gets or sets the DatabaseName property of type string?.</summary>
    public string? DatabaseName { get; set; }
    /// <summary>Gets or sets the TotalMigrationsAttempted property of type int.</summary>
    public int TotalMigrationsAttempted { get; set; }
    /// <summary>Gets or sets the SuccessfulMigrations property of type int.</summary>
    public int SuccessfulMigrations { get; set; }
    /// <summary>Gets the FailedMigrations property of type int, calculated as TotalMigrationsAttempted minus SuccessfulMigrations.</summary>
    public int FailedMigrations => TotalMigrationsAttempted - SuccessfulMigrations;
    /// <summary>Gets the IsSuccess property of type bool, true if FailedMigrations is 0.</summary>
    public bool IsSuccess => FailedMigrations == 0;
    /// <summary>Gets or sets the SchemaVersionReached property of type string?.</summary>
    public string? SchemaVersionReached { get; set; }
    /// <summary>Gets or sets the Failures property of type List&lt;MigrationFailureResponse&gt;, defaulting to a new list.</summary>
    public List<MigrationFailureResponse> Failures { get; set; } = new();
    /// <summary>Gets the ResultSummary property of type string, providing a summary of the migration result.</summary>
    public string ResultSummary => IsSuccess
        ? $"Success: {SuccessfulMigrations}/{TotalMigrationsAttempted} migrations applied"
        : $"Failed: {FailedMigrations} migration(s) failed, schema version reached: {SchemaVersionReached ?? "none"}";

    /// <summary>Creates a TenantMigrationResultResponse from a TenantMigrationResult model.</summary>
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
    /// <summary>Gets or sets the DatabaseId property of type string, defaulting to string.Empty.</summary>
    public string DatabaseId { get; set; } = string.Empty;
    /// <summary>Gets or sets the TotalMigrations property of type int.</summary>
    public int TotalMigrations { get; set; }
    /// <summary>Gets or sets the SuccessfulCount property of type int.</summary>
    public int SuccessfulCount { get; set; }
    /// <summary>Gets the FailedCount property of type int, calculated as TotalMigrations minus SuccessfulCount.</summary>
    public int FailedCount => TotalMigrations - SuccessfulCount;
    /// <summary>Gets or sets the IsSuccess property of type bool.</summary>
    public bool IsSuccess { get; set; }
    /// <summary>Gets or sets the ErrorMessage property of type string?.</summary>
    public string? ErrorMessage { get; set; }
    /// <summary>Gets or sets the AppliedAt property of type DateTime.</summary>
    public DateTime AppliedAt { get; set; }
    /// <summary>Gets or sets the AppliedBy property of type string, defaulting to string.Empty.</summary>
    public string AppliedBy { get; set; } = string.Empty;
    /// <summary>Gets or sets the TenantResults property of type List&lt;TenantMigrationResultResponse&gt;, defaulting to a new list.</summary>
    public List<TenantMigrationResultResponse> TenantResults { get; set; } = new();
    /// <summary>Gets the TotalTenants property of type int, equal to the count of TenantResults.</summary>
    public int TotalTenants => TenantResults.Count;
    /// <summary>Gets the TotalSuccessfulTenants property of type int, counting tenants where IsSuccess is true.</summary>
    public int TotalSuccessfulTenants => TenantResults.Count(r => r.IsSuccess);
    /// <summary>Gets the TotalFailedTenants property of type int, calculated as TotalTenants minus TotalSuccessfulTenants.</summary>
    public int TotalFailedTenants => TotalTenants - TotalSuccessfulTenants;
}

/// <summary>
/// Response DTO for migration history queries.
/// Used for status dashboards and schema evolution audits.
/// </summary>
public sealed class MigrationHistoryResponse {
    /// <summary>Gets or sets the DatabaseId property of type string, defaulting to string.Empty.</summary>
    public string DatabaseId { get; set; } = string.Empty;
    /// <summary>Gets or sets the PendingCount property of type int.</summary>
    public int PendingCount { get; set; }
    /// <summary>Gets or sets the AppliedCount property of type int.</summary>
    public int AppliedCount { get; set; }
    /// <summary>Gets or sets the LastMigrationDate property of type DateTime?.</summary>
    public DateTime? LastMigrationDate { get; set; }
}

/// <summary>
/// Response DTO for health check endpoint.
/// Standardizes monitoring and alerting across all components.
/// </summary>
public sealed class HealthCheckResponse {
    /// <summary>Gets or sets the Status property of type string, defaulting to string.Empty.</summary>
    public string Status { get; set; } = string.Empty;
    /// <summary>Gets or sets the CheckedAt property of type DateTime, defaulting to DateTime.UtcNow.</summary>
    public DateTime CheckedAt { get; set; } = DateTime.UtcNow;
    /// <summary>Gets or sets the Components property of type Dictionary&lt;string, ComponentHealth&gt;, defaulting to a new dictionary.</summary>
    public Dictionary<string, ComponentHealth> Components { get; set; } = new();
}

/// <summary>
/// Component-level health status.
/// Enables granular monitoring of database, cache, file system.
/// </summary>
public sealed class ComponentHealth {
    /// <summary>Gets or sets the Status property of type string, defaulting to string.Empty.</summary>
    public string Status { get; set; } = string.Empty;
    /// <summary>Gets or sets the Message property of type string?.</summary>
    public string? Message { get; set; }
    /// <summary>Gets or sets the ResponseTimeMs property of type long?.</summary>
    public long? ResponseTimeMs { get; set; }
}

/// <summary>
/// Paginated response wrapper for list endpoints.
/// Provides metadata for client-side pagination UI rendering.
/// </summary>
public sealed class PaginatedResponse<T> {
    /// <summary>Gets or sets the Items property of type List&lt;T&gt;, defaulting to a new list.</summary>
    public List<T> Items { get; set; } = new();
    /// <summary>Gets or sets the TotalCount property of type int.</summary>
    public int TotalCount { get; set; }
    /// <summary>Gets or sets the PageNumber property of type int.</summary>
    public int PageNumber { get; set; }
    /// <summary>Gets or sets the PageSize property of type int.</summary>
    public int PageSize { get; set; }
    /// <summary>Gets the TotalPages property of type int, calculated based on TotalCount and PageSize.</summary>
    public int TotalPages => (TotalCount + PageSize - 1) / PageSize;
}

/// <summary>
/// Response DTO for async operation status.
/// Clients poll this endpoint to track long-running operations (backups, migrations).
/// </summary>
public sealed class AsyncOperationResponse {
    /// <summary>Gets or sets the OperationId property of type string, defaulting to string.Empty.</summary>
    public string OperationId { get; set; } = string.Empty;
    /// <summary>Gets or sets the Status property of type string, defaulting to string.Empty.</summary>
    public string Status { get; set; } = string.Empty;
    /// <summary>Gets or sets the ProgressPercentage property of type int?.</summary>
    public int? ProgressPercentage { get; set; }
    /// <summary>Gets or sets the ResultId property of type string?.</summary>
    public string? ResultId { get; set; }
    /// <summary>Gets or sets the ErrorMessage property of type string?.</summary>
    public string? ErrorMessage { get; set; }
    /// <summary>Gets or sets the CreatedAt property of type DateTime.</summary>
    public DateTime CreatedAt { get; set; }
    /// <summary>Gets or sets the CompletedAt property of type DateTime?.</summary>
    public DateTime? CompletedAt { get; set; }
}

/// <summary>
/// Error response DTO for validation and business rule violations.
/// Multiple errors returned together to improve client user experience.
/// </summary>
public sealed class ErrorResponse {
    /// <summary>Gets or sets the Code property of type string, defaulting to string.Empty.</summary>
    public string Code { get; set; } = string.Empty;
    /// <summary>Gets or sets the Message property of type string, defaulting to string.Empty.</summary>
    public string Message { get; set; } = string.Empty;
    /// <summary>Gets or sets the Details property of type Dictionary&lt;string, string&gt;?.</summary>
    public Dictionary<string, string>? Details { get; set; }
    /// <summary>Gets or sets the TraceId property of type string?.</summary>
    public string? TraceId { get; set; }
}

/// <summary>
/// Response DTO for tenant quota information.
/// Includes tenant id, used bytes, quota bytes, and usage percentage.
/// </summary>
public sealed class TenantQuotaReport
{
    /// <summary>Gets or sets the TenantId property of type string, defaulting to string.Empty.</summary>
    public string TenantId { get; set; } = string.Empty;
    /// <summary>Gets or sets the UsedBytes property of type long.</summary>
    public long UsedBytes { get; set; }
    /// <summary>Gets or sets the QuotaBytes property of type long?.</summary>
    public long? QuotaBytes { get; set; }
    /// <summary>Gets or sets the UsagePercent property of type double.</summary>
    public double UsagePercent { get; set; }
}

/// <summary>
/// Response DTO for aggregated tenant quota report.
/// Includes summary statistics and individual tenant reports.
/// </summary>
public sealed class TenantQuotaSummaryReport
{
    /// <summary>Gets or sets the TotalUsedBytes property of type long.</summary>
    public long TotalUsedBytes { get; set; }
    /// <summary>Gets or sets the TotalQuotaBytes property of type long.</summary>
    public long TotalQuotaBytes { get; set; }
    /// <summary>Gets or sets the OverallUsagePercent property of type double.</summary>
    public double OverallUsagePercent { get; set; }
    /// <summary>Gets or sets the TotalTenants property of type int.</summary>
    public int TotalTenants { get; set; }
    /// <summary>Gets or sets the TenantsOverQuota property of type int.</summary>
    public int TenantsOverQuota { get; set; }
    /// <summary>Gets or sets the TenantsNearQuota property of type int.</summary>
    public int TenantsNearQuota { get; set; }
    /// <summary>Gets or sets the TenantReports property of type List&lt;TenantQuotaReport&gt;, defaulting to a new list.</summary>
    public List<TenantQuotaReport> TenantReports { get; set; } = new();
}
