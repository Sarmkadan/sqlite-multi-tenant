#nullable enable
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
public sealed class Result<T> {
    /// <summary>
    /// Indicates whether the operation was successful.
    /// </summary>
    public bool Success { get; set; }
    /// <summary>
    /// The data returned by the operation, if successful.
    /// </summary>
    public T? Data { get; set; }
    /// <summary>
    /// A list of error messages if the operation failed.
    /// </summary>
    public List<string> Errors { get; set; } = new();
    /// <summary>
    /// A message describing the result.
    /// </summary>
    public string? Message { get; set; }
    /// <summary>
    /// Additional metadata about the result.
    /// </summary>
    public ResultMetadata? Metadata { get; set; }

    /// <summary>
    /// Creates a successful result with the specified data and optional message.
    /// </summary>
    /// <param name="data">The data to include in the result.</param>
    /// <param name="message">An optional message describing the operation.</param>
    /// <returns>A successful result.</returns>
    public static Result<T> Ok(T data, string? message = null)
    {
        return new Result<T>
        {
            Success = true,
            Data = data,
            Message = message
        };
    }

    /// <summary>
    /// Creates a failed result with the specified error message.
    /// </summary>
    /// <param name="error">The error message.</param>
    /// <returns>A failed result.</returns>
    public static Result<T> Fail(string error)
    {
        return new Result<T>
        {
            Success = false,
            Errors = new List<string> { error }
        };
    }

    /// <summary>
    /// Creates a failed result with the specified list of error messages.
    /// </summary>
    /// <param name="errors">The list of error messages.</param>
    /// <returns>A failed result.</returns>
    public static Result<T> Fail(List<string> errors)
    {
        return new Result<T>
        {
            Success = false,
            Errors = errors
        };
    }

    public override string ToString() => $"Result {{ Success = {Success}, Data = {Data}, Errors = {Errors}, Message = {Message}, Metadata = {Metadata} }}";
}

/// <summary>
/// Result wrapper for paginated data.
/// </summary>
public sealed class PaginatedResult<T> {
    /// <summary>
    /// Indicates whether the operation was successful.
    /// </summary>
    public bool Success { get; set; }
    /// <summary>
    /// The collection of items returned by the operation.
    /// </summary>
    public List<T> Items { get; set; } = new();
    /// <summary>
    /// Pagination metadata for the result.
    /// </summary>
    public PaginationMetadata Pagination { get; set; } = new();
    /// <summary>
    /// A list of error messages if the operation failed.
    /// </summary>
    public List<string> Errors { get; set; } = new();
    /// <summary>
    /// A message describing the result.
    /// </summary>
    public string? Message { get; set; }

    /// <summary>
    /// Creates a successful paginated result with the specified items and pagination information.
    /// </summary>
    /// <param name="items">The collection of items to include in the result.</param>
    /// <param name="pageNumber">The current page number (1-based).</param>
    /// <param name="pageSize">The number of items per page.</param>
    /// <param name="totalCount">The total number of items available.</param>
    /// <returns>A successful paginated result.</returns>
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

    /// <summary>
    /// Creates a failed paginated result with the specified error message.
    /// </summary>
    /// <param name="error">The error message.</param>
    /// <returns>A failed paginated result.</returns>
    public static PaginatedResult<T> Fail(string error)
    {
        return new PaginatedResult<T>
        {
            Success = false,
            Errors = new List<string> { error }
        };
    }
}

/// <summary>
/// Additional metadata for result objects.
/// </summary>
public sealed class ResultMetadata {
    /// <summary>
    /// The timestamp when the result was created.
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    /// <summary>
    /// The trace identifier for debugging purposes.
    /// </summary>
    public string? TraceId { get; set; }
    /// <summary>
    /// The HTTP status code associated with the result.
    /// </summary>
    public int? StatusCode { get; set; }
    /// <summary>
    /// Additional data associated with the result.
    /// </summary>
    public Dictionary<string, object> AdditionalData { get; set; } = new();
}

/// <summary>
/// Pagination metadata for paginated results.
/// </summary>
public sealed class PaginationMetadata {
    /// <summary>
    /// The current page number (1-based).
    /// </summary>
    public int PageNumber { get; set; }
    /// <summary>
    /// The number of items per page.
    /// </summary>
    public int PageSize { get; set; }
    /// <summary>
    /// The total number of items available.
    /// </summary>
    public int TotalCount { get; set; }
    /// <summary>
    /// The total number of pages.
    /// </summary>
    public int TotalPages { get; set; }
    /// <summary>
    /// Indicates whether there is a previous page.
    /// </summary>
    public bool HasPreviousPage => PageNumber > 1;
    /// <summary>
    /// Indicates whether there is a next page.
    /// </summary>
    public bool HasNextPage => PageNumber < TotalPages;
}

/// <summary>
/// Operation result for actions without return data.
/// </summary>
public sealed class OperationResult {
    /// <summary>
    /// Indicates whether the operation was successful.
    /// </summary>
    public bool Success { get; set; }
    /// <summary>
    /// A message describing the result of the operation.
    /// </summary>
    public string? Message { get; set; }
    /// <summary>
    /// A list of error messages if the operation failed.
    /// </summary>
    public List<string> Errors { get; set; } = new();
    /// <summary>
    /// The timestamp when the operation completed.
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Creates a successful operation result with an optional message.
    /// </summary>
    /// <param name="message">An optional message describing the operation.</param>
    /// <returns>A successful operation result.</returns>
    public static OperationResult Ok(string? message = null)
    {
        return new OperationResult
        {
            Success = true,
            Message = message
        };
    }

    /// <summary>
    /// Creates a failed operation result with the specified error message.
    /// </summary>
    /// <param name="error">The error message.</param>
    /// <returns>A failed operation result.</returns>
    public static OperationResult Fail(string error)
    {
        return new OperationResult
        {
            Success = false,
            Errors = new List<string> { error }
        };
    }

    /// <summary>
    /// Creates a failed operation result with the specified list of error messages.
    /// </summary>
    /// <param name="errors">The list of error messages.</param>
    /// <returns>A failed operation result.</returns>
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
public sealed class BatchOperationResult {
    /// <summary>
    /// The number of items that were processed successfully.
    /// </summary>
    public int SuccessCount { get; set; }
    /// <summary>
    /// The number of items that failed to process.
    /// </summary>
    public int FailureCount { get; set; }
    /// <summary>
    /// The collection of individual item results.
    /// </summary>
    public List<BatchItemResult> Items { get; set; } = new();
    /// <summary>
    /// Indicates whether the batch operation was successful (no failures).
    /// </summary>
    public bool Success => FailureCount == 0;

    /// <summary>
    /// Gets the total number of items in the batch operation.
    /// </summary>
    /// <returns>The total number of items (successful + failed).</returns>
    public int GetTotalCount() => SuccessCount + FailureCount;
}

/// <summary>
/// Represents the result of processing an individual item in a batch operation.
/// </summary>
public sealed class BatchItemResult {
    /// <summary>
    /// The unique identifier of the item.
    /// </summary>
    public string ItemId { get; set; } = string.Empty;
    /// <summary>
    /// Indicates whether the item was processed successfully.
    /// </summary>
    public bool Success { get; set; }
    /// <summary>
    /// The error message if the item failed to process.
    /// </summary>
    public string? Error { get; set; }
    /// <summary>
    /// The data resulting from processing the item, if successful.
    /// </summary>
    public object? Data { get; set; }
}