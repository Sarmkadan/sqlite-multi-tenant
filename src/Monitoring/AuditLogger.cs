#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace SqliteMultiTenant.Monitoring;

/// <summary>
/// Centralized audit logger for tracking all system changes and operations.
/// Records user actions, system events, and configuration changes for compliance.
/// Supports filtering, searching, and retention policies.
/// </summary>
public interface IAuditLogger
{
    Task LogAsync(AuditLogEntry entry);
    Task<List<AuditLogEntry>> GetEntriesAsync(AuditLogFilter filter);
    Task<int> GetEntryCountAsync(AuditLogFilter filter);
    Task PurgeOldEntriesAsync(TimeSpan retentionPeriod);
}

public sealed class AuditLogger : IAuditLogger {
    private readonly List<AuditLogEntry> _entries;
    private readonly ILogger<AuditLogger> _logger;
    private readonly SemaphoreSlim _semaphore;
    private const int MaxEntriesInMemory = 10000;

    public AuditLogger(ILogger<AuditLogger> logger)
    {
        _logger = logger;
        _entries = new List<AuditLogEntry>();
        _semaphore = new SemaphoreSlim(1);
    }

    /// <summary>
    /// Logs an audit entry for a system operation or change.
    /// </summary>
    public async Task LogAsync(AuditLogEntry entry)
    {
        try
        {
            await _semaphore.WaitAsync();

            entry.Id = Guid.NewGuid().ToString();
            entry.Timestamp = DateTime.UtcNow;

            _entries.Add(entry);

            // Maintain memory limit
            if (_entries.Count > MaxEntriesInMemory)
                _entries.RemoveRange(0, _entries.Count - MaxEntriesInMemory);

            _logger.LogInformation(
                $"Audit logged: {entry.EventType} by {entry.Actor} - {entry.Description}");
        }
        finally
        {
            _semaphore.Release();
        }
    }

    /// <summary>
    /// Retrieves audit log entries matching the specified filter criteria.
    /// </summary>
    public async Task<List<AuditLogEntry>> GetEntriesAsync(AuditLogFilter filter)
    {
        try
        {
            await _semaphore.WaitAsync();

            var query = _entries.AsEnumerable();

            // Apply filters
            if (!string.IsNullOrEmpty(filter.EventType))
                query = query.Where(e => e.EventType == filter.EventType);

            if (!string.IsNullOrEmpty(filter.Actor))
                query = query.Where(e => e.Actor == filter.Actor);

            if (!string.IsNullOrEmpty(filter.ResourceId))
                query = query.Where(e => e.ResourceId == filter.ResourceId);

            if (filter.StartTime.HasValue)
                query = query.Where(e => e.Timestamp >= filter.StartTime);

            if (filter.EndTime.HasValue)
                query = query.Where(e => e.Timestamp <= filter.EndTime);

            if (!string.IsNullOrEmpty(filter.SearchTerm))
                query = query.Where(e =>
                    e.Description.Contains(filter.SearchTerm, StringComparison.OrdinalIgnoreCase));

            // Order and limit results
            return query
                .OrderByDescending(e => e.Timestamp)
                .Take(filter.Limit)
                .ToList();
        }
        finally
        {
            _semaphore.Release();
        }
    }

    /// <summary>
    /// Gets the count of audit log entries matching the filter.
    /// </summary>
    public async Task<int> GetEntryCountAsync(AuditLogFilter filter)
    {
        try
        {
            await _semaphore.WaitAsync();

            var query = _entries.AsEnumerable();

            if (!string.IsNullOrEmpty(filter.EventType))
                query = query.Where(e => e.EventType == filter.EventType);

            if (!string.IsNullOrEmpty(filter.Actor))
                query = query.Where(e => e.Actor == filter.Actor);

            if (filter.StartTime.HasValue)
                query = query.Where(e => e.Timestamp >= filter.StartTime);

            if (filter.EndTime.HasValue)
                query = query.Where(e => e.Timestamp <= filter.EndTime);

            return query.Count();
        }
        finally
        {
            _semaphore.Release();
        }
    }

    /// <summary>
    /// Purges old audit log entries older than the retention period.
    /// </summary>
    public async Task PurgeOldEntriesAsync(TimeSpan retentionPeriod)
    {
        try
        {
            await _semaphore.WaitAsync();

            var cutoffTime = DateTime.UtcNow - retentionPeriod;
            int removedCount = _entries.RemoveAll(e => e.Timestamp < cutoffTime);

            _logger.LogInformation("Purged {RemovedCount} old audit entries", removedCount);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    /// <summary>
    /// Gets statistics about audit log entries.
    /// </summary>
    public async Task<AuditLogStatistics> GetStatisticsAsync()
    {
        try
        {
            await _semaphore.WaitAsync();

            return new AuditLogStatistics
            {
                TotalEntries = _entries.Count,
                UniqueActors = _entries.Select(e => e.Actor).Distinct().Count(),
                UniqueEventTypes = _entries.Select(e => e.EventType).Distinct().Count(),
                OldestEntry = _entries.FirstOrDefault()?.Timestamp,
                NewestEntry = _entries.LastOrDefault()?.Timestamp
            };
        }
        finally
        {
            _semaphore.Release();
        }
    }
}

public sealed class AuditLogEntry {
    public string Id { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string Actor { get; set; } = string.Empty;
    public string ResourceId { get; set; } = string.Empty;
    public string ResourceType { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public AuditAction Action { get; set; }
    public Dictionary<string, object> Changes { get; set; } = new();
    public string IpAddress { get; set; } = string.Empty;
	public string TenantId { get; set; } = string.Empty;
}

public enum AuditAction
{
    Create,
    Read,
    Update,
    Delete,
    Execute,
    Export,
    Import
}

public sealed class AuditLogFilter {
    public string? EventType { get; set; }
    public string? Actor { get; set; }
    public string? ResourceId { get; set; }
    public DateTime? StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public string? SearchTerm { get; set; }
    public int Limit { get; set; } = 100;
}

public sealed class AuditLogStatistics {
    public int TotalEntries { get; set; }
    public int UniqueActors { get; set; }
    public int UniqueEventTypes { get; set; }
    public DateTime? OldestEntry { get; set; }
    public DateTime? NewestEntry { get; set; }
}
