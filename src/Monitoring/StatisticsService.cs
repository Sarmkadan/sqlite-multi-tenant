#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace SqliteMultiTenant.Monitoring;

/// <summary>
/// Collects and analyzes system statistics and usage metrics.
/// Provides insights into system health, performance, and resource utilization.
/// Supports time-series data aggregation and trend analysis.
/// </summary>
public interface IStatisticsService
{
    Task RecordEventAsync(SystemEvent @event);
    Task<SystemStatistics> GetStatisticsAsync(TimeSpan period);
    Task<List<AggregatedMetric>> GetMetricsAsync(string metricName, TimeSpan period);
    Task<TrendAnalysis> AnalyzeTrendAsync(string metricName, TimeSpan period);
}

/// <summary>
/// Implements statistics collection and analysis functionality.
/// Thread-safe service for recording system events and generating metrics.
/// </summary>
public sealed class StatisticsService : IStatisticsService {
    private readonly List<SystemEvent> _events;
    private readonly SemaphoreSlim _semaphore;
    private readonly ILogger<StatisticsService> _logger;
    private const int MaxEventsInMemory = 10000;

    /// <summary>
    /// Initializes a new instance of the StatisticsService class.
    /// </summary>
    /// <param name="logger">The logger instance for diagnostic logging.</param>
    public StatisticsService(ILogger<StatisticsService> logger)
    {
        _logger = logger;
        _events = new List<SystemEvent>();
        _semaphore = new SemaphoreSlim(1);
    }

    /// <summary>
    /// Records a system event for statistics.
    /// </summary>
    public async Task RecordEventAsync(SystemEvent @event)
    {
        ArgumentNullException.ThrowIfNull(@event);
        try
        {
            await _semaphore.WaitAsync();

            @event.Id = Guid.NewGuid().ToString();
            @event.Timestamp = DateTime.UtcNow;

            _events.Add(@event);

            // Maintain memory limit
            if (_events.Count > MaxEventsInMemory)
                _events.RemoveRange(0, _events.Count - MaxEventsInMemory);

            _logger.LogDebug($"Event recorded: {(@event.EventType)}");
        }
        finally
        {
            _semaphore.Release();
        }
    }

    /// <summary>
    /// Gets statistics for a given time period.
    /// </summary>
    public async Task<SystemStatistics> GetStatisticsAsync(TimeSpan period)
    {
        try
        {
            await _semaphore.WaitAsync();

            var cutoffTime = DateTime.UtcNow - period;
            var relevantEvents = _events.Where(e => e.Timestamp >= cutoffTime).ToList();

            var stats = new SystemStatistics
            {
                Period = period,
                StartTime = cutoffTime,
                EndTime = DateTime.UtcNow,
                TotalEvents = relevantEvents.Count,
                EventTypeBreakdown = relevantEvents
                    .GroupBy(e => e.EventType)
                    .ToDictionary(g => g.Key, g => g.Count()),
                AverageResponseTime = relevantEvents
                    .Where(e => e.Duration.HasValue)
                    .Average(e => e.Duration!.Value.TotalMilliseconds),
                PeakEventCount = GetPeakEventCount(relevantEvents)
            };

            return stats;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    /// <summary>
    /// Gets aggregated metrics for a specific metric type.
    /// </summary>
    public async Task<List<AggregatedMetric>> GetMetricsAsync(string metricName, TimeSpan period)
    {
        ArgumentException.ThrowIfNullOrEmpty(metricName);
        try
        {
            await _semaphore.WaitAsync();

            var cutoffTime = DateTime.UtcNow - period;
            var relevantEvents = _events
                .Where(e => e.Timestamp >= cutoffTime && e.EventType == metricName)
                .ToList();

            // Aggregate by hour
            var aggregated = relevantEvents
                .GroupBy(e => e.Timestamp.AddSeconds(-(e.Timestamp.Second + e.Timestamp.Minute * 60)))
                .Select(g => new AggregatedMetric
                {
                    Timestamp = g.Key,
                    Value = g.Average(e => e.Value),
                    Count = g.Count(),
                    Min = g.Min(e => e.Value),
                    Max = g.Max(e => e.Value)
                })
                .OrderBy(m => m.Timestamp)
                .ToList();

            return aggregated;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    /// <summary>
    /// Analyzes trends in a metric over time.
    /// </summary>
    public async Task<TrendAnalysis> AnalyzeTrendAsync(string metricName, TimeSpan period)
    {
        ArgumentException.ThrowIfNullOrEmpty(metricName);
        try
        {
            await _semaphore.WaitAsync();

            var cutoffTime = DateTime.UtcNow - period;
            var relevantEvents = _events
                .Where(e => e.Timestamp >= cutoffTime && e.EventType == metricName)
                .OrderBy(e => e.Timestamp)
                .ToList();

            if (relevantEvents.Count < 2)
                return new TrendAnalysis { TrendDirection = "Insufficient data" };

            var values = relevantEvents.Select(e => e.Value).ToList();

            // Calculate trend
            var trend = CalculateTrend(values);
            var volatility = CalculateVolatility(values);

            return new TrendAnalysis
            {
                MetricName = metricName,
                Period = period,
                DataPoints = relevantEvents.Count,
                AverageValue = values.Average(),
                MinValue = values.Min(),
                MaxValue = values.Max(),
                TrendDirection = trend > 0 ? "Upward" : trend < 0 ? "Downward" : "Stable",
                TrendStrength = Math.Abs(trend),
                Volatility = volatility,
                Timestamp = DateTime.UtcNow
            };
        }
        finally
        {
            _semaphore.Release();
        }
    }

    private int GetPeakEventCount(List<SystemEvent> events)
    {
        if (events.Count == 0)
            return 0;

        return events
            .GroupBy(e => e.Timestamp.AddSeconds(-(e.Timestamp.Second)))
            .Max(g => g.Count());
    }

    private double CalculateTrend(List<double> values)
    {
        if (values.Count < 2)
            return 0;

        // Simple linear regression trend
        int n = values.Count;
        double sumX = Enumerable.Range(0, n).Sum(i => (double)i);
        double sumY = values.Sum();
        double sumXY = Enumerable.Range(0, n).Sum(i => i * values[i]);
        double sumX2 = Enumerable.Range(0, n).Sum(i => i * i);

        double slope = (n * sumXY - sumX * sumY) / (n * sumX2 - sumX * sumX);
        return slope;
    }

    private double CalculateVolatility(List<double> values)
    {
        if (values.Count < 2)
            return 0;

        double mean = values.Average();
        double variance = values.Average(x => Math.Pow(x - mean, 2));
        return Math.Sqrt(variance);
    }
}

/// <summary>
/// Represents a system event with associated metrics and metadata.
/// </summary>
public sealed class SystemEvent {
    /// <summary>
    /// Unique identifier for the event.
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Type or category of the event (e.g., "Request", "Error", "Metric").
    /// </summary>
    public string EventType { get; set; } = string.Empty;

    /// <summary>
    /// Numeric value associated with the event (e.g., response time, count).
    /// </summary>
    public double Value { get; set; }

    /// <summary>
    /// Optional duration of the event if applicable.
    /// </summary>
    public TimeSpan? Duration { get; set; }

    /// <summary>
    /// Timestamp when the event occurred (UTC).
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// Additional key-value pairs for event metadata.
    /// </summary>
    public Dictionary<string, string> Tags { get; set; } = new();
}

/// <summary>
/// Contains statistical data for a specific time period.
/// </summary>
public sealed class SystemStatistics {
    /// <summary>
    /// The time period for which statistics are collected.
    /// </summary>
    public TimeSpan Period { get; set; }

    /// <summary>
    /// The start time of the period (UTC).
    /// </summary>
    public DateTime StartTime { get; set; }

    /// <summary>
    /// The end time of the period (UTC).
    /// </summary>
    public DateTime EndTime { get; set; }

    /// <summary>
    /// Total number of events in the period.
    /// </summary>
    public int TotalEvents { get; set; }

    /// <summary>
    /// Breakdown of events by type (event type -> count).
    /// </summary>
    public Dictionary<string, int> EventTypeBreakdown { get; set; } = new();

    /// <summary>
    /// Average response time in milliseconds for events with duration.
    /// </summary>
    public double AverageResponseTime { get; set; }

    /// <summary>
    /// Maximum number of events recorded in any single second within the period.
    /// </summary>
    public int PeakEventCount { get; set; }
}

/// <summary>
/// Represents an aggregated metric over a time interval.
/// </summary>
public sealed class AggregatedMetric {
    /// <summary>
    /// The timestamp of the aggregation interval (start of the interval).
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// The average value of the metric in the interval.
    /// </summary>
    public double Value { get; set; }

    /// <summary>
    /// The number of samples in the interval.
    /// </summary>
    public int Count { get; set; }

    /// <summary>
    /// The minimum value of the metric in the interval.
    /// </summary>
    public double Min { get; set; }

    /// <summary>
    /// The maximum value of the metric in the interval.
    /// </>
    public double Max { get; set; }
}

/// <summary>
/// Contains the result of a trend analysis for a metric over a time period.
/// </summary>
public sealed class TrendAnalysis {
    /// <summary>
    /// The name of the metric that was analyzed.
    /// </summary>
    public string MetricName { get; set; } = string.Empty;

    /// <summary>
    /// The time period over which the trend was analyzed.
    /// </summary>
    public TimeSpan Period { get; set; }

    /// <summary>
    /// The number of data points in the analysis.
    /// </summary>
    public int DataPoints { get; set; }

    /// <summary>
    /// The average value of the metric over the period.
    /// </summary>
    public double AverageValue { get; set; }

    /// <summary>
    /// The minimum value of the metric over the period.
    /// </summary>
    public double MinValue { get; set; }

    /// <summary>
    /// The maximum value of the metric over the period.
    /// </summary>
    public double MaxValue { get; set; }

    /// <summary>
    /// The direction of the trend (e.g., "Upward", "Downward", "Stable").
    /// </summary>
    public string TrendDirection { get; set; } = "Stable";

    /// <summary>
    /// The strength of the trend (absolute value of the slope).
    /// </summary>
    public double TrendStrength { get; set; }

    /// <summary>
    /// The volatility (standard deviation) of the metric values.
    /// </summary>
    public double Volatility { get; set; }

    /// <summary>
    /// The timestamp when the analysis was performed (UTC).
    /// </summary>
    public DateTime Timestamp { get; set; }
}
