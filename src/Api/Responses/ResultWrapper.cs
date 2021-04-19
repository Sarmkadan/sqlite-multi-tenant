// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace SqliteMultiTenant.Api.Responses;

/// <summary>
/// Generic result wrapper for consistent API responses.
/// Provides standardized structure for success, errors, and metadata.
/// Supports both data and paginated results.
/// </summary>
public class Result<T>
{
    public bool Success { get; set; }
    public T? Data { get; set; }
    public List<string> Errors { get; set; } = new();
    public string? Message { get; set; }
    public ResultMetadata? Metadata { get; set; }

    public static Result<T> Ok(T data, string? message = null)
    {
        return new Result<T>
        {
            Success = true,
            Data = data,
            Message = message
        };
    }

    public static Result<T> Fail(string error)
    {
        return new Result<T>
        {
            Success = false,
            Errors = new List<string> { error }
        };
    }

    public static Result<T> Fail(List<string> errors)
    {
        return new Result<T>
        {
            Success = false,
            Errors = errors
        };
    }
}

/// <summary>
/// Result wrapper for paginated data.
/// </summary>
public class PaginatedResult<T>
{
    public bool Success { get; set; }
    public List<T> Items { get; set; } = new();
    public PaginationMetadata Pagination { get; set; } = new();
    public List<string> Errors { get; set; } = new();
    public string? Message { get; set; }

    public static PaginatedResult<T> Ok(List<T> items, int pageNumber, int pageSize, int totalCount)
    {
        return new PaginatedResult<T>
        {
            Success = true,
            Items = items,
            Pagination = new PaginationMetadata
            {
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalCount = totalCount,
                TotalPages = (int)Math.Ceiling((double)totalCount / pageSize)
            }
        };
    }

    public static PaginatedResult<T> Fail(string error)
    {
        return new PaginatedResult<T>
        {
            Success = false,
            Errors = new List<string> { error }
        };
    }
}

public class ResultMetadata
{
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string? TraceId { get; set; }
    public int? StatusCode { get; set; }
    public Dictionary<string, object> AdditionalData { get; set; } = new();
}

public class PaginationMetadata
{
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public int TotalPages { get; set; }
    public bool HasPreviousPage => PageNumber > 1;
    public bool HasNextPage => PageNumber < TotalPages;
}

/// <summary>
/// Operation result for actions without return data.
/// </summary>
public class OperationResult
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public List<string> Errors { get; set; } = new();
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    public static OperationResult Ok(string? message = null)
    {
        return new OperationResult
        {
            Success = true,
            Message = message
        };
    }

    public static OperationResult Fail(string error)
    {
        return new OperationResult
        {
            Success = false,
            Errors = new List<string> { error }
        };
    }

    public static OperationResult Fail(List<string> errors)
    {
        return new OperationResult
        {
            Success = false,
            Errors = errors
        };
    }
}

/// <summary>
/// Batch operation result for multiple items.
/// </summary>
public class BatchOperationResult
{
    public int SuccessCount { get; set; }
    public int FailureCount { get; set; }
    public List<BatchItemResult> Items { get; set; } = new();
    public bool Success => FailureCount == 0;

    public int GetTotalCount() => SuccessCount + FailureCount;
}

public class BatchItemResult
{
    public string ItemId { get; set; } = string.Empty;
    public bool Success { get; set; }
    public string? Error { get; set; }
    public object? Data { get; set; }
}
