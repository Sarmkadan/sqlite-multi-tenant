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
    /// <summary>
    /// Logs an audit entry for a system operation or change.
    /// </summary>
    /// <param name="entry">The audit log entry to log.</param>
    /// <returns>A task that completes when the entry has been stored.</returns>
    Task LogAsync(AuditLogEntry entry);

    /// <summary>
    /// Retrieves audit log entries matching the specified filter criteria.
    /// </summary>
    /// <param name="filter">The filter criteria.</param>
    /// <returns>A list of matching entries, newest first, limited by the filter's <see cref="AuditLogFilter.Limit"/>.</returns>
    Task<List<AuditLogEntry>> GetEntriesAsync(AuditLogFilter filter);

    /// <summary>
    /// Gets the count of audit log entries matching the filter.
    /// </summary>
    /// <param name="filter">The filter criteria.</param>
    /// <returns>The number of matching entries.</returns>
    Task<int> GetEntryCountAsync(AuditLogFilter filter);

    /// <summary>
    /// Purges audit log entries older than the retention period.
    /// </summary>
    /// <param name="retentionPeriod">How long entries are kept, measured back from the current UTC time.</param>
    /// <returns>A task that completes when old entries have been removed.</returns>
    Task PurgeOldEntriesAsync(TimeSpan retentionPeriod);
}

/// <summary>
/// In-memory implementation of <see cref="IAuditLogger"/>. Keeps at most 10,000 entries.
/// </summary>
public sealed class AuditLogger : IAuditLogger
{
    private readonly List<AuditLogEntry> _entries;
    private readonly ILogger<AuditLogger> _logger;
    private readonly SemaphoreSlim _semaphore;
    private const int MaxEntriesInMemory = 10000;

    /// <summary>
    /// Initializes a new instance of the <see cref="AuditLogger"/> class.
    /// </summary>
    /// <param name="logger">The logger used to report audit activity.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="logger"/> is null.</exception>
    public AuditLogger(ILogger<AuditLogger> logger)
    {
        ArgumentNullException.ThrowIfNull(logger);
        _logger = logger;
        _entries = new List<AuditLogEntry>();
        _semaphore = new SemaphoreSlim(1);
    }

    /// <summary>
    /// Logs an audit entry for a system operation or change.
    /// </summary>
    /// <param name="entry">The audit log entry to log.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="entry"/> is null.</exception>
    public async Task LogAsync(AuditLogEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);
        try
        {
            await _semaphore.WaitAsync();

            entry.Id = Guid.NewGuid().ToString();
            entry.Timestamp = DateTime.UtcNow;

            _entries.Add(entry);

            // Maintain memory limit
            if (_entries.Count > MaxEntriesInMemory)
            {
                _entries.RemoveRange(0, _entries.Count - MaxEntriesInMemory);
            }

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
    /// <param name="filter">The filter criteria.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="filter"/> is null.</exception>
    public async Task<List<AuditLogEntry>> GetEntriesAsync(AuditLogFilter filter)
    {
        ArgumentNullException.ThrowIfNull(filter);
        try
        {
            await _semaphore.WaitAsync();

            var query = _entries.AsEnumerable();

            // Apply filters
            if (!string.IsNullOrEmpty(filter.EventType))
            {
                query = query.Where(e => e.EventType == filter.EventType);
            }

            if (!string.IsNullOrEmpty(filter.Actor))
            {
                query = query.Where(e => e.Actor == filter.Actor);
            }

            if (!string.IsNullOrEmpty(filter.ResourceId))
            {
                query = query.Where(e => e.ResourceId == filter.ResourceId);
            }

            if (filter.StartTime.HasValue)
            {
                query = query.Where(e => e.Timestamp >= filter.StartTime);
            }

            if (filter.EndTime.HasValue)
            {
                query = query.Where(e => e.Timestamp <= filter.EndTime);
            }

            if (!string.IsNullOrEmpty(filter.SearchTerm))
            {
                query = query.Where(e =>
                    e.Description.Contains(filter.SearchTerm, StringComparison.OrdinalIgnoreCase));
            }

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
    /// <param name="filter">The filter criteria.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="filter"/> is null.</exception>
    public async Task<int> GetEntryCountAsync(AuditLogFilter filter)
    {
        ArgumentNullException.ThrowIfNull(filter);
        try
        {
            await _semaphore.WaitAsync();

            var query = _entries.AsEnumerable();

            if (!string.IsNullOrEmpty(filter.EventType))
            {
                query = query.Where(e => e.EventType == filter.EventType);
            }

            if (!string.IsNullOrEmpty(filter.Actor))
            {
                query = query.Where(e => e.Actor == filter.Actor);
            }

            if (filter.StartTime.HasValue)
            {
                query = query.Where(e => e.Timestamp >= filter.StartTime);
            }

            if (filter.EndTime.HasValue)
            {
                query = query.Where(e => e.Timestamp <= filter.EndTime);
            }

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
    /// <param name="retentionPeriod">How long entries are kept, measured back from the current UTC time.</param>
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
    /// <returns>The current <see cref="AuditLogStatistics"/> snapshot.</returns>
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

/// <summary>
/// A single audit record describing who did what to which resource.
/// </summary>
public sealed class AuditLogEntry
{
    /// <summary>
    /// Gets or sets the unique identifier of the entry. Assigned by <see cref="AuditLogger.LogAsync"/>.
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the UTC time the entry was recorded. Assigned by <see cref="AuditLogger.LogAsync"/>.
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// Gets or sets the event type, such as a category name.
    /// </summary>
    public string EventType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the actor (user or system component) that performed the operation.
    /// </summary>
    public string Actor { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the identifier of the resource the operation applied to.
    /// </summary>
    public string ResourceId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the type of the resource the operation applied to.
    /// </summary>
    public string ResourceType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a human-readable description of the operation.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the <see cref="AuditAction"/> that was performed.
    /// </summary>
    public AuditAction Action { get; set; }

    /// <summary>
    /// Gets or sets the changed values, keyed by property or field name.
    /// </summary>
    public Dictionary<string, object> Changes { get; set; } = new();

    /// <summary>
    /// Gets or sets the IP address the operation originated from.
    /// </summary>
    public string IpAddress { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the identifier of the tenant the operation belongs to.
    /// </summary>
    public string TenantId { get; set; } = string.Empty;

    /// <summary>
    /// Returns a concise, single-line summary of the entry for logging and diagnostics.
    /// </summary>
    /// <returns>A string containing the entry's identifier, timestamp, event type, actor, action, resource, tenant and change count.</returns>
    public override string ToString() =>
        $"AuditLogEntry {{ Id = {Id}, Timestamp = {Timestamp:O}, EventType = {EventType}, Actor = {Actor}, Action = {Action}, ResourceType = {ResourceType}, ResourceId = {ResourceId}, TenantId = {TenantId}, Changes = {Changes.Count} }}";
}

/// <summary>
/// The kinds of operation an audit entry can record.
/// </summary>
public enum AuditAction
{
    /// <summary>A resource was created.</summary>
    Create,

    /// <summary>A resource was read.</summary>
    Read,

    /// <summary>A resource was updated.</summary>
    Update,

    /// <summary>A resource was deleted.</summary>
    Delete,

    /// <summary>An operation or command was executed.</summary>
    Execute,

    /// <summary>Data was exported.</summary>
    Export,

    /// <summary>Data was imported.</summary>
    Import
}

/// <summary>
/// Criteria used to query audit log entries. Unset properties do not restrict the result.
/// </summary>
public sealed class AuditLogFilter
{
    /// <summary>
    /// Gets or sets the event type to match exactly, or <c>null</c> to match any.
    /// </summary>
    public string? EventType { get; set; }

    /// <summary>
    /// Gets or sets the actor to match exactly, or <c>null</c> to match any.
    /// </summary>
    public string? Actor { get; set; }

    /// <summary>
    /// Gets or sets the resource identifier to match exactly, or <c>null</c> to match any.
    /// </summary>
    public string? ResourceId { get; set; }

    /// <summary>
    /// Gets or sets the inclusive lower bound on the entry timestamp, or <c>null</c> for no lower bound.
    /// </summary>
    public DateTime? StartTime { get; set; }

    /// <summary>
    /// Gets or sets the inclusive upper bound on the entry timestamp, or <c>null</c> for no upper bound.
    /// </summary>
    public DateTime? EndTime { get; set; }

    /// <summary>
    /// Gets or sets a case-insensitive substring to search for in the entry description, or <c>null</c> to skip.
    /// </summary>
    public string? SearchTerm { get; set; }

    /// <summary>
    /// Gets or sets the maximum number of entries to return. Defaults to 100.
    /// </summary>
    public int Limit { get; set; } = 100;

    /// <summary>
    /// Returns a concise, single-line summary of the filter for logging and diagnostics.
    /// </summary>
    /// <returns>A string listing each filter criterion, with <c>null</c> for unset values.</returns>
    public override string ToString() =>
        $"AuditLogFilter {{ EventType = {EventType ?? "null"}, Actor = {Actor ?? "null"}, ResourceId = {ResourceId ?? "null"}, StartTime = {StartTime:O}, EndTime = {EndTime:O}, SearchTerm = {SearchTerm ?? "null"}, Limit = {Limit} }}";
}

/// <summary>
/// Aggregate figures describing the contents of the audit log.
/// </summary>
public sealed class AuditLogStatistics
{
    /// <summary>
    /// Gets or sets the total number of entries currently held.
    /// </summary>
    public int TotalEntries { get; set; }

    /// <summary>
    /// Gets or sets the number of distinct actors across all entries.
    /// </summary>
    public int UniqueActors { get; set; }

    /// <summary>
    /// Gets or sets the number of distinct event types across all entries.
    /// </summary>
    public int UniqueEventTypes { get; set; }

    /// <summary>
    /// Gets or sets the timestamp of the oldest entry, or <c>null</c> when the log is empty.
    /// </summary>
    public DateTime? OldestEntry { get; set; }

    /// <summary>
    /// Gets or sets the timestamp of the newest entry, or <c>null</c> when the log is empty.
    /// </summary>
    public DateTime? NewestEntry { get; set; }

    /// <summary>
    /// Returns a concise, single-line summary of the statistics for logging and diagnostics.
    /// </summary>
    /// <returns>A string containing the entry, actor and event type counts and the oldest and newest timestamps.</returns>
    public override string ToString() =>
        $"AuditLogStatistics {{ TotalEntries = {TotalEntries}, UniqueActors = {UniqueActors}, UniqueEventTypes = {UniqueEventTypes}, OldestEntry = {OldestEntry:O}, NewestEntry = {NewestEntry:O} }}";
}
