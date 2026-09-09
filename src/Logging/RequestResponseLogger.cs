#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace SqliteMultiTenant.Logging;

/// <summary>
/// Logs HTTP request and response details for debugging and analytics.
/// Captures headers, body content, timing, and error information.
/// Implements sampling and filtering to manage log volume.
/// </summary>
public interface IRequestResponseLogger
{
    /// <summary>
    /// Logs an HTTP request asynchronously.
    /// </summary>
    /// <param name="request">The request log entry to log.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task LogRequestAsync(RequestLog request);
    /// <summary>
    /// Logs an HTTP response asynchronously.
    /// </summary>
    /// <param name="response">The response log entry to log.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task LogResponseAsync(ResponseLog response);
    /// <summary>
    /// Retrieves request logs matching the specified filter asynchronously.
    /// </summary>
    /// <param name="filter">The filter to apply when retrieving request logs.</param>
    /// <returns>A task that contains the list of request logs matching the filter.</returns>
    Task<List<RequestLog>> GetRequestLogsAsync(LogFilter filter);
    /// <summary>
    /// Retrieves response logs matching the specified filter asynchronously.
    /// </summary>
    /// <param name="filter">The filter to apply when retrieving response logs.</param>
    /// <returns>A task that contains the list of response logs matching the filter.</returns>
    Task<List<ResponseLog>> GetResponseLogsAsync(LogFilter filter);
}

public sealed class RequestResponseLogger : IRequestResponseLogger
{
    private readonly List<RequestLog> _requestLogs;
    private readonly List<ResponseLog> _responseLogs;
    private readonly ILogger<RequestResponseLogger> _logger;
    private readonly SemaphoreSlim _semaphore;
    private const int MaxLogsInMemory = 5000;
    private const int SamplingRate = 100; // Log 1 of every 100 requests by default

    /// <summary>
    /// Initializes a new instance of the <see cref="RequestResponseLogger"/> class.
    /// </summary>
    /// <param name="logger">The logger instance used for logging debug information.</param>
    public RequestResponseLogger(ILogger<RequestResponseLogger> logger)
    {
        _logger = logger;
        _requestLogs = new List<RequestLog>();
        _responseLogs = new List<ResponseLog>();
        _semaphore = new SemaphoreSlim(1);
    }

    /// <summary>
    /// Logs an HTTP request.
    /// </summary>
    public async Task LogRequestAsync(RequestLog request)
    {
        ArgumentNullException.ThrowIfNull(request);
        try
        {
            // Apply sampling
            if (new Random().Next(0, SamplingRate) != 0)
                return;
            await _semaphore.WaitAsync();
            request.Id = Guid.NewGuid().ToString();
            request.Timestamp = DateTime.UtcNow;
            _requestLogs.Add(request);
            // Maintain memory limit
            if (_requestLogs.Count > MaxLogsInMemory)
                _requestLogs.RemoveRange(0, _requestLogs.Count - MaxLogsInMemory);
            _logger.LogDebug("Request logged: {Method} {Path}", request.Method, request.Path);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    /// <summary>
    /// Logs an HTTP response.
    /// </summary>
    public async Task LogResponseAsync(ResponseLog response)
    {
        ArgumentNullException.ThrowIfNull(response);
        try
        {
            await _semaphore.WaitAsync();

            response.Id = Guid.NewGuid().ToString();
            response.Timestamp = DateTime.UtcNow;

            _responseLogs.Add(response);

            // Maintain memory limit
            if (_responseLogs.Count > MaxLogsInMemory)
                _responseLogs.RemoveRange(0, _responseLogs.Count - MaxLogsInMemory);

            _logger.LogDebug("Response logged: Status {StatusCode}, Duration {DurationMs}ms", response.StatusCode, response.DurationMs);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    /// <summary>
    /// Retrieves request logs matching filter criteria.
    /// </summary>
    public async Task<List<RequestLog>> GetRequestLogsAsync(LogFilter filter)
    {
        ArgumentNullException.ThrowIfNull(filter);
        try
        {
            await _semaphore.WaitAsync();

            var query = _requestLogs.AsEnumerable();

            if (!string.IsNullOrEmpty(filter.Method))
                query = query.Where(r => r.Method == filter.Method);

            if (!string.IsNullOrEmpty(filter.Path))
                query = query.Where(r => r.Path.Contains(filter.Path, StringComparison.OrdinalIgnoreCase));

            if (filter.StartTime.HasValue)
                query = query.Where(r => r.Timestamp >= filter.StartTime);

            if (filter.EndTime.HasValue)
                query = query.Where(r => r.Timestamp <= filter.EndTime);

            return query
                .OrderByDescending(r => r.Timestamp)
                .Take(filter.Limit)
                .ToList();
        }
        finally
        {
            _semaphore.Release();
        }
    }

    /// <summary>
    /// Retrieves response logs matching filter criteria.
    /// </summary>
    public async Task<List<ResponseLog>> GetResponseLogsAsync(LogFilter filter)
    {
        ArgumentNullException.ThrowIfNull(filter);
        try
        {
            await _semaphore.WaitAsync();

            var query = _responseLogs.AsEnumerable();

            if (filter.StatusCode.HasValue)
                query = query.Where(r => r.StatusCode == filter.StatusCode);

            if (filter.StartTime.HasValue)
                query = query.Where(r => r.Timestamp >= filter.StartTime);

            if (filter.EndTime.HasValue)
                query = query.Where(r => r.Timestamp <= filter.EndTime);

            if (filter.MinDuration.HasValue)
                query = query.Where(r => r.DurationMs >= filter.MinDuration);

            return query
                .OrderByDescending(r => r.Timestamp)
                .Take(filter.Limit)
                .ToList();
        }
        finally
        {
            _semaphore.Release();
        }
    }

    /// <summary>
    /// Gets comprehensive logging statistics.
    /// </summary>
    public async Task<LoggingStatistics> GetStatisticsAsync()
    {
        try
        {
            await _semaphore.WaitAsync();

            return new LoggingStatistics
            {
                TotalRequestsLogged = _requestLogs.Count,
                TotalResponsesLogged = _responseLogs.Count,
                AverageRequestSize = _requestLogs.Any() ? _requestLogs.Average(r => r.Body?.Length ?? 0) : 0,
                AverageResponseTime = _responseLogs.Any() ? _responseLogs.Average(r => r.DurationMs) : 0,
                MostCommonPath = _requestLogs
                    .GroupBy(r => r.Path)
                    .OrderByDescending(g => g.Count())
                    .FirstOrDefault()?.Key ?? "N/A",
                MostCommonMethod = _requestLogs
                    .GroupBy(r => r.Method)
                    .OrderByDescending(g => g.Count())
                    .FirstOrDefault()?.Key ?? "N/A"
            };
        }
        finally
        {
            _semaphore.Release();
        }
    }
}

/// <summary>
/// Represents an HTTP request log entry.
/// </summary>
public sealed class RequestLog
{
    /// <summary>
    /// Unique identifier for the log entry.
    /// </summary>
    public string Id { get; set; } = string.Empty;
    /// <summary>
    /// The date and time when the request was logged.
    /// </summary>
    public DateTime Timestamp { get; set; }
    /// <summary>
    /// The HTTP method of the request (e.g., GET, POST).
    /// </summary>
    public string Method { get; set; } = string.Empty;
    /// <summary>
    /// The URL path of the request.
    /// </summary>
    public string Path { get; set; } = string.Empty;
    /// <summary>
    /// The host header of the request.
    /// </summary>
    public string Host { get; set; } = string.Empty;
    /// <summary>
    /// The body content of the request, if any.
    /// </summary>
    public string? Body { get; set; }
    /// <summary>
    /// The HTTP headers of the request.
    /// </summary>
    public Dictionary<string, string> Headers { get; set; } = new();
    /// <summary>
    /// The query parameters of the request.
    /// </summary>
    public Dictionary<string, string> QueryParameters { get; set; } = new();
    /// <summary>
    /// The IP address of the client that made the request.
    /// </summary>
    public string IpAddress { get; set; } = string.Empty;
}

/// <summary>
/// Represents an HTTP response log entry.
/// </summary>
public sealed class ResponseLog
{
    /// <summary>
    /// Unique identifier for the log entry.
    /// </summary>
    public string Id { get; set; } = string.Empty;
    /// <summary>
    /// The date and time when the response was logged.
    /// </summary>
    public DateTime Timestamp { get; set; }
    /// <summary>
    /// The HTTP status code of the response.
    /// </summary>
    public int StatusCode { get; set; }
    /// <summary>
    /// The duration of the request in milliseconds.
    /// </summary>
    public long DurationMs { get; set; }
    /// <summary>
    /// The body content of the response, if any.
    /// </summary>
    public string? Body { get; set; }
    /// <summary>
    /// The size of the response in bytes.
    /// </summary>
    public long ResponseSize { get; set; }
    /// <summary>
    /// The HTTP headers of the response.
    /// </summary>
    public Dictionary<string, string> Headers { get; set; } = new();
}

/// <summary>
/// Represents the filter criteria for retrieving log entries.
/// </summary>
public sealed class LogFilter
{
    /// <summary>
    /// The HTTP method to filter by (e.g., GET, POST). If null or empty, no method filter is applied.
    /// </summary>
    public string? Method { get; set; }
    /// <summary>
    /// The URL path to filter by. If null or empty, no path filter is applied.
    /// </summary>
    public string? Path { get; set; }
    /// <summary>
    /// The HTTP status code to filter by. If null, no status code filter is applied.
    /// </summary>
    public int? StatusCode { get; set; }
    /// <summary>
    /// The start time for filtering logs. If null, no start time filter is applied.
    /// </summary>
    public DateTime? StartTime { get; set; }
    /// <summary>
    /// The end time for filtering logs. If null, no end time filter is applied.
    /// </summary>
    public DateTime? EndTime { get; set; }
    /// <summary>
    /// The minimum duration in milliseconds for filtering logs. If null, no minimum duration filter is applied.
    /// </summary>
    public long? MinDuration { get; set; }
    /// <summary>
    /// The maximum number of log entries to return. Defaults to 100.
    /// </summary>
    public int Limit { get; set; } = 100;
}

/// <summary>
/// Represents logging statistics.
/// </summary>
public sealed class LoggingStatistics
{
    /// <summary>
    /// The total number of requests logged.
    /// </summary>
    public int TotalRequestsLogged { get; set; }
    /// <summary>
    /// The total number of responses logged.
    /// </summary>
    public int TotalResponsesLogged { get; set; }
    /// <summary>
    /// The average size of the request bodies in bytes.
    /// </summary>
    public double AverageRequestSize { get; set; }
    /// <summary>
    /// The average response time in milliseconds.
    /// </summary>
    public double AverageResponseTime { get; set; }
    /// <summary>
    /// The most frequently requested URL path.
    /// </summary>
    public string MostCommonPath { get; set; } = string.Empty;
    /// <summary>
    /// The most frequently used HTTP method.
    /// </summary>
    public string MostCommonMethod { get; set; } = string.Empty;
}