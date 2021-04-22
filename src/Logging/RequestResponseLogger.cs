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
    Task LogRequestAsync(RequestLog request);
    Task LogResponseAsync(ResponseLog response);
    Task<List<RequestLog>> GetRequestLogsAsync(LogFilter filter);
    Task<List<ResponseLog>> GetResponseLogsAsync(LogFilter filter);
}

public class RequestResponseLogger : IRequestResponseLogger
{
    private readonly List<RequestLog> _requestLogs;
    private readonly List<ResponseLog> _responseLogs;
    private readonly ILogger<RequestResponseLogger> _logger;
    private readonly SemaphoreSlim _semaphore;
    private const int MaxLogsInMemory = 5000;
    private const int SamplingRate = 100; // Log 1 of every 100 requests by default

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

            _logger.LogDebug($"Request logged: {request.Method} {request.Path}");
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
        try
        {
            await _semaphore.WaitAsync();

            response.Id = Guid.NewGuid().ToString();
            response.Timestamp = DateTime.UtcNow;

            _responseLogs.Add(response);

            // Maintain memory limit
            if (_responseLogs.Count > MaxLogsInMemory)
                _responseLogs.RemoveRange(0, _responseLogs.Count - MaxLogsInMemory);

            _logger.LogDebug($"Response logged: Status {response.StatusCode}, Duration {response.DurationMs}ms");
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

public class RequestLog
{
    public string Id { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public string Method { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public string Host { get; set; } = string.Empty;
    public string? Body { get; set; }
    public Dictionary<string, string> Headers { get; set; } = new();
    public Dictionary<string, string> QueryParameters { get; set; } = new();
    public string IpAddress { get; set; } = string.Empty;
}

public class ResponseLog
{
    public string Id { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public int StatusCode { get; set; }
    public long DurationMs { get; set; }
    public string? Body { get; set; }
    public long ResponseSize { get; set; }
    public Dictionary<string, string> Headers { get; set; } = new();
}

public class LogFilter
{
    public string? Method { get; set; }
    public string? Path { get; set; }
    public int? StatusCode { get; set; }
    public DateTime? StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public long? MinDuration { get; set; }
    public int Limit { get; set; } = 100;
}

public class LoggingStatistics
{
    public int TotalRequestsLogged { get; set; }
    public int TotalResponsesLogged { get; set; }
    public double AverageRequestSize { get; set; }
    public double AverageResponseTime { get; set; }
    public string MostCommonPath { get; set; } = string.Empty;
    public string MostCommonMethod { get; set; } = string.Empty;
}
